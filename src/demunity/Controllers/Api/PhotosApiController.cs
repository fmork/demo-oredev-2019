using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Amazon.XRay.Recorder.Handlers.AwsSdk;
using demunity.lib;
using demunity.lib.Data.Models;
using demunity.lib.Logging;
using demunity.lib.Security;
using demunity.lib.Text;
using demunity.Models;
using demunity.Models.Converters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace demunity.Controllers.Api
{
    [Route("api/photos")]
    public class PhotosApiController : Controller
    {
        private readonly IPhotosService photosService;
        private readonly IFeedbackService feedbackService;
        private readonly IUsersService usersService;
        private readonly ISystem system;
        private readonly ILogWriter<PhotosApiController> logger;
        private readonly Lazy<Claim[]> lazyClaims;
        private readonly PhotoModelConverter photoModelConverter;

        public PhotosApiController(
            IPhotosService photosService,
            IFeedbackService feedbackService,
            IUsersService usersService,
            ISystem system,
            ITextSplitter textSplitter,
            ILogWriterFactory loggerFactory)
        {
            if (textSplitter is null)
            {
                throw new ArgumentNullException(nameof(textSplitter));
            }

            if (loggerFactory is null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            this.photosService = photosService ?? throw new ArgumentNullException(nameof(photosService));
            this.feedbackService = feedbackService ?? throw new ArgumentNullException(nameof(feedbackService));
            this.usersService = usersService ?? throw new ArgumentNullException(nameof(usersService));
            this.system = system ?? throw new ArgumentNullException(nameof(system));
            logger = loggerFactory.CreateLogger<PhotosApiController>();
            lazyClaims = new Lazy<Claim[]>(() => GetUserClaims());

            var staticAssetHost = new Uri(system.Environment.GetVariable(Constants.EnvironmentVariables.StaticAssetHost));
            this.photoModelConverter = new PhotoModelConverter(staticAssetHost, textSplitter, loggerFactory);
            AWSSDKHandler.RegisterXRayForAllServices();
        }



        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreatePhoto([FromBody] CreatePhotoModel input)
        {
            logger.LogInformation($"{nameof(CreatePhoto)}({nameof(input.Filename)} = '{input.Filename}')");

            try
            {
                var photo = await photosService.CreatePhoto(GetCurrentUserId(), GetCurrentUserName(), input.Filename, input.Text);
                return Json(photoModelConverter.ToPublic(photo));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in {nameof(CreatePhoto)}({nameof(input.Filename)} = '{input.Filename}'):\n{ex.ToString()}");
                throw;
            }
        }



        [HttpGet("latest")]
        public async Task<IActionResult> Latest()
        {
            logger.LogInformation($"{nameof(Latest)}()");
            IEnumerable<PhotoModel> latestPhotos = await photosService.GetLatestPhotos(GetCurrentUserId());
            return Json(latestPhotos.Select(photoModelConverter.ToPublic));
        }



        [HttpGet("popular")]
        public async Task<IActionResult> Popular()
        {
            logger.LogInformation($"{nameof(Popular)}()");
            IEnumerable<PhotoModel> popularPhotos = await photosService.GetPopularPhotos(GetCurrentUserId());
            return Json(popularPhotos);
        }



        [HttpGet("{photoId}")]
        public async Task<IActionResult> GetPhotoDetails(Guid photoId)
        {
            logger.LogInformation($"{nameof(GetPhotoDetails)}({nameof(photoId)} = '{photoId}')");
            PhotoModel photo = await photosService.GetPhoto(photoId, GetCurrentUserId());
            return Json(photoModelConverter.ToPublic(photo));
        }

        [HttpDelete("{photoId}")]
        [Authorize]
        public async Task<IActionResult> DeletePhoto(Guid photoId)
        {
            logger.LogInformation($"{nameof(DeletePhoto)}({nameof(photoId)} = '{photoId}')");
            UserId currentUserId = GetCurrentUserId();
            PhotoModel photo = await photosService.GetPhoto(photoId, currentUserId);

            if (!UserMayDeletePhoto(photo, currentUserId))
            {
                return new UnauthorizedResult();
            }

            await photosService.DeletePhoto(photoId);

            return Ok();
        }

        [HttpGet("{photoId}/state")]
        public async Task<IActionResult> GetPhotoState(Guid photoId)
        {
            logger.LogInformation($"{nameof(GetPhotoState)}({nameof(photoId)} = '{photoId}')");

            try
            {
                var state = await photosService.GetPhotoState(photoId);
                logger.LogInformation($"State is '{state.ToString()}'");
                return Json(state.ToString());
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in {nameof(GetPhotoState)}({nameof(photoId)} = '{photoId}'):\n{ex.ToString()}");
                throw;
            }
        }


        [HttpPut("{photoId}/text")]
        [Authorize]
        public async Task<IActionResult> SetPhotoText(Guid photoId, [FromBody] string text)
        {
            logger.LogInformation($"{nameof(SetPhotoText)}({nameof(photoId)} = {photoId}, {nameof(text)} = '{text}')");
            try
            {
                var htmlText = await photosService.SetPhotoText(photoId, GetCurrentUserId(), text);
                return Json(htmlText);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in {nameof(SetPhotoText)}({nameof(photoId)} = {photoId}, {nameof(text)} = '{text}'):\n{ex.ToString()}");
                return BadRequest();
            }
        }


        [HttpPost("{photoId}/comment")]
        [Authorize]
        public async Task<IActionResult> AddPhotoComment(Guid photoId, [FromBody] string text)
        {
            logger.LogInformation($"{nameof(AddPhotoComment)}({nameof(photoId)} = {photoId}, {nameof(text)} = '{text}')");
            try
            {
                await feedbackService.AddPhotoComment(photoId, GetCurrentUserId(), GetCurrentUserName(), text);
                return Json("OK");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in {nameof(AddPhotoComment)}({nameof(photoId)} = {photoId}, {nameof(text)} = '{text}'):\n{ex.ToString()}");
                return BadRequest();
            }
        }

        [HttpDelete("{photoId}/comment")]
        [Authorize]
        public async Task<IActionResult> DeletePhotoComment([FromBody] PublicPhotoComment comment)
        {
            logger.LogInformation($"{nameof(DeletePhotoComment)}({nameof(comment.PhotoId)} = {comment.PhotoId})");
            try
            {
                if (!GetCurrentUserId().Equals(comment.UserId))
                {
                    return Forbid();
                }

                await feedbackService.DeletePhotoComment(comment.FromPublic());
                return Json("OK");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in {nameof(DeletePhotoComment)}({nameof(comment.PhotoId)} = {comment.PhotoId}):\n{ex.ToString()}");
                return BadRequest();
            }
        }

        [HttpGet("{photoId}/comment")]
        public async Task<IActionResult> GetPhotoComments(Guid photoId)
        {
            logger.LogInformation($"{nameof(GetPhotoComments)}({nameof(photoId)} = {photoId})");
            try
            {
                IEnumerable<PhotoComment> comments = await feedbackService.GetPhotoComments(photoId);
                return Json(comments.OrderBy(x => x.CreatedTime).Select(x => x.ToPublic()));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in {nameof(GetPhotoComments)}({nameof(photoId)} = {photoId}):\n{ex.ToString()}");
                return BadRequest();
            }
        }


        [HttpPut("{photoId}/like")]
        [Authorize]
        public Task<IActionResult> LikePhoto(Guid photoId)
        {
            logger.LogInformation($"{nameof(LikePhoto)}()");
            return SetLikeState(photoId, GetCurrentUserId(), true);
        }

        [HttpPut("{photoId}/unlike")]
        [Authorize]
        public Task<IActionResult> UnlikePhoto(Guid photoId)
        {
            logger.LogInformation($"{nameof(UnlikePhoto)}()");
            return SetLikeState(photoId, GetCurrentUserId(), false);
        }

        private async Task<IActionResult> SetLikeState(PhotoId photoId, UserId userId, bool likeState)
        {
            try
            {
                await feedbackService.SetLikeState(photoId, userId, likeState);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in {nameof(SetLikeState)}({photoId})");
                throw;
            }

            return Json("OK");
        }

        private bool UserMayDeletePhoto(PhotoModel photo, Guid userId)
        {
            return photo.UserId.Equals(userId);
        }

        private UserId GetCurrentUserId()
        {
            var userIdAsString = lazyClaims.Value
                .FirstOrDefault(claim => claim.Type == Constants.Security.UserIdClaim)
                ?.Value;

            Guid userId;
            if (string.IsNullOrEmpty(userIdAsString) || !Guid.TryParse(userIdAsString, out userId))
            {
                return Guid.Empty;
            }

            return userId;
        }

        private string GetCurrentUserName()
        {
            return lazyClaims.Value.FirstOrDefault(c => c.Type.Equals("name", StringComparison.OrdinalIgnoreCase))?.Value;
        }

        private Claim[] GetUserClaims()
        {
            var claims = User.Identities
                .FirstOrDefault()
                ?.Claims
                ?.ToArray();

            logger.LogInformation($"User claims:\n{claims.Select(c => $"{c.Type} = '{c.Value}'\n").ToArray()}");

            return claims;
        }
    }
}
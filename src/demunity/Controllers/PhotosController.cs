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
using demunity.Models.Converters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace demunity.Controllers
{
    [Route("photos")]
    public class PhotosController : Controller
    {
        private readonly IPhotosService photosService;
        private readonly IUsersService usersService;
        private readonly ISystem system;
        private readonly ILogWriter<PhotosController> logger;
        private readonly Lazy<Claim[]> lazyClaims;
        private readonly PhotoModelConverter photoModelConverter;

        public PhotosController(
            IPhotosService photosService,
            IUsersService usersService,
            ITextSplitter textSplitter,
            ISystem system,
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
            this.usersService = usersService ?? throw new ArgumentNullException(nameof(usersService));
            this.system = system ?? throw new ArgumentNullException(nameof(system));
            logger = loggerFactory.CreateLogger<PhotosController>();
            lazyClaims = new Lazy<Claim[]>(() => GetUserClaims());


            var staticAssetHost = new Uri(system.Environment.GetVariable(Constants.EnvironmentVariables.StaticAssetHost));
            this.photoModelConverter = new PhotoModelConverter(staticAssetHost, textSplitter, loggerFactory);
            AWSSDKHandler.RegisterXRayForAllServices();
        }



        [HttpGet("")]
        public IActionResult Get()
        {
            logger.LogInformation($"{nameof(Get)}()");
            return RedirectToAction("Latest");
        }


        [HttpGet("new")]
        [Authorize]
        public IActionResult New()
        {
            logger.LogInformation($"{nameof(New)}()");
            return View();
        }


        [HttpGet("manynew")]
        [Authorize]
        public IActionResult ManyNew()
        {
            logger.LogInformation($"{nameof(New)}()");
            return View();
        }


        [HttpGet("latest")]
        public async Task<IActionResult> Latest()
        {
            logger.LogInformation($"{nameof(Latest)}()");
            IEnumerable<PhotoModel> latestPhotos = await photosService.GetLatestPhotos(GetCurrentUserId());
            UserId userId = GetCurrentUserId();

            return View(latestPhotos.Select(x => photoModelConverter.ToPublic(x, userId.Equals(Guid.Empty) ? null : (Guid?)userId)));
        }



        [HttpGet("popular")]
        public async Task<IActionResult> Popular()
        {
            logger.LogInformation($"{nameof(Popular)}()");
            IEnumerable<PhotoModel> popularPhotos = await photosService.GetPopularPhotos(GetCurrentUserId());
            return View(popularPhotos);
        }



        [HttpGet("{photoId}")]
        public async Task<IActionResult> PhotoDetails(Guid photoId)
        {
            logger.LogInformation($"{nameof(PhotoDetails)}({nameof(photoId)} = '{photoId}')");
            var userId = GetCurrentUserId();
            PhotoModel photo = await photosService.GetPhoto(photoId, userId);
            if (photo == null)
            {
                return NotFound();
            }

            bool photoIsOwnedByCurrentUser = userId == photo.UserId;
            logger.LogInformation($"{nameof(userId)} = '{userId}', {nameof(photo.UserId)} = '{photo.UserId}'. Photo {(photoIsOwnedByCurrentUser ? "is" : "is NOT")} owned by current user.");

            // If the state is not PhotoAvailable, show it only if it's owned by the current user
            if (photo.State != PhotoState.PhotoAvailable && !photoIsOwnedByCurrentUser)
            {
                return NotFound();
            }

            return base.View(photoModelConverter.ToPublic(photo, userId.Equals(Guid.Empty) ? null : (Guid?)userId));
        }

        [HttpGet("{fileName}/uploadurl")]
        public async Task<IActionResult> GetUploadUrl(string fileName)
        {
            logger.LogInformation($"{nameof(GetUploadUrl)}()");
            string uploadUrl = await photosService.GetUploadUrl(fileName);
            return Json(uploadUrl);
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

        private Claim[] GetUserClaims()
        {
            return User.Identities
                .FirstOrDefault()
                ?.Claims
                ?.ToArray();
        }
    }
}
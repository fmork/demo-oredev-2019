using System;
using System.Collections.Generic;
using System.Linq;
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

namespace demunity.Controllers
{
    [Route("users")]
    [Authorize]
    public class UsersController : Controller
    {
        private readonly IUsersService usersService;
        private readonly IPhotosService photosService;
        private readonly PhotoModelConverter photoModelConverter;

        public UsersController(
            IUsersService usersService,
            IPhotosService photosService,
            ITextSplitter textSplitter,
            ISystem system,
            ILogWriterFactory logWriterFactory)
        {
            if (textSplitter is null)
            {
                throw new ArgumentNullException(nameof(textSplitter));
            }

            if (system is null)
            {
                throw new ArgumentNullException(nameof(system));
            }

            if (logWriterFactory is null)
            {
                throw new ArgumentNullException(nameof(logWriterFactory));
            }

            this.usersService = usersService ?? throw new ArgumentNullException(nameof(usersService));
            this.photosService = photosService ?? throw new ArgumentNullException(nameof(photosService));

            var imageAssetHost = new Uri(system.Environment.GetVariable(Constants.EnvironmentVariables.StaticAssetHost));
            this.photoModelConverter = new PhotoModelConverter(imageAssetHost, textSplitter, logWriterFactory);

            AWSSDKHandler.RegisterXRayForAllServices();
        }

        [HttpGet("")]
        [AllowAnonymous]
        public IActionResult Get()
        {
            return RedirectToAction("me");
        }

        [HttpGet("me")]
        public async System.Threading.Tasks.Task<IActionResult> Me()
        {
            var userId = GetCurrentUserId();
            UserModel user = await usersService.GetUserById(userId);
            IEnumerable<PhotoModel> photos = await photosService.GetPhotosByUser(userId, userId);
            return View(new PublicUserWithPhotos
            {
                User = user.ToPublic(),
                Photos = photos.Select(photoModelConverter.ToPublic)
            });
        }

        [HttpGet("{userId}")]
        [AllowAnonymous]
        public async Task<IActionResult> UserProfile(Guid userId)
        {
            UserModel user = await usersService.GetUserById(userId);
            if (user == UserModel.Null)
            {
                return NotFound();
            }

            IEnumerable<PhotoModel> photos = await photosService.GetPhotosByUser((UserId)userId, GetCurrentUserId());
            return View(new PublicUserWithPhotos
            {
                User = user.ToPublic(),
                Photos = photos.Select(photoModelConverter.ToPublic)
            });
        }


        private Guid GetCurrentUserId()
        {
            var userIdAsString = User.Identities
                .FirstOrDefault()
                ?.Claims
                .FirstOrDefault(claim => claim.Type == Constants.Security.UserIdClaim)
                ?.Value;

            Guid userId;
            if (string.IsNullOrEmpty(userIdAsString) || !Guid.TryParse(userIdAsString, out userId))
            {
                return Guid.Empty;
            }

            return userId;
        }
    }
}
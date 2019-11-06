using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Amazon.XRay.Recorder.Handlers.AwsSdk;
using demunity.lib;
using demunity.lib.Data.Models;
using demunity.lib.Logging;
using demunity.lib.Text;
using demunity.Models.Converters;
using Microsoft.AspNetCore.Mvc;

namespace demunity.Controllers
{
    [Route("tags")]
    public class TagsController : Controller
    {
        private readonly IPhotosService photosService;
        private readonly PhotoModelConverter photoModelConverter;
        private readonly ITextSplitter textSplitter;
        private readonly ILogWriterFactory loggerFactory;
        private readonly Lazy<Claim[]> lazyClaims;

        public TagsController(
            IPhotosService photosService,
            ITextSplitter textSplitter,
            ISystem system,
            ILogWriterFactory loggerFactory)
        {
            if (system is null)
            {
                throw new ArgumentNullException(nameof(system));
            }

            this.textSplitter = textSplitter ?? throw new ArgumentNullException(nameof(textSplitter));
            this.loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            this.photosService = photosService ?? throw new System.ArgumentNullException(nameof(photosService));

            var imageAssetHost = new Uri(system.Environment.GetVariable(Constants.EnvironmentVariables.StaticAssetHost));
            this.photoModelConverter = new PhotoModelConverter(imageAssetHost, textSplitter, loggerFactory);
            lazyClaims = new Lazy<Claim[]>(() => GetUserClaims());

            AWSSDKHandler.RegisterXRayForAllServices();
        }


        [HttpGet]
        public IActionResult Index()
        {
            return RedirectToAction("Index", "Home");
        }


        [HttpGet("{hashtag}")]
        public async Task<IActionResult> PhotosByTag(string hashtag)
        {
            if (!hashtag.StartsWith("#"))
            {
                hashtag = $"#{hashtag}";
            }

            var photos = await photosService.GetPhotosByHashtag(hashtag, GetCurrentUserId());

            return View(photos.Select(photoModelConverter.ToPublic));
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
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using demunity.Models;
using demunity.lib.Logging;
using demunity.lib;
using Amazon.XRay.Recorder.Handlers.AwsSdk;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authentication;
using demunity.Models.Converters;
using demunity.lib.Text;
using System.Security.Claims;
using demunity.lib.Data.Models;
using Microsoft.AspNetCore.Authorization;

namespace demunity.Controllers
{
    public class HomeController : Controller
    {
        private readonly IPhotosService photosService;
        private readonly ILogWriter<HomeController> logWriter;
        private readonly PhotoModelConverter photoModelConverter;
        private readonly Lazy<Claim[]> lazyClaims;


        public HomeController(
            ILogWriterFactory logWriterFactory,
            IPhotosService photosService,
            ISystem system,
            ITextSplitter textSplitter)
        {
            if (logWriterFactory is null)
            {
                throw new ArgumentNullException(nameof(logWriterFactory));
            }

            if (system is null)
            {
                throw new ArgumentNullException(nameof(system));
            }

            if (textSplitter is null)
            {
                throw new ArgumentNullException(nameof(textSplitter));
            }

            this.photosService = photosService ?? throw new ArgumentNullException(nameof(photosService));
            logWriter = logWriterFactory.CreateLogger<HomeController>();

            var imageAssetHost = new Uri(system.Environment.GetVariable(Constants.EnvironmentVariables.StaticAssetHost));

            this.photoModelConverter = new PhotoModelConverter(imageAssetHost, textSplitter, logWriterFactory);
            lazyClaims = new Lazy<Claim[]>(() => GetUserClaims());

            AWSSDKHandler.RegisterXRayForAllServices();
        }

        public async Task<IActionResult> Index()
        {
            var latestPhotos = await photosService.GetPopularPhotos(GetCurrentUserId());
            var likedPhotos = latestPhotos.Where(x => x.PhotoIsLikedByCurrentUser);
            if (likedPhotos.Any())
            {
                logWriter.LogInformation($"User likes photos {string.Join(", ", likedPhotos.Select(x => x.PhotoId.ToDbValue()))}");
            }
            return View(latestPhotos.Select(photoModelConverter.ToPublic).ToArray());
        }

        [ResponseCache(Location = ResponseCacheLocation.Any, Duration = 120)]
        public IActionResult About()
        {
            return View();
        }

        [ResponseCache(Location = ResponseCacheLocation.Any, Duration = 120)]
        public IActionResult Contact()
        {
            return View();
        }

        [ResponseCache(Location = ResponseCacheLocation.Any, Duration = 120)]
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {

            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier, HttpStatusCode = HttpContext.Response.StatusCode });
        }

        [HttpGet("error/{httpCode}")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult ErrorWithCode(int httpCode)
        {

            return View(new ErrorViewModel { HttpStatusCode = httpCode });
        }


        [HttpGet("signout")]
        public IActionResult Signout()
        {
            logWriter.LogInformation($"{nameof(Signout)}");
            return SignOut(
                new AuthenticationProperties()
                {
                    RedirectUri = Url.Page("/SignedOut", pageHandler: null, values: null, protocol: Request.Scheme)
                },
                new[] {
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    OpenIdConnectDefaults.AuthenticationScheme
                });
        }

        [HttpGet("signin")]
        [Authorize]
        public IActionResult Signin()
        {
            logWriter.LogInformation($"{nameof(Signin)}");
            return Redirect("/");
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

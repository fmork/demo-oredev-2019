using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using demunity.lib;
using demunity.lib.Data.Models;
using demunity.lib.Logging;
using demunity.lib.Security;
using demunity.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace demunity.Controllers.Api
{
    [Route("api/users")]

    public class UsersApiController : Controller
    {
        private readonly IUsersService usersService;
        private readonly ILogWriter<UsersApiController> logger;

        public UsersApiController(
            IUsersService usersService,
            ILogWriterFactory logWriterFactory)
        {
            if (logWriterFactory is null)
            {
                throw new ArgumentNullException(nameof(logWriterFactory));
            }

            this.usersService = usersService ?? throw new System.ArgumentNullException(nameof(usersService));
            this.logger = logWriterFactory.CreateLogger<UsersApiController>();
        }

        [HttpPut("onlineprofile")]
        [Authorize]
        public async Task<IActionResult> AddOnlineProfile([FromBody] OnlineProfile profile)
        {
            var userId = GetCurrentUserId();

            if (profile == null)
            {
                logger.LogInformation($"Call to {nameof(AddOnlineProfile)} with null input (user {userId})");
                return BadRequest();
            }

            IEnumerable<OnlineProfile> profiles = await usersService.AddSocialProfile(profile, userId);
            return Json(profiles.Select(x => x.ToPublic()));
        }

        [HttpDelete("onlineprofile")]
        [Authorize]
        public async Task<IActionResult> DeleteOnlineProfile([FromBody] OnlineProfile profile)
        {
            var userId = GetCurrentUserId();
            IEnumerable<OnlineProfile> profiles = await usersService.DeleteSocialProfile(profile, userId);
            return Json(profiles.Select(x => x.ToPublic()));
        }

        [HttpGet("{userId}/onlineprofiles")]
        public async Task<IActionResult> GetOnlineProfiles(Guid userId)
        {
            var user = await usersService.GetUserById(userId);
            return Json(user.OnlineProfiles.Select(x => x.ToPublic()));
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
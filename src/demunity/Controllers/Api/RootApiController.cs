using System;
using demunity.lib;
using Microsoft.AspNetCore.Mvc;

namespace demunity.Controllers.Api
{
    [Route("api")]
    public class RootApiController : Controller
    {
        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return Json(DateTimeOffset.UtcNow.ToString(Constants.DateTimeFormatWithMilliseconds));
        }
    }
}
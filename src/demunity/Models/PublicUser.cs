using System;
using System.Collections.Generic;
using demunity.lib.Data.Models;

namespace demunity.Models
{

    public class PublicOnlineProfile
    {
        public string Type { get; set; }
        public string Profile { get; set; }
    }

    public class PublicUser
    {
        public UserId UserId { get; internal set; }
        public string Name { get; internal set; }
        public string Email { get; internal set; }
        public IEnumerable<PublicOnlineProfile> OnlineProfiles { get; set; }
        public DateTimeOffset CreatedTime { get; internal set; }
    }
}
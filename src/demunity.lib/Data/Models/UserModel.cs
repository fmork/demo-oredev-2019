using System;
using System.Collections.Generic;

namespace demunity.lib.Data.Models
{
    public enum OnlineProfileType
    {
        Undefined,
        Twitter,
        Instagram,
        Web
    }


    public class OnlineProfile
    {

        public OnlineProfileType Type { get; set; }
        public string Profile { get; set; }
    }
    public class UserWithData
    {
        public UserModel User { get; set; }
        public IEnumerable<PhotoModel> Photos { get; set; }
    }

    public class UserModel
    {
        private static readonly UserModel nullUserModel = new UserModel
        {
            Id = Guid.Empty,
            Email = string.Empty,
            Name = string.Empty
        };

        public static UserModel Null => nullUserModel;

        public UserId Id { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
        public DateTimeOffset CreatedTime { get; set; }
        public IEnumerable<OnlineProfile> OnlineProfiles { get; set; } = new OnlineProfile[] { };
    }
}
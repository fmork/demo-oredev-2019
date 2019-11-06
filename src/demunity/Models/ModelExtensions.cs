using System.Linq;
using demunity.lib.Data.Models;

namespace demunity.Models
{
    public static class ModelExtensions
    {
        public static PublicPhotoComment ToPublic(this PhotoComment input)
        {
            return new PublicPhotoComment
            {
                PhotoId = input.PhotoId,
                UserId = input.UserId,
                UserName = input.UserName,
                Text = input.Text,
                Time = input.CreatedTime.UtcDateTime
            };
        }

        public static PhotoComment FromPublic(this PublicPhotoComment input)
        {
            return new PhotoComment
            {
                CreatedTime = input.Time,
                PhotoId = input.PhotoId,
                Text = input.Text,
                UserId = input.UserId,
                UserName = input.UserName
            };
        }

        public static PublicUser ToPublic(this UserModel input)
        {
            return new PublicUser
            {
                UserId = input.Id,
                Name = input.Name,
                Email = input.Email,
                CreatedTime = input.CreatedTime,
                OnlineProfiles = input.OnlineProfiles.Select(x => x.ToPublic())
            };
        }

        public static PublicOnlineProfile ToPublic(this OnlineProfile input)
        {
            return new PublicOnlineProfile
            {
                Type = input.Type.ToString(),
                Profile = input.Profile
            };
        }
    }
}
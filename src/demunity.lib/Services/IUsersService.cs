using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using demunity.lib.Data;
using demunity.lib.Data.Models;
using demunity.lib.Logging;

namespace demunity.lib.Security
{
    public interface IUsersService
    {
        Task<UserModel> FindUserByEmail(string email);
        Task<UserModel> CreateUser(UserId userID, string name, string email);
        Task<UserModel> GetUserById(Guid userId);
        Task<IEnumerable<OnlineProfile>> AddSocialProfile(OnlineProfile profile, Guid userId);
        Task<IEnumerable<OnlineProfile>> DeleteSocialProfile(OnlineProfile profile, Guid userId);
    }

    public class UsersService : IUsersService
    {
        private readonly IUserRepository userRepository;
        private readonly ILogWriter<UsersService> logWriter;

        public UsersService(
            IUserRepository userRepository,
            ILogWriterFactory logWriterFactory)
        {
            this.userRepository = userRepository;
            logWriter = logWriterFactory.CreateLogger<UsersService>();
        }

        public Task<IEnumerable<OnlineProfile>> AddSocialProfile(OnlineProfile profile, Guid userId)
        {
            if (profile.Type == OnlineProfileType.Instagram || profile.Type == OnlineProfileType.Twitter)
            {
                // strip @-prefix from profile name for storage
                profile.Profile = profile.Profile.TrimStart('@');
            }
            else if (profile.Type == OnlineProfileType.Web)
            {
                // default to https:// if protocol is missing
                if (!profile.Profile.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                    && !profile.Profile.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    profile.Profile = $"https://{profile.Profile}";
                }
            }

            return userRepository.AddSocialProfile(profile, userId);
        }

        public Task<UserModel> CreateUser(UserId userId, string name, string email)
        {
            logWriter.LogInformation($"{nameof(CreateUser)}({nameof(userId)} = '{userId}', {nameof(name)} = '{name}', {nameof(email)} = '{email}')");
            var newUser = new UserModel
            {
                Id = userId,
                Email = email,
                Name = name,
                CreatedTime = DateTimeOffset.UtcNow
            };

            return userRepository.CreateUser(newUser);
        }

        public Task<IEnumerable<OnlineProfile>> DeleteSocialProfile(OnlineProfile profile, Guid userId)
        {
            return userRepository.DeleteSocialProfile(profile, userId);
        }

        public Task<UserModel> FindUserByEmail(string email)
        {
            logWriter.LogInformation($"{nameof(FindUserByEmail)}({nameof(email)} = '{email}')");
            return userRepository.FindUserByEmail(email);
        }

        public Task<UserModel> GetUserById(Guid userId)
        {
            return userRepository.GetUserById(userId);
        }
    }
}
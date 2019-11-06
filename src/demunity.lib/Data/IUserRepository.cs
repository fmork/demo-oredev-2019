using System.Collections.Generic;
using System.Threading.Tasks;
using demunity.lib.Data.Models;

namespace demunity.lib.Data
{
    public interface IUserRepository
    {
        Task<UserModel> CreateUser(UserModel user);
        Task<UserModel> FindUserByEmail(string email);
        Task<UserModel> GetUserById(UserId userId);
        Task<IEnumerable<OnlineProfile>> AddSocialProfile(OnlineProfile socialProfile, UserId userId);
        Task<IEnumerable<OnlineProfile>> DeleteSocialProfile(OnlineProfile profile, UserId userId);
    }
}
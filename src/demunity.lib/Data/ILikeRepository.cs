using System.Threading.Tasks;
using demunity.lib.Data.Models;

namespace demunity.lib.Data
{
    public interface ILikeRepository
    {
        Task SetLikeState(PhotoId photoId, UserId userId, bool like);

    }
}
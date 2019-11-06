using System.Collections.Generic;
using System.Threading.Tasks;
using demunity.lib.Data.Models;

namespace demunity.lib
{
    public interface IFeedbackService
    {
        Task SetLikeState(PhotoId photoId, UserId userId, bool likeState);
        Task AddPhotoComment(PhotoId photoId, UserId userId, string userName, string text);
        Task<IEnumerable<PhotoComment>> GetPhotoComments(PhotoId photoId);
        Task DeletePhotoComment(PhotoComment photoComment);

    }
}

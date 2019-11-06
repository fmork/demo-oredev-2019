using System.Collections.Generic;
using System.Threading.Tasks;
using demunity.lib.Data.Models;

namespace demunity.lib.Data
{
    public interface ICommentRepository
    {
        Task AddPhotoComment(PhotoComment comment);
        Task<IEnumerable<PhotoComment>> GetComments(PhotoId photoId);
        Task DeletePhotoComment(PhotoComment comment);
    }
}
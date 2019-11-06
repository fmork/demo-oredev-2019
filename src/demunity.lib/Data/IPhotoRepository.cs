using System.Collections.Generic;
using System.Threading.Tasks;
using demunity.lib.Data.Models;

namespace demunity.lib.Data
{
    public interface IPhotoRepository
    {
        Task<PhotoModel> CreatePhoto(PhotoModel photo);
        Task DeletePhoto(PhotoId photoId);
        Task<IEnumerable<PhotoModel>> GetLatestPhotos(UserId currentUserId);
        Task<PhotoModel> GetPhotoById(PhotoId photoId, UserId currentUserId);
        Task<IEnumerable<PhotoModel>> GetPhotosByHashtag(UserId currentUserId, HashtagModel hashtag);
        Task<IEnumerable<PhotoModel>> GetPhotosByUser(UserId userId, UserId currentUserId);
        Task<PhotoState> GetPhotoState(PhotoId photoId);
        Task<IEnumerable<PhotoWithCommentsAndLikes>> GetPhotosWithCommentsAndLikesForScoring(System.DateTimeOffset dateTimeOffset);
        Task<IEnumerable<PhotoModel>> GetPopularPhotos(UserId currentUserId);
        Task SetPhotoState(UserId userId, PhotoId photoId, PhotoState photoState);
        Task SetPhotoText(UserId userId, PhotoId photoId, string text, IEnumerable<HashtagModel> hashtags);
        Task UpdatePhoto(PhotoModel photo);
        Task UpdateScores(IEnumerable<PhotoScore> scores);
    }
}
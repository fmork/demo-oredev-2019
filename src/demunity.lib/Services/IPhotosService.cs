using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using demunity.lib.Data.Models;
using demunity.lib.Model;

namespace demunity.lib
{
    public interface IPhotosService
    {
        Task<PhotoModel> CreatePhoto(UserId userId, string userName, string fileName, string text);
        Task<IEnumerable<PhotoModel>> GetLatestPhotos(UserId currentUserId);
        Task<PhotoModel> GetPhoto(PhotoId photoId, UserId currentUserId);
        Task ReportPhoto(Guid photoId, string userName, ReportReason reason);
        Task<IEnumerable<PhotoWithCommentsAndLikes>> GetPhotosWithCommentsAndLikes(DateTimeOffset dateTimeOffset);
        Task<IEnumerable<PhotoModel>> GetPopularPhotos(UserId currentUserId);
        Task<string> GetUploadUrl(string fileName);
        Task<IEnumerable<PhotoModel>> GetPhotosByHashtag(string hashtag, UserId currentUserId);
        Task SetPhotoState(UserId userId, PhotoId photoId, PhotoState photoState);
        Task<PhotoState> GetPhotoState(PhotoId photoId);
        Task UpdatePhoto(PhotoModel photoFromDb);
        Task DeletePhoto(PhotoId photoId);
        Task<string> SetPhotoText(PhotoId photoId, UserId userId, string text);
        Task<IEnumerable<PhotoModel>> GetPhotosByUser(UserId userId, UserId currentUserId);
        Task UpdateScores(IEnumerable<PhotoScore> scores);
    }
}

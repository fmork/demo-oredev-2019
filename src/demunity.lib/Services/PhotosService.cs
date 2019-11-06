using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using demunity.lib.Data;
using demunity.lib.Data.Models;
using demunity.lib.Extensions;
using demunity.lib.Logging;
using demunity.lib.Model;
using demunity.lib.Text;

namespace demunity.lib
{
    public class PhotosService : IPhotosService
    {
        private readonly ILogWriter<PhotosService> logWriter;
        private readonly IPhotoRepository dataRepository;
        private readonly IRemoteFileRepository remoteFileRepository;
        private readonly ITextSplitter textSplitter;

        public PhotosService(
            IPhotoRepository dataRepository,
            IRemoteFileRepository remoteFileRepository,
            ITextSplitter textSplitter,
            ILogWriterFactory logWriterFactory)
        {
            if (logWriterFactory is null)
            {
                throw new ArgumentNullException(nameof(logWriterFactory));
            }

            logWriter = logWriterFactory.CreateLogger<PhotosService>();

            this.dataRepository = dataRepository ?? throw new ArgumentNullException(nameof(dataRepository));
            this.remoteFileRepository = remoteFileRepository ?? throw new ArgumentNullException(nameof(remoteFileRepository));
            this.textSplitter = textSplitter ?? throw new ArgumentNullException(nameof(textSplitter));
        }

        public async Task<PhotoModel> CreatePhoto(UserId userId, string userName, string fileName, string text)
        {
            logWriter.LogInformation($"{nameof(CreatePhoto)}({nameof(userId)} = '{userId}', {nameof(fileName)} = '{fileName}')");

            try
            {
                Guid photoId = Guid.NewGuid();
                DateTime utcNow = DateTime.UtcNow;
                var photo = new PhotoModel
                {
                    CreatedTime = utcNow,
                    Filename = fileName,
                    UserId = userId,
                    UserName = userName,
                    PhotoId = photoId,
                    ObjectKey = $"{userId}/{photoId}",
                    State = PhotoState.Created,
                    Sizes = new Size[] { },
                    RawText = text,
                    Hashtags = GetHashtags(photoId, text, utcNow)
                };

                photo.UploadUri = await remoteFileRepository.GetUploadUri($"{photo.ObjectKey}{Path.GetExtension(fileName)}");
                return await dataRepository.CreatePhoto(photo);
            }
            catch (Exception ex)
            {
                logWriter.LogError(ex, $"Error in {nameof(CreatePhoto)}({nameof(userId)} = '{userId}', {nameof(fileName)} = '{fileName}')");
                throw new Exception("Error when creating photo.", ex);
            }
        }

        public Task<IEnumerable<PhotoModel>> GetLatestPhotos(UserId currentUserId)
        {
            logWriter.LogInformation($"{nameof(GetLatestPhotos)}()");
            return dataRepository.GetLatestPhotos(currentUserId);
        }

        public Task<PhotoModel> GetPhoto(PhotoId photoId, UserId currentUserId)
        {
            logWriter.LogInformation($"{nameof(GetPhoto)}({nameof(photoId)} = {photoId})");
            return dataRepository.GetPhotoById(photoId, currentUserId);
        }

        public Task<IEnumerable<PhotoModel>> GetPopularPhotos(UserId currentUserId)
        {
            logWriter.LogInformation($"{nameof(GetPopularPhotos)}()");
            return dataRepository.GetPopularPhotos(currentUserId);
        }

        public Task<PhotoState> GetPhotoState(PhotoId photoId)
        {
            logWriter.LogInformation($"{nameof(GetPhotoState)}({nameof(photoId)} = '{photoId}')");
            return dataRepository.GetPhotoState(photoId);
        }

        public Task<string> GetUploadUrl(string fileName)
        {
            logWriter.LogInformation($"{nameof(GetUploadUrl)}({nameof(fileName)} = '{fileName}')");
            return Task.FromResult((string)null);
        }

        public Task ReportPhoto(Guid photoId, string userName, ReportReason reason)
        {
            logWriter.LogInformation($"{nameof(ReportPhoto)}({nameof(photoId)} = '{photoId}', {nameof(userName)} = '{userName}', {nameof(reason)} = {reason})");
            return Task.CompletedTask;
        }



        public Task SetPhotoState(UserId userId, PhotoId photoId, PhotoState photoState)
        {
            logWriter.LogInformation($"{nameof(SetPhotoState)}({nameof(userId)} = '{userId}', {nameof(photoId)} = '{photoId}', {nameof(photoState)} = {photoState})");
            return dataRepository.SetPhotoState(userId, photoId, photoState);

        }



        public Task UpdatePhoto(PhotoModel photo)
        {
            logWriter.LogInformation($"{nameof(UpdatePhoto)}({nameof(photo.PhotoId)} = '{photo.PhotoId}'");
            return dataRepository.UpdatePhoto(photo);
        }

        public Task DeletePhoto(PhotoId photoId)
        {
            logWriter.LogInformation($"{nameof(DeletePhoto)}({nameof(photoId)} = '{photoId}'");
            return dataRepository.DeletePhoto(photoId);
        }

        public async Task<string> SetPhotoText(PhotoId photoId, UserId userId, string text)
        {
            logWriter.LogInformation($"{nameof(SetPhotoText)}({nameof(photoId)} = '{photoId}', {nameof(userId)} = '{userId}', {nameof(text)} = '{text}')");
            var utcNow = DateTimeOffset.UtcNow;
            IEnumerable<HashtagModel> hashtags = GetHashtags(photoId, text, utcNow);

            await dataRepository.SetPhotoText(userId, photoId, text, hashtags);

            return text.GetHtmlText(textSplitter);

        }

        private IEnumerable<HashtagModel> GetHashtags(PhotoId photoId, string text, DateTimeOffset utcNow)
        {
            return textSplitter.Split(text, TextPatterns.Hashtags)
                .Where(x => x.ItemType == TextItemType.HashTag)
                .Select(x => new HashtagModel { PhotoId = photoId, Hashtag = x.Text, CreatedTime = utcNow });
        }


        public Task<IEnumerable<PhotoModel>> GetPhotosByUser(UserId userId, UserId currentUserId)
        {
            return dataRepository.GetPhotosByUser(userId, currentUserId);
        }

        public Task<IEnumerable<PhotoWithCommentsAndLikes>> GetPhotosWithCommentsAndLikes(DateTimeOffset dateTimeOffset)
        {
            return dataRepository.GetPhotosWithCommentsAndLikesForScoring(dateTimeOffset);
        }

        public Task UpdateScores(IEnumerable<PhotoScore> scores)
        {
            logWriter.LogInformation($"{nameof(UpdateScores)}");
            return dataRepository.UpdateScores(scores)
                .ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        AggregateException exception = t.Exception.Flatten();
                        logWriter.LogError(exception, $"{nameof(UpdateScores)}:\n{exception.ToString()}");
                    }
                });
        }

        public Task<IEnumerable<PhotoModel>> GetPhotosByHashtag(string hashtag, UserId currentUserId)
        {
            logWriter.LogInformation($"{nameof(GetPhotosByHashtag)}({nameof(hashtag)} = '{hashtag}')");
            return dataRepository.GetPhotosByHashtag(currentUserId, new HashtagModel { Hashtag = hashtag });
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using demunity.lib.Data;
using demunity.lib.Data.Models;
using demunity.lib.Logging;

namespace demunity.lib
{
    public class FeedbackService : IFeedbackService
    {
        private readonly ILogWriter<FeedbackService> logWriter;
        private readonly ILikeRepository likeRepository;
        private readonly ICommentRepository commentRepository;

        public FeedbackService(
            ILikeRepository likeRepository,
            ICommentRepository commentRepository,
            ILogWriterFactory logWriterFactory)
        {
            logWriter = logWriterFactory.CreateLogger<FeedbackService>();
            this.likeRepository = likeRepository ?? throw new System.ArgumentNullException(nameof(likeRepository));
            this.commentRepository = commentRepository ?? throw new System.ArgumentNullException(nameof(commentRepository));
        }

        public async Task AddPhotoComment(PhotoId photoId, UserId userId, string userName, string text)
        {
            logWriter.LogInformation($"{nameof(AddPhotoComment)}({nameof(photoId)} = '{photoId}', {nameof(userId)} = '{userId}', {nameof(userName)} = '{userName}', {nameof(text)} = '{text}')");

            if (string.IsNullOrWhiteSpace(text))
            {
                throw new Exception("Cannot add empty comments.");
            }

            PhotoComment comment = new PhotoComment
            {
                PhotoId = photoId,
                UserId = userId,
                UserName = userName,
                Text = text,
                CreatedTime = DateTimeOffset.UtcNow
            };

            try
            {
                await commentRepository.AddPhotoComment(comment);
            }
            catch (Exception ex)
            {
                logWriter.LogError(ex, $"{nameof(AddPhotoComment)}({nameof(photoId)} = '{photoId}', {nameof(userId)} = '{userId}', {nameof(userName)} = '{userName}', {nameof(text)} = '{text}'):\n{ex.ToString()}");
                throw;
            }
        }

        public Task<IEnumerable<PhotoComment>> GetPhotoComments(PhotoId photoId)
        {
            return commentRepository.GetComments(photoId);
        }

        public Task DeletePhotoComment(PhotoComment comment)
        {
            return commentRepository.DeletePhotoComment(comment);
        }



        public Task SetLikeState(PhotoId photoId, UserId userId, bool likeState)
        {
            logWriter.LogInformation($"{nameof(SetLikeState)}({nameof(photoId)} = '{photoId}', {nameof(userId)} = '{userId}', {nameof(likeState)} = {likeState})");
            return likeRepository.SetLikeState(photoId, userId, likeState);

        }
    }
}

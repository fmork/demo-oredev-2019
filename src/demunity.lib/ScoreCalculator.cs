using System;
using System.Collections.Generic;
using System.Linq;
using demunity.lib.Data.Models;
using demunity.lib.Extensions;
using demunity.lib.Logging;

namespace demunity.lib
{
    public class ScoreCalculator : IScoreCalculator
    {
        private readonly ILogWriter<ScoreCalculator> logger;
        private readonly ISystem system;
        private const double PhotoAgeWeight = 0.1;
        private const int TitleWeight = 2;
        private const int CommentWeight = 1;
        private const int LikeWeight = 2;
        private const int CutoffTimeInHours = 18;

        public ScoreCalculator(
            ISystem system,
            ILogWriterFactory logWriterFactory)
        {
            if (logWriterFactory is null)
            {
                throw new ArgumentNullException(nameof(logWriterFactory));
            }

            this.system = system ?? throw new ArgumentNullException(nameof(system));
            logger = logWriterFactory.CreateLogger<ScoreCalculator>();
            logger.LogInformation($"Created {nameof(ScoreCalculator)} instance");
        }

        public IEnumerable<PhotoScore> CalculateScores(IEnumerable<PhotoWithCommentsAndLikes> photosWithCommentsAndLikes)
        {
            var calculationData = photosWithCommentsAndLikes.ToArray();
            logger.LogInformation($"{nameof(CalculateScores)}({nameof(photosWithCommentsAndLikes)} = {calculationData.Length} items)");

            try
            {
                // var calculationData = await photosService.GetPhotosWithCommentsAndLikes(referenceTime);
                var result = calculationData.Select(item => GetScore(item)).ToArray();

                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in {nameof(CalculateScores)}({nameof(photosWithCommentsAndLikes)} = {calculationData.Length} items):\n{ex.ToString()}");
                throw;
            }
        }

        private PhotoScore GetScore(PhotoWithCommentsAndLikes item)
        {

            double photoScore = GetPhotoScore(item.Photo);

            double commentScore = GetCommentScore(item.Comments.ToArray());
            double likeScore = GetLikeScore(item.Likes.ToArray());
            double totalScore = photoScore + commentScore + likeScore;

            return new PhotoScore
            {
                PhotoId = item.Photo.PhotoId,
                Score = totalScore
            };
        }

        public double GetPhotoScore(PhotoModel item)
        {
            TimeSpan itemAge = system.Time.UtcNow - item.CreatedTime;

            var defaultPhotoScore = GetWeightedScore(itemAge, PhotoAgeWeight);

            var titleScore = !string.IsNullOrWhiteSpace(item.RawText)
                ? GetWeightedScore(itemAge, TitleWeight)
                : 0;


            return titleScore + defaultPhotoScore;
        }

        public double GetLikeScore(params PhotoLikeRecord[] likes)
        {
            DateTimeOffset utcNow = system.Time.UtcNow;

            return likes.Sum(like =>
            {
                TimeSpan itemAge = utcNow - like.CreatedTime;
                return GetWeightedScore(itemAge, LikeWeight);
            });
        }

        public double GetCommentScore(params PhotoComment[] comments)
        {
            DateTimeOffset utcNow = system.Time.UtcNow;

            return comments.Sum(comment =>
            {
                TimeSpan itemAge = utcNow - comment.CreatedTime;
                return GetWeightedScore(itemAge, CommentWeight);
            });
        }

        private static double GetWeightedScore(TimeSpan itemAge, double itemWeight)
        {
            return (Math.Max(CutoffTimeInHours - itemAge.TotalHours, 1) * itemWeight);
        }
    }
}

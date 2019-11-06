using System.Collections.Generic;
using demunity.lib.Data.Models;

namespace demunity.lib
{
    public interface IScoreCalculator
    {
        IEnumerable<PhotoScore> CalculateScores(IEnumerable<PhotoWithCommentsAndLikes> photosWithCommentsAndLikes);
        double GetCommentScore(params PhotoComment[] comments);
        double GetLikeScore(params PhotoLikeRecord[] likes);
        double GetPhotoScore(PhotoModel item);
    }
}

using System.Collections.Generic;

namespace demunity.lib.Data.Models
{
    public class PhotoWithCommentsAndLikes
    {
        public PhotoModel Photo { get; set; }
        public IEnumerable<PhotoComment> Comments { get; set; }
        public IEnumerable<PhotoLikeRecord> Likes { get; set; }
    }
}
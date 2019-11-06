using System;

namespace demunity.lib.Data.Models
{
    public class PhotoLikeRecord
    {
        public PhotoId PhotoId { get; set; }
        public UserId UserId { get; set; }
        public DateTimeOffset CreatedTime { get; set; }
    }
}
using System;

namespace demunity.lib.Data.Models
{
    public class PhotoComment
    {
        public PhotoId PhotoId { get; set; }
        public UserId UserId { get; set; }
        public string UserName { get; set; }
        public string Text { get; set; }
        public DateTimeOffset CreatedTime { get; set; }

    }
}
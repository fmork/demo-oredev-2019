using System;

namespace demunity.Models
{
    public class PublicPhotoComment
    {
        public Guid PhotoId { get; set; }
        public Guid UserId { get; set; }
        public string UserName { get; set; }
        public string Text { get; set; }
        public DateTime Time { get; set; }
    }
}
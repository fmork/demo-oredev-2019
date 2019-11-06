using System;
using System.Collections.Generic;

namespace demunity.Models
{

    public class PhotoUri
    {
        public PhotoUri(int Width, Uri jpgUri, Uri webpUri)
        {
            this.Width = Width;
            JpgUri = jpgUri;
            WebpUri = webpUri;
        }
        public int Width { get; }
        public Uri JpgUri { get; }
        public Uri WebpUri { get; }
    }

    public class PublicPhotoModel
    {

        public Guid Id { get; set; }
        public IEnumerable<PhotoUri> Uris { get; set; }
        public Guid UserId { get; set; }
        public string UserName { get; set; }
        public Guid? CurrentUserId { get; set; }
        public DateTimeOffset CreatedTime { get; set; }
        public string Filename { get; set; }
        public string State { get; set; }
        public int LikeCount { get; set; }
        public int CommentCount { get; set; }
        public bool LikedByCurrentUser { get; set; }
        public Uri UploadUri { get; set; }
        public string Text { get; set; }
        public string HtmlText { get; set; }
        public bool PhotoIsOwnedByCurrentUser { get; set; }
        public bool IsPortraitOrientation { get; set; }
        public double Score { get; set; }
    }
}
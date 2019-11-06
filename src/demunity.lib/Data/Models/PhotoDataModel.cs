using System;
using System.Collections.Generic;
using System.Drawing;

namespace demunity.lib.Data.Models
{
    public class PhotoModel
    {

        public static PhotoModel Null { get; } = new PhotoModel
        {
            PhotoId = Guid.Empty,
            CreatedTime = DateTimeOffset.MinValue,
            ObjectKey = string.Empty,
            UserId = Guid.Empty,
            Filename = string.Empty,
            State = PhotoState.Undefined
        };

        public PhotoId PhotoId { get; set; }
        public string ObjectKey { get; set; }
        public UserId UserId { get; set; }
        public string UserName { get; set; }
        public DateTimeOffset CreatedTime { get; set; }
        public string Filename { get; set; }
        public PhotoState State { get; set; }
        public IEnumerable<Size> Sizes { get; set; } = new Size[] { };
        public Uri UploadUri { get; set; }
        public int LikeCount { get; set; }
        public bool PhotoIsLikedByCurrentUser { get; set; }
        public string RawText { get; set; }
        public double Score { get; set; }
        public IEnumerable<HashtagModel> Hashtags { get; set; } = new HashtagModel[] { };
        public int CommentCount { get; set; }

        public override string ToString()
        {
            return $"[{GetType().Name}: Id={PhotoId}, ObjectKey={ObjectKey}]";
        }

    }

    public enum PhotoState
    {
        Undefined,
        Created,
        UploadStarted,
        UploadCompleted,
        ProcessingStarted,
        PhotoAvailable,
        ProcessingFailed
    }
}
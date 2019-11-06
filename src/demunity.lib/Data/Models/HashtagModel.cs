using System;
using System.Collections.Generic;

namespace demunity.lib.Data.Models
{
    public class HashtagModel
    {
        public PhotoId PhotoId { get; set; }
        public string Hashtag { get; set; }
        public DateTimeOffset CreatedTime { get; set; }

        public override bool Equals(object obj)
        {
            // CreatedTime is not included in equality comparison
            return obj is HashtagModel model &&
                   EqualityComparer<PhotoId>.Default.Equals(PhotoId, model.PhotoId) &&
                   Hashtag == model.Hashtag;
        }

        public override int GetHashCode()
        {
            var hashCode = 1819195551;
            hashCode = hashCode * -1521134295 + EqualityComparer<PhotoId>.Default.GetHashCode(PhotoId);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Hashtag);
            return hashCode;
        }
    }
}
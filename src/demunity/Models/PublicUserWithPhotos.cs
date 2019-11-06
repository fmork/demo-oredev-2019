using System.Collections.Generic;

namespace demunity.Models
{
    public class PublicUserWithPhotos
    {
        public PublicUser User { get; set; }
        public IEnumerable<PublicPhotoModel> Photos { get; set; }
    }
}
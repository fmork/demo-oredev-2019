using System;
using System.Linq;

namespace demunity
{
    public class BackgroundImage
    {
        private readonly string imageFile;
        private readonly string[] backgroundImages = new[] {
            "20150311-212108.jpg",
            "20150311-213056.jpg",
            "20160217-172233.jpg",
            "20160830-201812-3242.jpg",
            "20170117-164345.jpg",
            "20170117-170457-C1.jpg",
            "20170211-164812.jpg",
            "20171029-163030.jpg",
            "20171222-163303-9182.jpg",
            "20180808-201716-9189-HDR.jpg",
            "20181227-171203-6434-HDR.jpg",
            "20181227-172936-6454-HDR-Pano.jpg",
            "20190519-220736-6472.jpg",
            "20190812-213708-2450.jpg",
        };

        public BackgroundImage()
        {
            imageFile = backgroundImages.OrderBy(x => Guid.NewGuid()).ToArray().First();
        }
        public string Filename
        {
            get
            {
                return imageFile;
            }
        }
    }
}
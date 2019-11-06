using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;

namespace demunity.imagesizer.Images
{
    public interface IImageScaler
    {
        Task<IEnumerable<Size>> ScaleImageByWidths(Stream input, Func<Stream, Size, string, Task> storeFunc, IEnumerable<int> widths);
    }
}
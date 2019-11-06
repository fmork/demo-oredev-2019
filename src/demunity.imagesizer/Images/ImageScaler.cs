using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using demunity.lib.Logging;
using ImageMagick;

namespace demunity.imagesizer.Images
{

    public class ImageScaler : IImageScaler
    {


        private const int None = 0;
        private const int Horizontal = 1;
        private const int Vertical = 2;
        private const int DefaultImageQuality = 65;
        private static int[][] RotationOperations = new int[][] {
            new int[] {  0, None},
            new int[] {  0, Horizontal},
            new int[] {180, None},
            new int[] {  0, Vertical},
            new int[] { 90, Horizontal},
            new int[] { 90, None},
            new int[] {-90, Horizontal},
            new int[] {-90, None},
        };


        private readonly ILogWriter<ImageScaler> logger;
        private readonly Stopwatch stopwatch = new Stopwatch();
        public ImageScaler(ILogWriterFactory logWriterFactory)
        {
            if (logWriterFactory == null)
            {
                throw new System.ArgumentNullException(nameof(logWriterFactory));
            }

            logger = logWriterFactory.CreateLogger<ImageScaler>();
        }

        public async Task<IEnumerable<Size>> ScaleImageByWidths(Stream input, Func<Stream, Size, string, Task> storeFunc, IEnumerable<int> widths)
        {
            stopwatch.Start();
            HashSet<Size> sizes = new HashSet<Size>();

            using (var originalImage = GetImageForScaling(input))
            {
                // Images are scaled to the target sizes in descending order. We resize
                // the same MagickImage step by step.
                foreach (var width in widths.OrderByDescending(n => n))
                {
                    // create scaled images, 
                    sizes.Add(await ScaleMagickImageByWidth(originalImage, width, storeFunc));
                }
            }
            stopwatch.Stop();
            return sizes;
        }


        private string GetTimeStampString()
        {
            return $"{stopwatch.ElapsedMilliseconds} ms";
        }

        private MagickImage GetImageForScaling(Stream input)
        {
            var image = new MagickImage(input);

            PrintAttributesToLog(image);
            RotateByExif(image);
            image.Strip();

            logger.LogInformation($"[{GetTimeStampString()}]\tImage prepared.\n\tBaseSize: {image.BaseWidth}x{image.BaseHeight}\n\tSize {image.Width}x{image.Height}");

            return image;

        }


        private void RotateByExif(MagickImage image)
        {
            try
            {
                if (image.Orientation != OrientationType.Undefined)
                {
                    int index = (int)image.Orientation - 1;
                    int degrees = RotationOperations[index][0];
                    if (degrees != 0)
                        image.Rotate(degrees);
                    switch (RotationOperations[index][1])
                    {
                        case Horizontal:
                            image.Flop();
                            break;
                        case Vertical:
                            image.Flip();
                            break;
                    }

                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in RotateByExif:\n{ex.ToString()}");
                throw;
            }
        }


        private void PrintAttributesToLog(MagickImage originalImage)
        {
            StringBuilder sb = new StringBuilder();

            foreach (var name in originalImage.AttributeNames)
            {
                sb.AppendLine($"{name} = {originalImage.GetAttribute(name)}");
            }

            sb.AppendLine($"OrientationType = {originalImage.Orientation.ToString()}");

            logger.LogInformation(sb.ToString());
        }


        private static void WriteImageToStream(Stream output, MagickImage originalImage, int quality, MagickFormat format)
        {
            originalImage.Quality = quality;
            originalImage.Write(output, format);
            output.Position = 0;
        }


        private async Task<Size> ScaleMagickImageByWidth(MagickImage image, int width, Func<Stream, Size, string, Task> storeFunc)
        {
            try
            {

                Size originalSize = new Size(image.Width, image.Height);
                Size scaledSize = GetSizeByWidth(originalSize, width);
                image.Resize(scaledSize.Width, scaledSize.Height);
                logger.LogInformation($"[{GetTimeStampString()}]\tImage size after resizing: {image.Width}w x {image.Height}h.");

                using (var jpgStream = new MemoryStream())
                {
                    // store in jpg format
                    WriteImageToStream(jpgStream, image, DefaultImageQuality, MagickFormat.Jpg);
                    await storeFunc(jpgStream, scaledSize, "jpg");
                }

                return scaledSize;

            }
            catch (System.Exception ex)
            {
                logger.LogError(ex, $"Error in {nameof(ScaleImageByWidth)}:\n{ex.ToString()}");
                throw;
            }
        }

        private async Task<IEnumerable<Stream>> ScaleImageByWidth(Stream input, int width, Func<Stream, Size, string, Task> storeFunc)
        {
            try
            {
                using (var originalImage = new MagickImage(input))
                {
                    Size originalSize = new Size(originalImage.BaseWidth, originalImage.BaseHeight);
                    Size scaledSize = GetSizeByWidth(originalSize, width);
                    originalImage.Resize(scaledSize.Width, scaledSize.Height);
                    logger.LogInformation($"Image size after resizing: {originalImage.Width}w x {originalImage.Height}h.");


                    var jpgStream = new MemoryStream();
                    var webpStream = new MemoryStream();

                    // store in jpg format
                    WriteImageToStream(jpgStream, originalImage, DefaultImageQuality, MagickFormat.Jpg);
                    WriteImageToStream(webpStream, originalImage, DefaultImageQuality, MagickFormat.WebP);

                    var storeTasks = new[]{
                        storeFunc(jpgStream, scaledSize, "jpg"),
                        storeFunc(webpStream, scaledSize, "webp")
                    };

                    await Task.WhenAll(storeTasks);

                    return new Stream[] { jpgStream, webpStream };
                }
            }
            catch (System.Exception ex)
            {
                logger.LogError(ex, $"Error in {nameof(ScaleImageByWidth)}:\n{ex.ToString()}");
                throw;
            }
        }

        private Size GetSizeByWidth(Size originalSize, int width)
        {
            var ratio = (decimal)width / originalSize.Width;
            var result = new Size(width, (int)(originalSize.Height * ratio));
            logger.LogInformation($"Scaling to width {width}. Original size: {originalSize.Width}w x {originalSize.Height}h, scaled size: {result.Width}w x {result.Height}h.");
            return result;
        }

    }
}
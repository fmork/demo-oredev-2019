using Amazon.Lambda.Core;
using Amazon.Lambda.S3Events;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.XRay.Recorder.Handlers.AwsSdk;
using demunity.aws;
using demunity.aws.Data;
using demunity.aws.Data.DynamoDb;
using demunity.aws.Logging;
using demunity.imagesizer.Images;
using demunity.lib;
using demunity.lib.Data.Models;
using demunity.lib.Logging;
using demunity.lib.Net;
using demunity.lib.Text;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace demunity.imagesizer
{
    public class ImageResizeHandler
    {

        private class ImageKeys
        {
            public ImageKeys(IDictionary<int, string> keys)
            {
                ObjectKeys = keys;
            }

            public IDictionary<int, string> ObjectKeys { get; }
        }

        private static readonly int[] ImageWidths = new[] {
            1440,
            1080,
            720,
            360
        };

        private const int KBYTE = 1024;
        IAmazonS3 S3Client { get; }

        private readonly Func<ILogWriterFactory, S3FileRepository> remoteFileRepositoryFactory;
        private readonly Func<ILogWriterFactory, DynamoDbPhotoRepository> dataRepositoryFactory;
        private readonly Func<ILogWriterFactory, IPhotosService> photosServiceFactory;
        private readonly Func<ILogWriterFactory, IImageScaler> imageScalerFactory;
        private readonly Func<ILambdaLogger, LambdaLogWriterFactory> logWriterFactoryFactory;

        public ImageResizeHandler()
        {
            S3Client = new AmazonS3Client();
            remoteFileRepositoryFactory = logWriterFactory => new S3FileRepository(logWriterFactory);
            var system = new DefaultSystem(new SystemEnvironment(), new SystemTime());
            var dynamoDbClientFactory = new DynamoDbClientFactory(string.Empty);
            dataRepositoryFactory = logWriterFactory =>
                new DynamoDbPhotoRepository(
                    new ScoreCalculator(system, logWriterFactory),
                    new DynamoDbCore(dynamoDbClientFactory, logWriterFactory),
                    system,
                    logWriterFactory);


            photosServiceFactory = logWriterFactory => new PhotosService(
                dataRepositoryFactory(logWriterFactory),
                remoteFileRepositoryFactory(logWriterFactory),
                new TextSplitter(new HttpHelper(), logWriterFactory),
                logWriterFactory);
            imageScalerFactory = logWriterFactory => new ImageScaler(logWriterFactory);
            logWriterFactoryFactory = lambdaLogger => new LambdaLogWriterFactory(lambdaLogger);

            AWSSDKHandler.RegisterXRayForAllServices();
        }

        public async Task<string> MakeWebImagesAsync(S3Event s3Event, ILambdaContext context)
        {
            try
            {
                var logWriterFactory = logWriterFactoryFactory(context.Logger);
                var logWriter = logWriterFactory.CreateLogger<ImageResizeHandler>();
                var imageScaler = imageScalerFactory(logWriterFactory);
                var photosService = photosServiceFactory(logWriterFactory);

                await ProcessImages(s3Event, imageScaler, logWriter, photosService);

                return "OK";
            }
            catch (Exception ex)
            {
                return $"Error in  {nameof(MakeWebImagesAsync)}:\n{ex.ToString()}\n----------\nInput event:\n{JsonConvert.SerializeObject(s3Event)}";
            }
        }

        private async Task<string> ProcessImages(
            S3Event s3Event,
            IImageScaler imageScaler,
            ILogWriter<ImageResizeHandler> logger,
            IPhotosService photoService)
        {
            logger.LogInformation($"{nameof(ProcessImages)}");

            var s3Entity = s3Event.Records?[0].S3;
            if (s3Entity == null)
            {
                logger.LogCritical($"{nameof(s3Entity)} is null");
                return "NULL";
            }

            var urlDecodedKey = System.Web.HttpUtility.UrlDecode(s3Entity.Object.Key);
            UserId userId = GetUserIdFromKey(urlDecodedKey);
            PhotoId photoId = GetPhotoIdFromKey(urlDecodedKey);

            try
            {


                logger.LogInformation($"{nameof(urlDecodedKey)} is '{urlDecodedKey}'");



                await photoService.SetPhotoState(userId, photoId, PhotoState.ProcessingStarted);

                // generate filenames to use for the scaled images
                ImageKeys imageKeys = GetImageKeysWithoutExtension(urlDecodedKey);

                IEnumerable<Size> imageSizes;

                using (var s3InputObject = await S3Client.GetObjectAsync(s3Entity.Bucket.Name, urlDecodedKey))
                using (var originalImageStream = new MemoryStream())
                {
                    await ReadImageIntoStream(logger, originalImageStream, s3InputObject.ResponseStream);
                    imageSizes = await CreateScaledImages(imageScaler, logger, imageKeys, originalImageStream, GetTargetBucket());
                }

                logger.LogInformation($"Updating photo data, making it available.");
                await UpdatePhotoData(photoService, photoId, imageSizes);

                // finish with deleting the upload file
                await DeleteOriginalSourceFile(logger, s3Entity, urlDecodedKey);

                logger.LogInformation($"Done with {urlDecodedKey}");
                return "OK";

            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error when resizing {s3Entity.Object.Key} from bucket {s3Entity.Bucket.Name}:\n{ex.ToString()}");
                // set photo state to ProcessingFailed
                await photoService.SetPhotoState(userId, photoId, PhotoState.ProcessingFailed);
                throw;
            }
        }


        /// <summary>
        /// Reads the image content of the input stream into the target stream.
        /// </summary>
        /// <remarks>
        /// The input stream is typically a multipart upload file, consisting of the multipart separators surrounding
        /// the actual image data. This method detects that, and writes the actual image data onto the target stream.
        /// </remarks>
        private Task ReadImageIntoStream(ILogWriter<ImageResizeHandler> logger, MemoryStream targetStream, Stream inputStream)
        {
            // Get the first line of bytes. This is used to determine whether the input file is a Multipart data file, or a clean image file
            List<byte> lineOfBytes = ReadLineOfBytes(inputStream);

            // Read the data into the target stream (based on if it's a multipart file or just a clean image file)
            return IsMultiPartFile(lineOfBytes.ToArray())
                ? ReadMultipartFileIntoTargetStream(logger, targetStream, inputStream, lineOfBytes)
                : ReadFileIntoTargetStream(logger, targetStream, inputStream, lineOfBytes);

        }

        private async Task DeleteOriginalSourceFile(
            ILogWriter<ImageResizeHandler> logger,
            Amazon.S3.Util.S3EventNotification.S3Entity s3Event,
            string urlDecodedKey)
        {
            DeleteObjectRequest deleteRequest = new DeleteObjectRequest
            {
                BucketName = s3Event.Bucket.Name,
                Key = urlDecodedKey
            };

            logger.LogInformation($"Deleting source file");
            await S3Client.DeleteObjectAsync(deleteRequest);
        }

        private static async Task UpdatePhotoData(IPhotosService photoService, PhotoId photoId, IEnumerable<Size> imageSizes)
        {
            var photoFromDb = await photoService.GetPhoto(photoId, Guid.Empty);
            photoFromDb.State = PhotoState.PhotoAvailable;
            photoFromDb.Sizes = imageSizes;
            await photoService.UpdatePhoto(photoFromDb);
        }

        private Task<IEnumerable<Size>> CreateScaledImages(
            IImageScaler imageScaler,
            ILogWriter<ImageResizeHandler> logger,
            ImageKeys imageKeys,
            MemoryStream originalImageStream,
            string targetBucket)
        {

            var imageSizes = new HashSet<Size>();

            originalImageStream.Position = 0;
            return imageScaler.ScaleImageByWidths(
                originalImageStream,
                (scaledStream, size, extension) =>
                {
                    imageSizes.Add(size);
                    string s3key = $"{imageKeys.ObjectKeys[size.Width]}.{extension}";
                    return StoreImageInTargetBucket(targetBucket, s3key, scaledStream, logger);
                },
                imageKeys.ObjectKeys.Keys);
        }

        private static Task ReadFileIntoTargetStream(ILogWriter<ImageResizeHandler> logger, MemoryStream targetStream, Stream inputStream, List<byte> lineOfBytes)
        {
            logger.LogInformation($"Input is a file");

            // Write the line of bytes previously read from the stream to the beginning of 
            // the original image stream, so that we get the complete data into the target stream.
            targetStream.Write(lineOfBytes.ToArray(), 0, lineOfBytes.Count);

            // Copy the rest of the file onto the stream.
            return inputStream.CopyToAsync(targetStream);
        }

        private Task ReadMultipartFileIntoTargetStream(
            ILogWriter<ImageResizeHandler> logger,
            MemoryStream originalImageStream,
            Stream inputStream,
            List<byte> lineOfBytes)
        {
            logger.LogInformation($"Input is multipart upload");

            // Get the boundary line, keep it for future reference
            var boundaryStartLine = lineOfBytes;

            int imageDataStartPosition = lineOfBytes.Count;
            // Advance in the stream until we have passed a line consisting only
            // of carriage return / linefeed (bytes 13 and 10). After this loop,
            // the input stream is positioned at the start of the actual image data
            do
            {
                lineOfBytes = ReadLineOfBytes(inputStream);
                imageDataStartPosition += lineOfBytes.Count;
            } while (!(lineOfBytes.Count == 2 && lineOfBytes[0] == 13 && lineOfBytes[1] == 10));


            logger.LogInformation($"Reading image file into memory");


            return ReadImageFileIntoStream(originalImageStream, inputStream, boundaryStartLine, imageDataStartPosition, logger);
        }

        private UserId GetUserIdFromKey(string urlDecodedKey)
        {
            var parts = urlDecodedKey.Split('/');
            return Guid.Parse(parts[0]);
        }

        private PhotoId GetPhotoIdFromKey(string urlDecodedKey)
        {
            return Guid.Parse(Path.GetFileNameWithoutExtension(urlDecodedKey));
        }

        private bool IsMultiPartFile(byte[] initialFileData)
        {
            return initialFileData[0] == 45 && initialFileData[1] == 45;
        }

        private ImageKeys GetImageKeysWithoutExtension(string urlDecodedKey)
        {

            var dictionary = ImageWidths.ToDictionary(
                width => width,
                width => Path.Combine(
                    Path.GetDirectoryName(urlDecodedKey),
                     $"{Path.GetFileNameWithoutExtension(urlDecodedKey)}-{width}")

            );

            return new ImageKeys(dictionary);
        }

        private async Task StoreImageInTargetBucket(string targetBucket, string imageKey, Stream inputStream, ILogWriter logger)
        {
            try
            {
                PutObjectRequest putImageRequest = new PutObjectRequest
                {
                    BucketName = targetBucket,
                    Key = imageKey,
                    InputStream = inputStream,
                    AutoCloseStream = false
                };

                // Set the cache policy for images
                putImageRequest.Headers.CacheControl = $"public, max-age={(int)TimeSpan.FromDays(7).TotalSeconds}";

                logger.LogInformation($"Writing image to S3 bucket '{targetBucket}', key = '{putImageRequest.Key}'");
                var imageResponse = await S3Client.PutObjectAsync(putImageRequest);

                inputStream.Position = 0;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error writing object {imageKey} to bucket {targetBucket}:\n{ex.ToString()}");
                throw;
            }
        }


        private string GetTargetBucket()
        {
            return Environment.GetEnvironmentVariable("PHOTO_WEB_BUCKET");
        }


        private async Task ReadImageFileIntoStream(
            Stream outputStream,
            Stream inputStream,
            List<byte> boundaryStartLine,
            int imageDataStartPosition,
            ILogWriter logger)
        {
            // inputStream should at this point be positioned at the start of the actual image data
            // in the multipart content. Calculate where the image data actually ends, by calculating
            // where the multipard end-line begins.
            long lastPositionToRead = inputStream.Length - (boundaryStartLine.Count + 4);

            logger.LogInformation($"First byte of image data is at {imageDataStartPosition}, last byte is at {lastPositionToRead}");

            // read photo file content into memory stream
            byte[] buffer = new byte[8 * KBYTE];
            long currentPos = imageDataStartPosition;
            int readBytes;

            do
            {
                // Calculate size of next chunk to read. Read 8 KB if there is more than
                // 8 KB left to read, otherwise read whatever is left
                int nextChunkSize = (int)Math.Min(8 * KBYTE, lastPositionToRead - currentPos);

                // Read next chunk of data, making note of how much we actually got
                readBytes = await inputStream.ReadAsync(buffer, 0, nextChunkSize);

                // Write the bytes that we got onto the output stream
                await outputStream.WriteAsync(buffer, 0, readBytes);

                // Advance the position counter
                currentPos += readBytes;
            } while (currentPos < lastPositionToRead);

            logger.LogInformation($"Image read into memory. currentPos = '{currentPos}'");
        }

        private static List<byte> ReadLineOfBytes(Stream stream)
        {
            var lineOfBytes = new List<byte>();
            byte nextByte;
            do
            {
                nextByte = (byte)stream.ReadByte();
                lineOfBytes.Add(nextByte);
            } while (nextByte != (byte)10);
            return lineOfBytes;
        }
    }
}

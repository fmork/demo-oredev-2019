using System;
using System.Threading.Tasks;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using demunity.lib;
using demunity.lib.Logging;

namespace demunity.aws
{
	public class S3FileRepository : IRemoteFileRepository
    {
        private readonly ILogWriter<S3FileRepository> logWriter;

        public S3FileRepository(ILogWriterFactory logWriterFactory)
        {
            logWriter = logWriterFactory.CreateLogger<S3FileRepository>();
        }

        public Task<Uri> GetUploadUri(string filename)
        {
            logWriter.LogInformation($"{nameof(GetUploadUri)}({nameof(filename)} = '{filename}')");
            
            try
            {
                var bucket = GetTargetBucket();
                GetPreSignedUrlRequest request = new GetPreSignedUrlRequest
                {
                    BucketName = bucket,
                    Expires = DateTime.UtcNow.AddMinutes(5),
                    Key = filename,
                    Protocol = Protocol.HTTPS,
                    Verb = HttpVerb.PUT,
                    ContentType = "multipart/form-data"
                };

                using (var client = new AmazonS3Client(RegionEndpoint.EUWest1))
                {
                    logWriter.LogInformation($"Creating presigned url. Bucket = '{request.BucketName}', Key='{request.Key}', Expires='{request.Expires.ToString("yyyy-MM-ddTHH:mm:ss")}'");
                    var result = client.GetPreSignedURL(request);
                    logWriter.LogInformation($"Presigned url created: {result}");

                    return Task.FromResult(new Uri(result));
                }

            }
            catch (Exception ex)
            {
                logWriter.LogError(ex, $"Error in {nameof(GetUploadUri)}({nameof(filename)} = '{filename}')");
                throw;
            }
        }


        private string GetTargetBucket()
        {
            return Environment.GetEnvironmentVariable(Constants.EnvironmentVariables.UploadBucket);
        }
    }
}

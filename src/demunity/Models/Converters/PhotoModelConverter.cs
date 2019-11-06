using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using demunity.lib.Data.Models;
using demunity.lib.Extensions;
using demunity.lib.Logging;
using demunity.lib.Text;
using Newtonsoft.Json;

namespace demunity.Models.Converters
{
    public class PhotoModelConverter
    {
        private readonly ILogWriter<PhotoModelConverter> logger;
        private readonly Uri staticAssetHost;
        private readonly ITextSplitter textSplitter;

        public PhotoModelConverter(Uri staticAssetHost, ITextSplitter textSplitter, ILogWriterFactory logWriterFactory)
        {
            logger = logWriterFactory.CreateLogger<PhotoModelConverter>();
            this.staticAssetHost = staticAssetHost ?? throw new ArgumentNullException(nameof(staticAssetHost));
            this.textSplitter = textSplitter ?? throw new ArgumentNullException(nameof(textSplitter));
        }
        public PublicPhotoModel ToPublic(PhotoModel input)
        {
            return ToPublic(input, Guid.Empty);
        }

        public PublicPhotoModel ToPublic(PhotoModel input, Guid? currentUserId)
        {
            try
            {
                Size referenceSize = input.Sizes.FirstOrDefault();
                return new PublicPhotoModel
                {
                    CreatedTime = input.CreatedTime,
                    Filename = input.Filename,
                    Id = input.PhotoId,
                    Uris = GetPhotoUris(input, staticAssetHost),
                    State = input.State.ToString(),
                    UploadUri = input.UploadUri,
                    UserId = input.UserId,
                    UserName = input.UserName,
                    LikeCount = input.LikeCount,
                    CommentCount = input.CommentCount,
                    LikedByCurrentUser = input.PhotoIsLikedByCurrentUser,
                    PhotoIsOwnedByCurrentUser = input.UserId.Equals(currentUserId),
                    CurrentUserId = currentUserId,
                    Text = input.RawText,
                    HtmlText = input.RawText.GetHtmlText(textSplitter),
                    IsPortraitOrientation = referenceSize.Width < referenceSize.Height,
                    Score = input.Score
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in {nameof(ToPublic)}\n{nameof(input)}:\n{JsonConvert.SerializeObject(input)}\n\nException:\n{ex.ToString()}");
                throw;
            }
        }



        private IEnumerable<PhotoUri> GetPhotoUris(PhotoModel input, Uri imageAssetHost)
        {

            var parts = input.ObjectKey.Split('/');

            if (parts.Length != 2)
            {
                throw new Exception($"Error in GetPhotoUris. Path parts was not of expected format. Expected two elements. Actual:\n{string.Join("\n", parts.Select((s, n) => $"\t[{n}]: {s}"))}");
            }

            var extension = Path.GetExtension(parts[1]);
            var fileWithoutExtension = Path.GetFileNameWithoutExtension(parts[1]);


            return input.Sizes.Select(size =>
            {
                Uri relativeJpgUri = new Uri($"{parts[0]}/{fileWithoutExtension}-{size.Width}.jpg", UriKind.Relative);
                Uri relativeWebpUri = new Uri($"{parts[0]}/{fileWithoutExtension}-{size.Width}.webp", UriKind.Relative);

                return new PhotoUri(
                    size.Width,
                    new Uri(imageAssetHost, relativeJpgUri),
                    new Uri(imageAssetHost, relativeWebpUri));
            });
        }
    }
}
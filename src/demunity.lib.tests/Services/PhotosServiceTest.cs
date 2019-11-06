using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using demunity.lib.Data;
using demunity.lib.Data.Models;
using demunity.lib.Logging;
using demunity.lib.Net;
using demunity.lib.Text;
using demunity.testtools;
using Moq;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace demunity.lib.tests
{
    public class PhotosServiceTest
    {
        private readonly Mock<IPhotoRepository> dataRepositoryMock = new Mock<IPhotoRepository>();
        private readonly Mock<IRemoteFileRepository> remoteFileRepositoryMock = new Mock<IRemoteFileRepository>();
        private readonly Mock<ILogWriterFactory> logWriterFactoryMock = new Mock<ILogWriterFactory>();



        public PhotosServiceTest(ITestOutputHelper outputHelper)
        {
            SetupLogWriterFactoryMock(outputHelper);
            SetupDataRepositoryMock();
            SetupRemoteFileRepositoryMock();
        }

        private void SetupRemoteFileRepositoryMock()
        {
            remoteFileRepositoryMock
               .Setup(x => x.GetUploadUri(It.IsAny<string>()))
               .Returns<string>(s => Task.FromResult(new Uri($"https://thehost/{s}")));
        }

        private void SetupDataRepositoryMock()
        {
            dataRepositoryMock
                .Setup(x => x.CreatePhoto(It.IsAny<PhotoModel>()))
                .Returns<PhotoModel>(x => Task.FromResult(x));
        }

        private void SetupLogWriterFactoryMock(ITestOutputHelper outputHelper)
        {
            logWriterFactoryMock
                .Setup(x => x.CreateLogger<PhotosService>())
                .Returns(() => new TestOutputHelperLogger<PhotosService>(outputHelper));

            logWriterFactoryMock
                .Setup(x => x.CreateLogger<TextSplitter>())
                .Returns(() => new TestOutputHelperLogger<TextSplitter>(outputHelper));

        }

        [Fact]
        public async Task CreatePhoto_InvokesRemoteFileRepository()
        {
            UserId userId = new Guid("13999be1-095a-4726-87e9-d11f2103585c");
            string userName = "Fredrik Mörk";
            string filename = "filename.jpg";
            string text = "text";

            var service = GetPhotosService();

            var result = await service.CreatePhoto(userId, userName, filename, text);

            result.UploadUri.ShouldBe(new Uri($"https://thehost/{result.ObjectKey}.jpg"));

        }


        [Fact]
        public async Task SetPhotoText_ExpectedNumberOfHashtags()
        {
            //Given
            PhotoId photoId = new Guid("d3f31301-c244-4104-9b83-2ad073a8d2d0");
            UserId userId = new Guid("13999be1-095a-4726-87e9-d11f2103585c");
            var newText = "This is a new text #with #hashtags.";

            IEnumerable<HashtagModel> observedHashtags = null;

            dataRepositoryMock
                .Setup(x => x.SetPhotoText(It.IsAny<UserId>(), It.IsAny<PhotoId>(), It.IsAny<string>(), It.IsAny<IEnumerable<HashtagModel>>()))
                .Callback<UserId, PhotoId, string, IEnumerable<HashtagModel>>((uid, pid, t, hashtags) => observedHashtags = hashtags)
                .Returns<UserId, PhotoId, string, IEnumerable<HashtagModel>>((uid, pid, t, hashtags) => Task.CompletedTask);

            var service = GetPhotosService();


            //When
            await service.SetPhotoText(photoId, userId, newText);

            //Then
            observedHashtags.ShouldNotBeNull();
            observedHashtags.Count().ShouldBe(2);
            observedHashtags.First().CreatedTime.ShouldNotBe(DateTimeOffset.MinValue);
        }

        private PhotosService GetPhotosService()
        {
            return new PhotosService(
                dataRepositoryMock.Object,
                remoteFileRepositoryMock.Object,
                new TextSplitter(new HttpHelper(), logWriterFactoryMock.Object),
                logWriterFactoryMock.Object);
        }
    }
}

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
    public class FeedbackServiceTest
    {
        private readonly Mock<ILikeRepository> likeRepositoryMock = new Mock<ILikeRepository>();
        private readonly Mock<ILogWriterFactory> logWriterFactoryMock = new Mock<ILogWriterFactory>();
        private readonly Mock<ICommentRepository> commentRepositoryMock = new Mock<ICommentRepository>();

        public FeedbackServiceTest(ITestOutputHelper outputHelper)
        {
            SetupLogWriterFactoryMock(outputHelper);
        }



        private void SetupLogWriterFactoryMock(ITestOutputHelper outputHelper)
        {
            logWriterFactoryMock
                .Setup(x => x.CreateLogger<FeedbackService>())
                .Returns(() => new TestOutputHelperLogger<FeedbackService>(outputHelper));
        }





        [Fact]
        public Task AddPhotoComment_EmptyText_ThrowsExceptionAsync()
        {
            UserId userId = new Guid("13999be1-095a-4726-87e9-d11f2103585c");
            PhotoId photoId = new Guid("d3f31301-c244-4104-9b83-2ad073a8d2d0");
            string userName = "Fredrik Mörk";
            string text = "";
            var service = GetFeedbackService();


            return service.AddPhotoComment(photoId, userId, userName, text).ShouldThrowAsync<Exception>();

        }

        [Fact]
        public async Task AddPhotoComment_NonEmptyText_InvokesDataRepository()
        {
            UserId userId = new Guid("13999be1-095a-4726-87e9-d11f2103585c");
            PhotoId photoId = new Guid("d3f31301-c244-4104-9b83-2ad073a8d2d0");
            string userName = "Fredrik Mörk";
            string text = "not empty";

            // set up expectation that AddPhotoComment is invoked in the data repository
            commentRepositoryMock
                .Setup(x => x.AddPhotoComment(It.IsAny<PhotoComment>()))
                .Verifiable();

            var service = GetFeedbackService();


            await service.AddPhotoComment(photoId, userId, userName, text);

            // verify expected calls were made.
            likeRepositoryMock.Verify();
        }

        [Fact]
        public async Task AddPhotoComment_PopulatesInputProperly()
        {
            UserId userId = new Guid("13999be1-095a-4726-87e9-d11f2103585c");
            PhotoId photoId = new Guid("d3f31301-c244-4104-9b83-2ad073a8d2d0");
            string userName = "Fredrik Mörk";
            string text = "not empty";
            PhotoComment observedPhotoComment = null;
            // set up expectation that AddPhotoComment is invoked in the data repository
            commentRepositoryMock
                .Setup(x => x.AddPhotoComment(It.IsAny<PhotoComment>()))
                .Callback<PhotoComment>(x => observedPhotoComment = x)
                .Returns(Task.CompletedTask);

            var service = GetFeedbackService();


            await service.AddPhotoComment(photoId, userId, userName, text);

            observedPhotoComment.ShouldNotBeNull();
            observedPhotoComment.PhotoId.ShouldBe(photoId);
            observedPhotoComment.UserId.ShouldBe(userId);
            observedPhotoComment.UserName.ShouldBe(userName);
            observedPhotoComment.Text.ShouldBe(text);

        }

        [Fact]
        public async Task GetPhotoCommentsAsync()
        {
            PhotoId photoId = new Guid("d3f31301-c244-4104-9b83-2ad073a8d2d0");

            commentRepositoryMock
                .Setup(x => x.GetComments(It.IsAny<PhotoId>()))
                .Verifiable();

            var service = GetFeedbackService();

            IEnumerable<PhotoComment> comments = await service.GetPhotoComments(photoId);

            likeRepositoryMock.Verify();
        }


        private IFeedbackService GetFeedbackService()
        {
            return new FeedbackService(
                likeRepositoryMock.Object,
                commentRepositoryMock.Object,
                logWriterFactoryMock.Object);
        }
    }
}

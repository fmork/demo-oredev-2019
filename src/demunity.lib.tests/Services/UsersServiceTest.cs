using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using demunity.lib.Data;
using demunity.lib.Data.Models;
using demunity.lib.Logging;
using demunity.lib.Net;
using demunity.lib.Security;
using demunity.lib.Text;
using demunity.testtools;
using Moq;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace demunity.lib.tests
{
    public class UsersServiceTest
    {
        private readonly Mock<IUserRepository> userRepositoryMock = new Mock<IUserRepository>();
        private readonly Mock<ILogWriterFactory> logWriterFactoryMock = new Mock<ILogWriterFactory>();



        public UsersServiceTest(ITestOutputHelper outputHelper)
        {
            SetupLogWriterFactoryMock(outputHelper);
        }

        [Theory]
        [InlineData(OnlineProfileType.Instagram, "@photosbyfmork", "photosbyfmork")]
        [InlineData(OnlineProfileType.Instagram, "photosbyfmork", "photosbyfmork")]
        [InlineData(OnlineProfileType.Twitter, "@fmork", "fmork")]
        [InlineData(OnlineProfileType.Twitter, "fmork", "fmork")]
        [InlineData(OnlineProfileType.Web, "www.fmork.net", "https://www.fmork.net")]
        [InlineData(OnlineProfileType.Web, "http://www.fmork.net", "http://www.fmork.net")]
        [InlineData(OnlineProfileType.Web, "https://www.fmork.net", "https://www.fmork.net")]
        public async Task AddSocialProfileAsync(OnlineProfileType profileType, string profile, string expectedProfile)
        {
            
            Guid userId = Guid.Parse("a1a537ea-6a78-49f5-bdbe-63099197cd49");

            userRepositoryMock
                .Setup(x => x.AddSocialProfile(It.IsAny<OnlineProfile>(), It.IsAny<UserId>()))
                .Returns<OnlineProfile, UserId>((p, uid) => Task.FromResult(new[] { p }.AsEnumerable()));
            
            var service = GetUsersService();

            var input = new OnlineProfile
            {
                Type = profileType,
                Profile = profile
            };

            var result = await service.AddSocialProfile(input, userId);

            result.First().Profile.ShouldBe(expectedProfile);
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


        private UsersService GetUsersService()
        {
            return new UsersService(
                userRepositoryMock.Object,
                logWriterFactoryMock.Object);
        }
    }
}

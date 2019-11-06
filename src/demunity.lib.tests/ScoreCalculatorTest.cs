using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using demunity.lib;
using demunity.lib.Data.Models;
using demunity.lib.Logging;
using demunity.testtools;
using Moq;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace demunity.popscore.tests
{
    public class ScoreCalculatorTest
    {
        private readonly PhotoId photoWithHighScoreId = new Guid("a6010657-960c-4b58-a181-799666cbc920");
        private readonly PhotoId photoWithLowScoreId = new Guid("87ff0d32-f9ab-43c9-abaf-5ccc7d91dd09");
        private readonly Mock<ISystem> systemMock = new Mock<ISystem>();
        private readonly Mock<ILogWriterFactory> logWriterFactoryMock = new Mock<ILogWriterFactory>();
        private readonly DateTimeOffset referenceTime = new DateTimeOffset(2019, 9, 26, 8, 15, 34, 0, TimeSpan.Zero);
        private readonly DateTimeOffset utcNow = new DateTimeOffset(2019, 9, 26, 8, 29, 34, 0, TimeSpan.Zero);
        public ScoreCalculatorTest(ITestOutputHelper outputHelper)
        {
            SetupLogWriterFactoryMock(outputHelper);
        }



        [Fact]
        public void CalculateScores_ReturnsExpectedScores()
        {

            SetCurrentTime(utcNow);

            var scoreCalculator = new ScoreCalculator(systemMock.Object, logWriterFactoryMock.Object);
            var scores = scoreCalculator.CalculateScores(GetPhotosWithCommentsAndLikes());

            scores.Count().ShouldBe(2);

            scores.First(s => s.PhotoId == photoWithHighScoreId).Score.ShouldBe(126.8);
            scores.First(s => s.PhotoId == photoWithLowScoreId).Score.ShouldBe(10.1);
        }

        private void SetCurrentTime(DateTimeOffset utcNow)
        {
            Mock<ISystemTime> timeMock = new Mock<ISystemTime>();
            timeMock
                .Setup(x => x.UtcNow)
                .Returns(utcNow);

            systemMock
                .Setup(x => x.Time)
                .Returns(timeMock.Object);
        }


        private void SetupLogWriterFactoryMock(ITestOutputHelper outputHelper)
        {
            logWriterFactoryMock
                .Setup(x => x.CreateLogger<ScoreCalculator>())
                .Returns(() => new TestOutputHelperLogger<ScoreCalculator>(outputHelper));
        }


        private IEnumerable<PhotoWithCommentsAndLikes> GetPhotosWithCommentsAndLikes()
        {

            return new PhotoWithCommentsAndLikes[]
            {

                // this photo has comments and likes within the cutoff range, so it should get high score
                new PhotoWithCommentsAndLikes
                {
                    Photo = new PhotoModel{ PhotoId = photoWithHighScoreId, CreatedTime = utcNow.AddHours(-10)},
                    Comments = new PhotoComment[]{
                        new PhotoComment { PhotoId = photoWithHighScoreId, CreatedTime = utcNow.AddHours(-6) }, // (18-6)*1=12
                        new PhotoComment { PhotoId = photoWithHighScoreId, CreatedTime = utcNow.AddHours(-10) } // (18-10)*1=8
                    },
                    Likes = new PhotoLikeRecord[]{
                        new PhotoLikeRecord { PhotoId = photoWithHighScoreId, CreatedTime = utcNow.AddHours(-2) }, // (18-2)*2=32
                        new PhotoLikeRecord { PhotoId = photoWithHighScoreId, CreatedTime = utcNow.AddHours(-2) }, // (18-2)*2=32
                        new PhotoLikeRecord { PhotoId = photoWithHighScoreId, CreatedTime = utcNow.AddHours(-5) }, // (18-5)*2=26
                        new PhotoLikeRecord { PhotoId = photoWithHighScoreId, CreatedTime = utcNow.AddHours(-10) }, // (18-10)*2=16
                    }
                },

                // this photo has comments and likes only outside of the cutoff frame, so it should get low score
                new PhotoWithCommentsAndLikes
                {
                    Photo = new PhotoModel{ PhotoId = photoWithLowScoreId ,CreatedTime = utcNow.AddHours(-30)},
                    Comments = new PhotoComment[]{
                        new PhotoComment { PhotoId = photoWithLowScoreId, CreatedTime = utcNow.AddHours(-26) },
                        new PhotoComment { PhotoId = photoWithLowScoreId, CreatedTime = utcNow.AddHours(-30) }
                    },
                    Likes = new PhotoLikeRecord[]{
                        new PhotoLikeRecord { PhotoId = photoWithLowScoreId, CreatedTime = utcNow.AddHours(-22) },
                        new PhotoLikeRecord { PhotoId = photoWithLowScoreId, CreatedTime = utcNow.AddHours(-22) },
                        new PhotoLikeRecord { PhotoId = photoWithLowScoreId, CreatedTime = utcNow.AddHours(-25) },
                        new PhotoLikeRecord { PhotoId = photoWithLowScoreId, CreatedTime = utcNow.AddHours(-30) },
                    }
                }
            };
        }
    }
}

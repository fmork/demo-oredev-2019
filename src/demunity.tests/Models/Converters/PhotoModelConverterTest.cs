using System;
using demunity.lib.Data.Models;
using demunity.lib.Logging;
using demunity.lib.Net;
using demunity.lib.Text;
using demunity.Models.Converters;
using demunity.testtools;
using Moq;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace demunity.tests.Models.Converters
{
	public class PhotoModelConverterTest
	{
		private const string RegularTextWithNoSpecialFeatures = "This is a regular text with no special features.";
		private const string TextWithHashTag = "This is a regular text with a #hashtag embedded.";
		private readonly Uri imageAssetHost = new Uri("https://image.asset.host");
		private readonly Mock<ILogWriterFactory> logWriterFactoryMock = new Mock<ILogWriterFactory>();

		public PhotoModelConverterTest(ITestOutputHelper outputHelper)
		{
			SetupLogWriterFactoryMock(outputHelper);
		}

		[Fact]
		public void ToPublic_RegularText()
		{
			var converter = new PhotoModelConverter(imageAssetHost, new TextSplitter(new HttpHelper(), logWriterFactoryMock.Object), logWriterFactoryMock.Object);

			var publicPhoto = converter.ToPublic(GetPhotoModelWithGivenText(RegularTextWithNoSpecialFeatures));

			publicPhoto.Text.ShouldBe(RegularTextWithNoSpecialFeatures);
		}

		[Fact]
		public void ToPublic_HashTag()
		{
			var converter = new PhotoModelConverter(imageAssetHost, new TextSplitter(new HttpHelper(), logWriterFactoryMock.Object), logWriterFactoryMock.Object);

			var publicPhoto = converter.ToPublic(GetPhotoModelWithGivenText(TextWithHashTag));

			publicPhoto.HtmlText.ShouldBe("This is a regular text with a <a href=\"/tags/hashtag\">#hashtag</a> embedded.");
		}

		private static PhotoModel GetPhotoModelWithGivenText(string text)
		{
			return new PhotoModel
			{
				ObjectKey = "object/key",
				RawText = text
			};
		}

		private void SetupLogWriterFactoryMock(ITestOutputHelper outputHelper)
        {
            logWriterFactoryMock
                .Setup(x => x.CreateLogger<PhotoModelConverter>())
                .Returns(() => new TestOutputHelperLogger<PhotoModelConverter>(outputHelper));
        }
	}
}
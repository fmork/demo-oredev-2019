using System;
using System.Collections.Generic;
using System.Linq;
using demunity.lib.Logging;
using demunity.lib.Net;
using demunity.lib.Text;
using demunity.testtools;
using Moq;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace demunity.lib.tests.Text
{
	public class TextSplitterTest
	{

		private const string TextWithoutSpecialElements = "This is a regular text. Just text.";
		private const string TextWithTwoHashtags = "This is a #regular text. Just #text.";
		private const string TextWithOneUrl = "This text contans a link to https://www.oredev.org";
		private const string TextWithOneUrlFollowedByADot = "This text contans a link to https://www.oredev.org.";
		private readonly Mock<ILogWriterFactory> logWriterFactoryMock = new Mock<ILogWriterFactory>();

		private readonly IHttpHelper httpHelper = new HttpHelper();


		public TextSplitterTest(ITestOutputHelper outputHelper)
		{
			SetupLogWriterFactoryMock(outputHelper);
		}

		[Fact]
		public void Split_NullText_HandledGracefully()
		{
			var textSplitter = GetTextSplitter();

			var result = textSplitter.Split(null, TextPatterns.Hashtags | TextPatterns.Urls);

			result.Count().ShouldBe(1);
			result.First().Text.ShouldBe(string.Empty);
		
		}

		[Theory]
		[InlineData(TextWithoutSpecialElements, 1)]
		[InlineData(TextWithOneUrl, 3)]
		[InlineData(TextWithOneUrlFollowedByADot, 3)]
		[InlineData(TextWithTwoHashtags, 5)]
		public void Split_ReturnsExpectedNumberOfItems(string input, int expectedCount)
		{
			GetTextSplitter()
				.Split(input, TextPatterns.Hashtags | TextPatterns.Urls)
				.Count()
				.ShouldBe(expectedCount);
		}

		[Fact]
		public void Split_RegularText_ReturnsOneTextItem()
		{
			var textSplitter = GetTextSplitter();

			var result = textSplitter.Split(TextWithoutSpecialElements, TextPatterns.Hashtags | TextPatterns.Urls);

			result.Count().ShouldBe(1);
			result.First().ItemType.ShouldBe(TextItemType.Text);

		}

		

		[Fact]
		public void Split_TwoHashtags_ItemsHaveExpectedTypes()
		{
			var textSplitter = GetTextSplitter();

			var result = textSplitter.Split(TextWithTwoHashtags, TextPatterns.Hashtags | TextPatterns.Urls);

			var resultArray = result.ToArray();
			resultArray[0].ItemType.ShouldBe(TextItemType.Text);
			resultArray[1].ItemType.ShouldBe(TextItemType.HashTag);
			resultArray[2].ItemType.ShouldBe(TextItemType.Text);
			resultArray[3].ItemType.ShouldBe(TextItemType.HashTag);
			resultArray[4].ItemType.ShouldBe(TextItemType.Text);

		}

		[Fact]
		public void Split_TwoHashtags_HashtagsHaveExpectedTextsAndLinks()
		{
			var textSplitter = GetTextSplitter();

			var result = textSplitter.Split(TextWithTwoHashtags, TextPatterns.Hashtags | TextPatterns.Urls);

			var hashTags = result.Where(x => x.ItemType == TextItemType.HashTag).ToArray();
			hashTags[0].Text.ShouldBe("#regular");
			hashTags[0].Link.ShouldBe(new Uri("/tags/regular", UriKind.Relative));
			hashTags[1].Text.ShouldBe("#text");
			hashTags[1].Link.ShouldBe(new Uri("/tags/text", UriKind.Relative));
		}

		[Fact]
		public void Split_OneUrl_ReturnsExpectedUrl()
		{
			var textSplitter = GetTextSplitter();

			var result = textSplitter.Split(TextWithOneUrl, TextPatterns.Hashtags | TextPatterns.Urls).ToArray();

			result[1].Link.ToString().ShouldBe("https://www.oredev.org/");
		}


		private void SetupLogWriterFactoryMock(ITestOutputHelper outputHelper)
		{
			logWriterFactoryMock
				.Setup(x => x.CreateLogger<TextSplitter>())
				.Returns(() => new TestOutputHelperLogger<TextSplitter>(outputHelper));
		}

		private TextSplitter GetTextSplitter()
		{
			return new TextSplitter(httpHelper, logWriterFactoryMock.Object);
		}
	}
}
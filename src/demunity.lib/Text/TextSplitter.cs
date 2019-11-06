using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using demunity.lib.Logging;
using demunity.lib.Net;

namespace demunity.lib.Text
{

    public partial class TextSplitter : ITextSplitter
    {
        private const string HashTagPattern = @"#\w+";
        private const string HttpUrlPattern = @"http[s]{0,1}\://[a-zA-Z0-9\-\.]+\.[a-zA-Z]{2,3}(?:[a-zA-Z0-9]*)?/?(?:[a-zA-Z0-9\-\._\?\,\'/\\\+&amp;%\$#\=~])*[^\.\,\)\(\s]";
        private readonly IHttpHelper httpHelper;
        private readonly ILogWriter<TextSplitter> logger;

        public TextSplitter(IHttpHelper httpHelper, ILogWriterFactory logWriterFactory)
        {
            this.httpHelper = httpHelper;
            logger = logWriterFactory.CreateLogger<TextSplitter>();

        }

        private static readonly IEnumerable<string> SpecialConstructs = new[]
            {
                "@",
                "#",
                "www.",
                "http://",
                "https://"
            };


        public IEnumerable<TextItem> Split(string text, TextPatterns patterns)
        {
            List<TextItem> result = new List<TextItem>();
            try
            {
                // check if there is a chance that the text contains any parts that need to be treated specially:
                if (string.IsNullOrEmpty(text) || !SpecialConstructs.Any(text.Contains))
                {
                    result.Add(new TextItem(text ?? string.Empty, TextItemType.Text));
                }
                else
                {
                    IEnumerable<InternalMatch> specialMatches = new InternalMatch[] { };
                    if ((patterns & TextPatterns.Urls) == TextPatterns.Urls)
                    {
                        specialMatches = specialMatches
                            .Union(GetMatches(text, HttpUrlPattern, TextItemType.WebLink));
                    }

                    if ((patterns & TextPatterns.Hashtags) == TextPatterns.Hashtags)
                    {
                        specialMatches = specialMatches
                            .Union(GetMatches(text, HashTagPattern, TextItemType.HashTag));
                    }

                    specialMatches = specialMatches
                        .OrderBy(im => im.RegexMatch.Index)
                        .ToArray();

                    if (!specialMatches.Any())
                    {
                        result.Add(new TextItem(text, TextItemType.Text));
                    }
                    else
                    {
                        bool thereAreMoreSpecialFeatures;
                        int matchIndex = 0;
                        InternalMatch match;
                        int currentStartIndex = 0;
                        do
                        {
                            thereAreMoreSpecialFeatures = null != (match = specialMatches.Skip(matchIndex).FirstOrDefault());
                            if (!thereAreMoreSpecialFeatures)
                            {
                                // Return the remaining text as a regular text item. 
                                result.Add(new TextItem(text.Substring(currentStartIndex), TextItemType.Text));
                            }
                            else
                            {

                                Match regexMatch = match.RegexMatch;
                                if (regexMatch.Index > currentStartIndex)
                                {
                                    result.Add(new TextItem(text.Substring(currentStartIndex, regexMatch.Index - currentStartIndex), TextItemType.Text));
                                }
                                result.Add(GetTextItem(match));
                                matchIndex++;
                                currentStartIndex = regexMatch.Index + regexMatch.Length;
                            }
                        }
                        while (thereAreMoreSpecialFeatures);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in {nameof(Split)}({nameof(text)} = '{text}'):\n{ex.ToString()}");
                throw;
            }
        }

        private TextItem GetTextItem(InternalMatch match)
        {
            string text = match.RegexMatch.Value;
            switch (match.TextItemType)
            {
                case TextItemType.WebLink:

                    Uri webUri;
                    if (Uri.TryCreate(text, UriKind.Absolute, out webUri))
                    {
                        return new TextItem(text, TextItemType.WebLink, webUri);
                    }

                    return new TextItem(text, TextItemType.Text);

                case TextItemType.HashTag:
                    Uri twitterHashUri;
                    if (TryGetHashTagUri(text, out twitterHashUri))
                    {
                        return new TextItem(text, TextItemType.HashTag, twitterHashUri);
                    }
                    return new TextItem(text, TextItemType.Text);

                default:
                    return new TextItem(text, match.TextItemType);
            }
        }

        private static IEnumerable<InternalMatch> GetMatches(string text, string pattern, TextItemType textItemType)
        {
            return Regex.Matches(text, pattern, RegexOptions.ExplicitCapture).Cast<Match>().Select(m => new InternalMatch(textItemType, m));
        }

        public bool TryGetHashTagUri(string hashTag, out Uri uri)
        {
            return Uri.TryCreate($"/tags/{httpHelper.UrlEncode(hashTag.Substring(1))}", UriKind.Relative, out uri);
        }
    }
}
using System;
using System.Linq;
using demunity.lib.Text;

namespace demunity.lib.Extensions
{
    public static class StringExtensions
    {
        public static string EnsureEndsWith(this string input, string endsWith)
        {
            return input?.EndsWith(endsWith) ?? false
                ? input
                : string.Concat(input ?? string.Empty, endsWith);
        }

        public static string GetHtmlText(this string input, ITextSplitter textSplitter)
        {
            return string.Join("", textSplitter.Split(input, TextPatterns.Hashtags | TextPatterns.Urls).Select(x =>
            {
                switch (x.ItemType)
                {
                    case TextItemType.HashTag:
                    case TextItemType.WebLink:
                        return $"<a href=\"{x.Link}\">{x.Text}</a>";
                    case TextItemType.Text:
                    default:
                        return string.Join("<br />", x.Text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));
                }
            }));
        }
    }
}
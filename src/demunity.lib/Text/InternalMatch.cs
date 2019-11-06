using System.Text.RegularExpressions;

namespace demunity.lib.Text
{

    public partial class TextSplitter
    {
        private class InternalMatch
        {
            public InternalMatch(TextItemType textItemType, Match regexMatch)
            {
                TextItemType = textItemType;
                RegexMatch = regexMatch;
            }

            public TextItemType TextItemType { get; private set; }

            public Match RegexMatch { get; private set; }
        }
    }
}
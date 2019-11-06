using System;
using System.Diagnostics;

namespace demunity.lib.Text
{
    [DebuggerDisplay("TextItem: {Text} ({ItemType})")]
    public class TextItem
    {
        public TextItem(string text, TextItemType itemType, Uri link = null)
        {
            Text = text;
            ItemType = itemType;
            Link = link;
        }

        public string Text { get; private set; }

        public TextItemType ItemType { get; private set; }

        public Uri Link { get; private set; }
    }
}
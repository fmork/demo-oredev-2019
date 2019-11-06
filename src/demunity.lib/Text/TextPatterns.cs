using System;

namespace demunity.lib.Text
{
    [Flags]
    public enum TextPatterns
    {
        None = 0,
        Urls = 1,
        Hashtags = 2,
    }
}
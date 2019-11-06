using System.Collections.Generic;

namespace demunity.lib.Text
{
    public interface ITextSplitter
    {
        IEnumerable<TextItem> Split(string text, TextPatterns patterns);
    }
}
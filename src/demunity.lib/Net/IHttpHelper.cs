namespace demunity.lib.Net
{
    public interface IHttpHelper
    {
        string HtmlDecode(string text);
        string UrlEncode(string text);
    }
}
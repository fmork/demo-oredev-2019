using System.Web;

namespace demunity.lib.Net
{


    public class HttpHelper : IHttpHelper
    {
        public string UrlEncode(string text)
        {
            return HttpUtility.UrlEncode(text);
        }

        public string HtmlDecode(string text)
        {
            string htmlDecode = HttpUtility.HtmlDecode(text);
            return htmlDecode;
        }
    }
}
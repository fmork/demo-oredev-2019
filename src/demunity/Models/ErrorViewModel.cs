using System;

namespace demunity.Models
{
    public class ErrorViewModel
    {
        public string RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        public int HttpStatusCode { get; internal set; }
    }
}
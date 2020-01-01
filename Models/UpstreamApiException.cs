using System;
using System.Collections.Generic;
using System.Text;

namespace Overseer.Models
{
    public class UpstreamApiException : Exception
    {

        public UpstreamApiException() : base() { }

        public UpstreamApiException(string message) : base(message) { }

        public UpstreamApiException(string message, Exception innerException) : base(message, innerException) { }
    }
}

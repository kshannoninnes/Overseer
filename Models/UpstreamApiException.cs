using System;
using System.Collections.Generic;
using System.Text;

namespace Overseer.Models
{
    public class UpstreamApiException : Exception
    {
        public UpstreamApiException(string message) : base(message) { }
    }
}

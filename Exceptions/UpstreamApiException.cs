using System;

namespace Overseer.Exceptions
{
    public class UpstreamApiException : Exception
    {
        public UpstreamApiException() 
        {
        }

        public UpstreamApiException(string message) : base(message) 
        { 
        }

        public UpstreamApiException(string message, Exception innerException) : base(message, innerException) 
        { 
        }
    }
}

using System;

namespace Overseer.Exceptions
{
    public class UpstreamApiException : Exception
    {
        public UpstreamApiException(string message) : base(message) 
        { 
        }
    }
}

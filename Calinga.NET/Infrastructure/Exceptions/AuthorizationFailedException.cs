using System;

namespace Calinga.NET.Infrastructure.Exceptions
{
    [Serializable]
    public class AuthorizationFailedException : Exception
    {
        public AuthorizationFailedException() : base()
        {
        }

        public AuthorizationFailedException(string message) : base(message)
        {
        }

        public AuthorizationFailedException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}

using System;

namespace Calinga.NET.Infrastructure.Exceptions
{
    [Serializable]
    public class LanguagesNotAvailableException : Exception
    {
        public LanguagesNotAvailableException()
        {
        }

        public LanguagesNotAvailableException(string message) : base(message)
        {
        }

        public LanguagesNotAvailableException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}

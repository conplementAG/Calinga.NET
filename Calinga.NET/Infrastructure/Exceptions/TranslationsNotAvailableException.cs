using System;

namespace Calinga.NET.Infrastructure.Exceptions
{
    [Serializable]
    public class TranslationsNotAvailableException : Exception
    {
        public TranslationsNotAvailableException()
        {
        }

        public TranslationsNotAvailableException(string message) : base(message)
        {
        }

        public TranslationsNotAvailableException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}

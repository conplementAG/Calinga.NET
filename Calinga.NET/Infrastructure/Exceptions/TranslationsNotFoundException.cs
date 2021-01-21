using System;

namespace Calinga.NET.Infrastructure.Exceptions
{
    [Serializable]
    public class TranslationsNotFoundException : Exception
    {
        public TranslationsNotFoundException()
        {
        }

        public TranslationsNotFoundException(string message) : base(message)
        {
        }

        public TranslationsNotFoundException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}

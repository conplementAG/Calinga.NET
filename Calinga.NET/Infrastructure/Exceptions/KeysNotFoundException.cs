using System;
using System.Collections.Generic;

namespace Calinga.NET.Infrastructure.Exceptions
{
    [Serializable]
    public class KeysNotFoundException : Exception
    {
        public IReadOnlyCollection<string> MissingKeys { get; }

        public KeysNotFoundException()
        {
            MissingKeys = Array.Empty<string>();
        }

        public KeysNotFoundException(string message) : base(message)
        {
            MissingKeys = Array.Empty<string>();
        }

        public KeysNotFoundException(string message, Exception innerException) : base(message, innerException)
        {
            MissingKeys = Array.Empty<string>();
        }

        public KeysNotFoundException(IReadOnlyCollection<string> missingKeys, string message) : base(message)
        {
            MissingKeys = missingKeys;
        }
    }
}

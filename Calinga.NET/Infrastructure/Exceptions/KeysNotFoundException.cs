using System;
using System.Collections.Generic;

namespace Calinga.NET.Infrastructure.Exceptions
{
    public class KeysNotFoundException : Exception
    {
        public IReadOnlyCollection<string> MissingKeys { get; }

        public KeysNotFoundException(IReadOnlyCollection<string> missingKeys, string message) : base(message)
        {
            MissingKeys = missingKeys;
        }
    }
}

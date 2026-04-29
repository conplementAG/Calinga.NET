using System;

namespace Calinga.NET
{
    public static class Guard
    {
        public static void IsNotNullOrWhiteSpace(string parameter)
        {
            if (string.IsNullOrWhiteSpace(parameter)) throw new ArgumentNullException($"Parameter cannot be null, empty, or whitespace.");
        }

        public static void IsNotNull(object parameter, string name)
        {
            if (parameter == null) throw new ArgumentNullException($"Parameter {name} cannot be null or empty.");
        }
    }
}

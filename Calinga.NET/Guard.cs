using System;

namespace Calinga.NET
{
    public static class Guard
    {
        public static void IsNotNullOrWhiteSpace(string parameter)
        {
            parameter = parameter.Replace(" ", string.Empty);
            if (string.IsNullOrEmpty(parameter)) throw new ArgumentNullException($"Parameter {parameter} cannot be null or empty.");
        }

        public static void IsNotNull(object parameter, string name)
        {
            if (parameter == null) throw new ArgumentNullException($"Parameter {name} cannot be null or empty.");
        }
    }
}

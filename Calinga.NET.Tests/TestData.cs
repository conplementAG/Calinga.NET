using static System.FormattableString;

using System.Collections.Generic;

namespace Calinga.NET.Tests
{
    internal static class TestData
    {
        internal const string Language_DE = "de";
        internal const string Language_EN = "en";
        internal const string Key_1 = "UnitTest_Key1";
        internal const string Key_2 = "UnitTest_Key2";
        internal const string Translation_Key_1 = "translation for key 1";
        internal const string Translation_Key_2 = "translation for key 1";

        internal static IReadOnlyDictionary<string, string> Translations_De => CreateTranslations(Language_DE);

        internal static IReadOnlyDictionary<string, string> Translations_En => CreateTranslations(Language_EN);

        internal static IEnumerable<string> Languages => new List<string> { Language_DE, Language_EN };

        private static IReadOnlyDictionary<string, string> CreateTranslations(string language) => new Dictionary<string, string>
        {
            {Key_1, Invariant($"{language} {Translation_Key_1}")},
            {Key_2, Invariant($"{language} {Translation_Key_2}")}
        };
    }
}

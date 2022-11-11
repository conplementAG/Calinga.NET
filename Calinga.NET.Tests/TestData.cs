using System.Collections.Generic;
using System.Collections.ObjectModel;
using Calinga.NET.Caching;
using static System.FormattableString;

namespace Calinga.NET.Tests
{
    internal static class TestData
    {
        internal const string Language_DE = "de";
        internal const string Language_EN = "en";
        internal const string Language_FR = "fr";
        internal const string Key_1 = "UnitTest_Key1";
        internal const string Key_2 = "UnitTest_Key2";
        internal const string Translation_Key_1 = "translation for key 1";
        internal const string Translation_Key_2 = "translation for key 1";

        internal static CacheResponse Cache_Translations_De = new CacheResponse(Translations_De, true);
        internal static CacheResponse Cache_Translations_En = new CacheResponse(Translations_En, true);

        internal static IReadOnlyDictionary<string, string> Translations_De => CreateTranslations(Language_DE);

        internal static IReadOnlyDictionary<string, string> Translations_En => CreateTranslations(Language_EN);

        internal static IReadOnlyDictionary<string, string> EmptyTranslations =>
            new ReadOnlyDictionary<string, string>(new Dictionary<string, string>());

        internal static IEnumerable<Language> Languages => new List<Language>
        {
            new Language { Name = Language_DE, IsReference = false },
            new Language { Name = Language_EN, IsReference = true }
        };

        private static IReadOnlyDictionary<string, string> CreateTranslations(string language)
        {
            return new Dictionary<string, string>
            {
                { Key_1, Invariant($"{language} {Translation_Key_1}") },
                { Key_2, Invariant($"{language} {Translation_Key_2}") }
            };
        }
    }
}
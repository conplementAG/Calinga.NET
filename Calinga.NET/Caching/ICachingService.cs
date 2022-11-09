using System.Collections.Generic;
using System.Threading.Tasks;

namespace Calinga.NET.Caching
{
    public interface ICachingService
    {
        Task<CacheResponse> GetTranslations(string languageName, bool includeDrafts);

        Task<CachedLanguageListResponse> GetLanguages();
        Task StoreLanguagesAsync(IEnumerable<Language> languageList);

        Task StoreTranslationsAsync(string language, IReadOnlyDictionary<string, string> translations);

        Task ClearCache();
    }
}
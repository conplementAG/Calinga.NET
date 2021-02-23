using System.Collections.Generic;
using System.Threading.Tasks;

namespace Calinga.NET.Caching
{
    public interface ICachingService
    {
        Task<CacheResponse> GetTranslations(string language, bool includeDrafts);

        Task StoreTranslationsAsync(string language, IReadOnlyDictionary<string, string> translations);

        Task ClearCache();
    }
}

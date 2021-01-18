using System.Collections.Generic;

using System.Threading.Tasks;

namespace Calinga.NET.Infrastructure
{
    public interface ICachingService
    {
        Task<IReadOnlyDictionary<string, string>> GetTranslations(string language, bool includeDrafts);

        Task StoreTranslationsAsync(string language, IReadOnlyDictionary<string, string> translations);

        void ClearCache();
    }
}

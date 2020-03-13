using System.Collections.Generic;

using System.Threading.Tasks;

namespace Calinga.Infrastructure
{
    public interface ICachingService
    {
        Task<IReadOnlyDictionary<string, string>> GetTranslations(string language);

        Task<IEnumerable<string>> GetLanguages();

        void ClearCache();
    }
}

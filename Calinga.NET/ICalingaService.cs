using System.Collections.Generic;
using System.Threading.Tasks;

namespace Calinga.NET
{
    public interface  ICalingaService
    {
        Task<string> TranslateAsync(string key, string language);

        Task<IReadOnlyDictionary<string, string>> GetTranslationsAsync(string language);

        Task<IEnumerable<string>> GetLanguagesAsync();

        ILanguageContext CreateContext(string language);

        Task ClearCache();
    }
}
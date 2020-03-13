using System.Collections.Generic;
using System.Threading.Tasks;

namespace Calinga.NET
{
    public interface  ICalingaService
    {
        Task<string> TranslateAsync(string key, string language);

        Task<IEnumerable<string>> GetLanguagesAsync();

        ILanguageContext CreateContext(string language);

        void ClearCache();
    }
}
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Calinga.NET.Infrastructure
{
    public interface IConsumerHttpClient
    {
        Task<IReadOnlyDictionary<string, string>> GetTranslationsAsync(string language);

        Task<IEnumerable<string>> GetLanguagesAsync();

        Task<string> GetReferenceLanguageAsync();
    }
}
using System.Collections.Generic;
using System.Threading.Tasks;
using Calinga.NET.Caching;

namespace Calinga.NET.Infrastructure
{
    public interface IConsumerHttpClient
    {
        Task<IReadOnlyDictionary<string, string>> GetTranslationsAsync(string language);

        Task<IReadOnlyDictionary<string, string>> GetTranslationsAsync(string language, IEnumerable<string> keys);

        Task<IEnumerable<Language>> GetLanguagesAsync();
    }
}
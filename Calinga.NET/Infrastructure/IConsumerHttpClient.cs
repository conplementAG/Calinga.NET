using System.Collections.Generic;
using System.Threading.Tasks;
using Calinga.NET.Caching;

namespace Calinga.NET.Infrastructure
{
    public interface IConsumerHttpClient
    {
        Task<IReadOnlyDictionary<string, string>> GetTranslationsAsync(string language);

        Task<IEnumerable<Language>> GetLanguagesAsync();
    }
}
using System.Collections.Generic;
using System.Threading.Tasks;
using Calinga.NET.Caching;

namespace Calinga.NET.Infrastructure
{
    public interface IConsumerHttpClient
    {
        Task<TranslationsHttpResponse> GetTranslationsAsync(string language);

        Task<TranslationsHttpResponse> GetTranslationsAsync(string language, string? ifNoneMatch);

        Task<IReadOnlyDictionary<string, string>> GetTranslationsAsync(string language, IEnumerable<string> keys);

        Task<IEnumerable<Language>> GetLanguagesAsync();
    }
}
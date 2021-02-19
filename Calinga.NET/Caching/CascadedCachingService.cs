using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Calinga.NET.Infrastructure;

namespace Calinga.NET.Caching
{
    public class CascadedCachingService : ICachingService
    {
        private readonly ICachingService[] _cachingServices;

        public CascadedCachingService(params ICachingService[] cachingServices)
        {
            _cachingServices = cachingServices;
        }

        public async Task<IReadOnlyDictionary<string, string>> GetTranslations(string language, bool includeDrafts)
        {
            foreach (var cachingService in _cachingServices)
            {
                var result = await cachingService.GetTranslations(language, includeDrafts);
                if (result.Any())
                {
                    return result;
                }
            }

            return new ReadOnlyDictionary<string, string>(new Dictionary<string, string>());
        }

        public Task StoreTranslationsAsync(string language, IReadOnlyDictionary<string, string> translations)
        {
            var tasks = _cachingServices.Select(x => x.StoreTranslationsAsync(language, translations));
            return Task.WhenAll(tasks.ToArray());
        }

        public Task ClearCache()
        {
            var tasks = _cachingServices.Select(x => x.ClearCache());
            return Task.WhenAll(tasks.ToArray());
        }
    }
}

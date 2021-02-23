using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

using static System.FormattableString;

namespace Calinga.NET.Caching
{
    public class InMemoryCachingService : ICachingService
    {
        private readonly IMemoryCache _translationsCache;
        private static CancellationTokenSource _resetCacheToken = new CancellationTokenSource();

        public InMemoryCachingService()
        {
            _translationsCache = new MemoryCache(new MemoryCacheOptions());
        }

        public async Task<CacheResponse> GetTranslations(string language, bool includeDrafts)
        {
            if (_translationsCache.TryGetValue(CreateKey(language), out var translationsFromCache))
            {
                return new CacheResponse((IReadOnlyDictionary<string, string>)translationsFromCache, true);
            }

            return CacheResponse.Empty;
        }

        public Task StoreTranslationsAsync(string language, IReadOnlyDictionary<string, string> translations)
        {
            var options = new MemoryCacheEntryOptions().SetPriority(CacheItemPriority.Normal);
            options.AddExpirationToken(new CancellationChangeToken(_resetCacheToken.Token));

            _translationsCache.Set(CreateKey(language), translations, options);
            return Task.CompletedTask;
        }

        public Task ClearCache()
        {
            if (!_resetCacheToken.IsCancellationRequested && _resetCacheToken.Token.CanBeCanceled)
            {
                _resetCacheToken.Cancel();
                _resetCacheToken.Dispose();
            }

            _resetCacheToken = new CancellationTokenSource();
            return Task.CompletedTask;
        }

        private string CreateKey(string language)
        {
            return Invariant($"Language_{language}");
        }
    }
}
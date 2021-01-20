using static System.FormattableString;

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.Caching.Memory;

namespace Calinga.NET.Infrastructure
{
    public class CachingService : ICachingService
    {
        private readonly IMemoryCache _translationsCache;
        private readonly IFileSystemService _fileService;
        private readonly ConcurrentDictionary<string, bool> _allKeys = new ConcurrentDictionary<string, bool>();

        public CachingService(IFileSystemService fileService)
        {
            _translationsCache = new MemoryCache(new MemoryCacheOptions());
            _fileService = fileService;
        }

        public async Task<IReadOnlyDictionary<string, string>> GetTranslations(string language, bool includeDrafts)
        {
            var cacheKey = Invariant($"Language_{language}");

            if (_translationsCache.TryGetValue(cacheKey, out var translationsFromCache))
            {
                return (IReadOnlyDictionary<string, string>)translationsFromCache;
            }

            var translationsFromFile = await _fileService.ReadCacheFileAsync(language).ConfigureAwait(false);

            StoreInCache(cacheKey, translationsFromFile);

            return translationsFromFile;
        }

        public void ClearCache()
        {
            foreach (var key in CachedKeys)
            {
                _translationsCache.Remove(RemoveKey(key));
            }
            ClearKeys();
        }

        public Task StoreTranslationsAsync(string language, IReadOnlyDictionary<string, string> translations)
        {
            return _fileService.SaveTranslationsAsync(language, translations);
        }

        #region Private helper Methods

        private IEnumerable<string> CachedKeys => _allKeys.Keys;

        private void StoreInCache(string cacheKey, IReadOnlyDictionary<string, string> translations)
        {
            _allKeys.TryAdd(cacheKey, true);
            _translationsCache.Set(cacheKey, translations, new MemoryCacheEntryOptions());
        }

        private string RemoveKey(string key)
        {
            if (!_allKeys.TryRemove(key, out _))
            {
                //if not possible to remove key from dictionary, then try to mark key as not existing in cache
                _allKeys.TryUpdate(key, false, true);
            }

            return key;
        }

        private void ClearKeys()
        {
            foreach (var key in _allKeys.Where(p => !p.Value).Select(p => p.Key).ToList())
            {
                RemoveKey(key);
            }
        }

        #endregion Private helper Methods
    }
}

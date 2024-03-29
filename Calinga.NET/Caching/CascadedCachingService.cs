﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Calinga.NET.Caching
{
    public class CascadedCachingService : ICachingService
    {
        private readonly ICachingService[] _cachingServices;

        public CascadedCachingService(params ICachingService[] cachingServices)
        {
            _cachingServices = cachingServices;
        }

        public async Task<CacheResponse> GetTranslations(string language, bool includeDrafts)
        {
            foreach (var cachingService in _cachingServices)
            {
                var cacheResponse = await cachingService.GetTranslations(language, includeDrafts);

                if (cacheResponse.FoundInCache)
                {
                    return cacheResponse;
                }
            }

            return CacheResponse.Empty;
        }

        public async Task<CachedLanguageListResponse> GetLanguages()
        {
            foreach (var cachingService in _cachingServices)
            {
                var cacheResponse = await cachingService.GetLanguages();

                if (cacheResponse.FoundInCache)
                {
                    return cacheResponse;
                }
            }

            return CachedLanguageListResponse.Empty;
        }

        public Task StoreLanguagesAsync(IEnumerable<Language> languageList)
        {
            var tasks = _cachingServices.Select(x => x.StoreLanguagesAsync(languageList));

            return Task.WhenAll(tasks.ToArray());
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
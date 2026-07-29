using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Calinga.NET.Infrastructure;

namespace Calinga.NET.Caching
{
    public class InMemoryCachingService : ICachingService
    {
        private readonly IDateTimeService _dateTimeService;

        private readonly uint? _memoryCacheExpirationIntervalInSeconds;
        private readonly bool _withExpirationDate;
        private readonly object _lock = new object();

        private DateTime _expirationDate;
        private volatile IReadOnlyList<Language> _languagesList;
        private ConcurrentDictionary<string, IReadOnlyDictionary<string, string>> _translations;
        private ConcurrentDictionary<string, string> _etags;

        public InMemoryCachingService(IDateTimeService timeService, CalingaServiceSettings settings)
        {
            _dateTimeService = timeService;
            _memoryCacheExpirationIntervalInSeconds = settings.MemoryCacheExpirationIntervalInSeconds;
            _expirationDate = GetExpirationDate(_memoryCacheExpirationIntervalInSeconds);
            _withExpirationDate = _expirationDate != DateTime.MaxValue;
            _translations = new ConcurrentDictionary<string, IReadOnlyDictionary<string, string>>();
            _etags = new ConcurrentDictionary<string, string>();
            _languagesList = new List<Language>();
        }

        public Task<CacheResponse> GetTranslations(string language, bool includeDrafts)
        {
            if (!_translations.TryGetValue(language, out var translations))
            {
                return Task.FromResult(CacheResponse.Empty);
            }

            var etag = _etags.TryGetValue(language, out var storedEtag) ? storedEtag : null;
            // On expiry we preserve the entry so callers can revalidate via If-None-Match.
            // The next StoreTranslationsAsync resets _expirationDate, flipping IsStale back to false.
            var isStale = _withExpirationDate && IsCacheExpired();
            return Task.FromResult(new CacheResponse(translations, true, etag, isStale));
        }

        public Task<CachedLanguageListResponse> GetLanguages()
        {
            if (_withExpirationDate && IsCacheExpired())
            {
                return Task.FromResult(CachedLanguageListResponse.Empty);
            }

            var languages = _languagesList;
            return Task.FromResult(languages.Any()
                ? new CachedLanguageListResponse(languages, true)
                : CachedLanguageListResponse.Empty);
        }

        public Task ClearCache()
        {
            ClearCacheInternal();
            return Task.CompletedTask;
        }

        private void ClearCacheInternal()
        {
            lock (_lock)
            {
                _translations = new ConcurrentDictionary<string, IReadOnlyDictionary<string, string>>();
                _etags = new ConcurrentDictionary<string, string>();
                _languagesList = new List<Language>();
                _expirationDate = DateTime.MinValue;
            }
        }

        public Task StoreLanguagesAsync(IEnumerable<Language> languageList)
        {
            lock (_lock)
            {
                _languagesList = languageList.ToList();
                _expirationDate = GetExpirationDate(_memoryCacheExpirationIntervalInSeconds);
            }

            return Task.CompletedTask;
        }

        public Task StoreTranslationsAsync(string language, IReadOnlyDictionary<string, string> translations) =>
            StoreTranslationsAsync(language, translations, null);

        public Task StoreTranslationsAsync(string language, IReadOnlyDictionary<string, string> translations, string? etag)
        {
            _translations[language] = translations;
            if (etag == null)
            {
                _etags.TryRemove(language, out _);
            }
            else
            {
                _etags[language] = etag;
            }
            lock (_lock)
            {
                _expirationDate = GetExpirationDate(_memoryCacheExpirationIntervalInSeconds);
            }

            return Task.CompletedTask;
        }

        #region Privat helper Methods

        private bool IsCacheExpired()
        {
            DateTime expiration;
            lock (_lock)
            {
                expiration = _expirationDate;
            }
            return _dateTimeService.GetCurrentDateTime() >= expiration;
        }

        private DateTime GetExpirationDate(uint? expiration)
        {
            return expiration == null || expiration == 0 ? DateTime.MaxValue : _dateTimeService.GetCurrentDateTime().AddSeconds(expiration.Value);
        }

        #endregion Privat helper Methods
    }
}

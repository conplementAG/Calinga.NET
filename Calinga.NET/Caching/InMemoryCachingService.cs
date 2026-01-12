using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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

        public InMemoryCachingService(IDateTimeService timeService, CalingaServiceSettings settings)
        {
            _dateTimeService = timeService;
            _memoryCacheExpirationIntervalInSeconds = settings.MemoryCacheExpirationIntervalInSeconds;
            _expirationDate = GetExpirationDate(_memoryCacheExpirationIntervalInSeconds);
            _withExpirationDate = _expirationDate != DateTime.MaxValue;
            _translations = new ConcurrentDictionary<string, IReadOnlyDictionary<string, string>>();
            _languagesList = new List<Language>();
        }

        public Task<CacheResponse> GetTranslations(string language, bool includeDrafts)
        {
            if (_withExpirationDate && IsCacheExpired())
            {
                ClearCacheInternal();
                return Task.FromResult(CacheResponse.Empty);
            }

            return Task.FromResult(_translations.TryGetValue(language, out var translations)
                ? new CacheResponse(translations, true)
                : CacheResponse.Empty);
        }

        public Task<CachedLanguageListResponse> GetLanguages()
        {
            if (_withExpirationDate && IsCacheExpired())
            {
                ClearCacheInternal();
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

        public Task StoreTranslationsAsync(string language, IReadOnlyDictionary<string, string> translations)
        {
            _translations[language] = translations;
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

        private static DateTime ConvertToDateTime(object? date)
        {
            return Convert.ToDateTime(date);
        }

        private DateTime GetExpirationDate(uint? expiration)
        {
            return expiration == null || expiration == 0 ? DateTime.MaxValue : _dateTimeService.GetCurrentDateTime().AddSeconds(expiration.Value);
        }

        #endregion Privat helper Methods
    }
}
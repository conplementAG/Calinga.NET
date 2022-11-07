using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Calinga.NET.Infrastructure;

namespace Calinga.NET.Caching
{
    public class InMemoryCachingService : ICachingService
    {
        private readonly IDateTimeService _dateTimeService;
        private readonly uint? _memoryCacheExpirationIntervalInSeconds;
        private readonly bool _withExpirationDate;

        private DateTime _expirationDate;
        private Dictionary<string, IReadOnlyDictionary<string, string>> _translations;

        public InMemoryCachingService(IDateTimeService timeService, CalingaServiceSettings settings)
        {
            _dateTimeService = timeService;
            _memoryCacheExpirationIntervalInSeconds = settings.MemoryCacheExpirationIntervalInSeconds;
            _expirationDate = GetExpirationDate(_memoryCacheExpirationIntervalInSeconds);
            _withExpirationDate = _expirationDate != DateTime.MaxValue;
            _translations = new Dictionary<string, IReadOnlyDictionary<string, string>>();
        }

        public Task<CachedLanguageListResponse> GetLanguagesList()
        {
            throw new NotImplementedException();
        }

        public Task StoreLanguagesListAsync()
        {
            throw new NotImplementedException();
        }

        public async Task<CacheResponse> GetTranslations(string language, bool includeDrafts)
        {
            if (_withExpirationDate && IsCacheExpired())
            {
                await ClearCache().ConfigureAwait(false);

                return CacheResponse.Empty;
            }

            return _translations.ContainsKey(language) ? new CacheResponse(_translations[language], true) : CacheResponse.Empty;
        }

        public Task StoreTranslationsAsync(string language, IReadOnlyDictionary<string, string> translations)
        {
            _translations.Add(language, translations);
            _expirationDate = GetExpirationDate(_memoryCacheExpirationIntervalInSeconds);

            return Task.CompletedTask;
        }

        public Task ClearCache()
        {
            _translations = new Dictionary<string, IReadOnlyDictionary<string, string>>();
            _expirationDate = DateTime.MinValue;

            return Task.CompletedTask;
        }

        #region Privat helper Methods

        private bool IsCacheExpired()
        {
            return _dateTimeService.GetCurrentDateTime() >= ConvertToDateTime(_expirationDate);
        }

        private static DateTime ConvertToDateTime(object? date)
        {
            return Convert.ToDateTime(date);
        }

        private static DateTime GetExpirationDate(uint? expiration)
        {
            return expiration == null || expiration == 0 ? DateTime.MaxValue : DateTime.Now.AddSeconds(expiration.Value);
        }

        #endregion Privat helper Methods
    }
}
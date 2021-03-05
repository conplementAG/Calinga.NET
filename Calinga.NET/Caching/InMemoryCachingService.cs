using static System.FormattableString;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

using Calinga.NET.Infrastructure;

namespace Calinga.NET.Caching
{
    public class InMemoryCachingService : ICachingService
    {
        private readonly IMemoryCache _translationsCache;
        private readonly IDateTimeService _dateTimeService;
        private static CancellationTokenSource _resetCacheToken = new CancellationTokenSource();

        private const string ExpirationDateKey = "ExpirationTime";
        private readonly DateTime _expirationDate;
        private readonly bool _withExpirationDate;

        public InMemoryCachingService(IDateTimeService timeService, CalingaServiceSettings settings)
        {
            _translationsCache = new MemoryCache(new MemoryCacheOptions());
            _dateTimeService = timeService;

            _expirationDate = GetExpirationDate(settings.MemoryCacheExpirationIntervalInSeconds);
            _withExpirationDate = _expirationDate != DateTime.MaxValue;
        }

        public async Task<CacheResponse> GetTranslations(string language, bool includeDrafts)
        {
            if (_withExpirationDate && IsCacheExpired())
            {
                await ClearCache().ConfigureAwait(false);
                return CacheResponse.Empty;
            }

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

            if (_withExpirationDate)
                _translationsCache.Set(ExpirationDateKey, _expirationDate, options);

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

        #region Privat helper Methods

        private string CreateKey(string language)
        {
            return Invariant($"Language_{language}");
        }

        private bool IsCacheExpired()
        {
            _translationsCache.TryGetValue(ExpirationDateKey, out var expiryDate);

            return _dateTimeService.GetCurrentDateTime() >= ConvertToDateTime(expiryDate);
        }

        private DateTime ConvertToDateTime(object? date)
        {
            return Convert.ToDateTime(date);
        }

        private DateTime GetExpirationDate(uint? expiration)
        {
            return expiration == null || expiration == 0 ? DateTime.MaxValue : DateTime.Now.AddSeconds(expiration.Value);
        }

        #endregion Privat helper Methods
    }
}
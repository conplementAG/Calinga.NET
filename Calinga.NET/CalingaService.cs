using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

using Calinga.NET.Caching;
using Calinga.NET.Infrastructure;
using Calinga.NET.Infrastructure.Exceptions;

namespace Calinga.NET
{
    public class CalingaService : ICalingaService
    {
        private readonly ICachingService _cachingService;
        private readonly IConsumerHttpClient _consumerHttpClient;
        private readonly CalingaServiceSettings _settings;
        private IEnumerable<string>? _languages;

        public CalingaService(ICachingService cachingService, IConsumerHttpClient consumerHttpClient, CalingaServiceSettings settings)
        {
            ValidateSettings(settings);
            _cachingService = cachingService;
            _consumerHttpClient = consumerHttpClient;
            _settings = settings;
        }

        public CalingaService(CalingaServiceSettings settings)
            : this(new CascadedCachingService(new InMemoryCachingService(new DateTimeService(), settings), new FileCachingService(settings)), new ConsumerHttpClient(settings), settings)
        {
        }

        public CalingaService(CalingaServiceSettings settings, HttpClient httpClient)
            : this(new CascadedCachingService(new InMemoryCachingService(new DateTimeService(), settings), new FileCachingService(settings)), new ConsumerHttpClient(settings, httpClient), settings)
        {
        }

        public CalingaService(ICachingService cachingService, CalingaServiceSettings settings)
            : this(cachingService, new ConsumerHttpClient(settings), settings)
        {
        }

        public ILanguageContext CreateContext(string language)
        {
            Guard.IsNotNullOrWhiteSpace(language);

            return new LanguageContext(language, this);
        }

        public async Task<string> TranslateAsync(string key, string language)
        {
            Guard.IsNotNullOrWhiteSpace(language);
            Guard.IsNotNullOrWhiteSpace(key);

            if (_settings.IsDevMode) return key;

            try
            {
                var translations = await GetTranslationsAsync(language).ConfigureAwait(false);
                var translation = translations.FirstOrDefault(k => k.Key == key);
                if (translation.Equals(default(KeyValuePair<string, string>))) return key;

                return translation.Value;
            }
            catch (TranslationsNotAvailableException)
            {
                return key;
            }
        }

        public async Task<IReadOnlyDictionary<string, string>> GetTranslationsAsync(string language)
        {
            Guard.IsNotNullOrWhiteSpace(language);

            IReadOnlyDictionary<string, string> cachedTranslations;
            var cacheResponse = await _cachingService.GetTranslations(language, _settings.IncludeDrafts).ConfigureAwait(false);

            if (cacheResponse.FoundInCache)
            {
                cachedTranslations = cacheResponse.Result;
            }
            else
            {
                cachedTranslations = await _consumerHttpClient.GetTranslationsAsync(language).ConfigureAwait(false);

                if (cachedTranslations == null || !cachedTranslations.Any())
                {
                    throw new TranslationsNotAvailableException(
                        $"Translation not found, path: {_settings.Organization}, {_settings.Team}, {_settings.Project}, {language}");
                }

                await _cachingService.StoreTranslationsAsync(language, cachedTranslations).ConfigureAwait(false);
            }

            return _settings.IsDevMode ? cachedTranslations.ToDictionary(k => k.Key, k => k.Key) : cachedTranslations;
        }

        public async Task<IEnumerable<string>> GetLanguagesAsync()
        {
            return _languages ??= await _consumerHttpClient.GetLanguagesAsync().ConfigureAwait(false);
        }

        public Task ClearCache()
        {
            _languages = null;

            return _cachingService.ClearCache();
        }

        private static void ValidateSettings(CalingaServiceSettings setting)
        {
            Guard.IsNotNull(setting, nameof(setting));
            Guard.IsNotNullOrWhiteSpace(setting.Project);
            Guard.IsNotNullOrWhiteSpace(setting.Organization);
            Guard.IsNotNullOrWhiteSpace(setting.CacheDirectory);
        }
    }
}
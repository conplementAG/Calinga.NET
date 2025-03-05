using System;
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
        private readonly ILogger _logger;
        private readonly CalingaServiceSettings _settings;
        private string _referenceLanguage;


        public CalingaService(ICachingService cachingService, IConsumerHttpClient consumerHttpClient, CalingaServiceSettings settings, ILogger logger)
        {
            ValidateSettings(settings);
            _cachingService = cachingService;
            _consumerHttpClient = consumerHttpClient;
            _settings = settings;
            _logger = logger;
        }

        public CalingaService(ICachingService cachingService, IConsumerHttpClient consumerHttpClient, CalingaServiceSettings settings) : this(
            cachingService, consumerHttpClient, settings, new DefaultLogger())
        {
        }

        public CalingaService(CalingaServiceSettings settings)
            : this(
                new CascadedCachingService(new InMemoryCachingService(new DateTimeService(), settings),
                    new FileCachingService(settings, new DefaultLogger())),
                new ConsumerHttpClient(settings), settings, new DefaultLogger())
        {
        }

        public CalingaService(CalingaServiceSettings settings, ILogger logger)
            : this(
                new CascadedCachingService(new InMemoryCachingService(new DateTimeService(), settings),
                    new FileCachingService(settings, logger)),
                new ConsumerHttpClient(settings), settings, logger)
        {
        }

        public CalingaService(CalingaServiceSettings settings, HttpClient httpClient)
            : this(
                new CascadedCachingService(new InMemoryCachingService(new DateTimeService(), settings),
                    new FileCachingService(settings, new DefaultLogger())),
                new ConsumerHttpClient(settings, httpClient), settings, new DefaultLogger())
        {
        }

        public CalingaService(CalingaServiceSettings settings, HttpClient httpClient, ILogger logger)
            : this(
                new CascadedCachingService(new InMemoryCachingService(new DateTimeService(), settings),
                    new FileCachingService(settings, logger)),
                new ConsumerHttpClient(settings, httpClient), settings, logger)
        {
        }


        public CalingaService(ICachingService cachingService, CalingaServiceSettings settings)
            : this(cachingService, new ConsumerHttpClient(settings), settings, new DefaultLogger())
        {
        }

        public CalingaService(ICachingService cachingService, CalingaServiceSettings settings, ILogger logger)
            : this(cachingService, new ConsumerHttpClient(settings), settings, logger)
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

            if (_settings.IsDevMode)
                return key;

            try
            {
                var translations = await GetTranslationsAsync(language).ConfigureAwait(false);
                var translation = translations.FirstOrDefault(k => k.Key == key);

                if (translation.Equals(default(KeyValuePair<string, string>)))
                    return key;

                return translation.Value;
            }
            catch (TranslationsNotAvailableException e)
            {
                _logger.Warn($"Translations for {language} not found, returning key: {key}. Error: {e.Message}");
                return key;
            }
        }

        public async Task<IReadOnlyDictionary<string, string>> GetTranslationsAsync(string language)
        {
            while (true)
            {
                Guard.IsNotNullOrWhiteSpace(language);

                IReadOnlyDictionary<string, string>? foundTranslations = null;
                CacheResponse? cacheResponse = null;

                try
                {
                    cacheResponse = await _cachingService.GetTranslations(language, _settings.IncludeDrafts).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    _logger.Warn($"Error while fetching translations for language {language} from cache. Trying to fetch from consumer API. Error: {e.Message}");
                }

                if (cacheResponse is { FoundInCache: true })
                {
                    foundTranslations = cacheResponse.Result;

                    _logger.Info($"Translations for language {language} fetched from cache");
                    
                    return _settings.IsDevMode ? foundTranslations.ToDictionary(k => k.Key, k => k.Key) : foundTranslations;
                }

                if (!_settings.UseCacheOnly)
                {
                    try
                    {
                        foundTranslations = await _consumerHttpClient.GetTranslationsAsync(language).ConfigureAwait(false);

                        if (foundTranslations != null && foundTranslations.Any())
                        {
                            _logger.Info($"Translations for language {language} fetched from consumer API");
                            
                            await _cachingService.StoreTranslationsAsync(language, foundTranslations).ConfigureAwait(false);
                            return _settings.IsDevMode ? foundTranslations.ToDictionary(k => k.Key, k => k.Key) : foundTranslations;
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.Warn($"Error when fetching translations for language {language} from consumer API: {e.Message}");

                        if (!_settings.FallbackToReferenceLanguage)
                            throw;
                    }
                }
                
                var referenceLanguage = await GetReferenceLanguage().ConfigureAwait(false);

                if (!_settings.FallbackToReferenceLanguage || referenceLanguage == language)
                    throw new TranslationsNotAvailableException(
                        $"Translation not found, path: {_settings.Organization}, {_settings.Team}, {_settings.Project}, {language}");

                _logger.Warn("Translations not found, trying to fetch reference language");

                language = referenceLanguage;
            }
        }

        public async Task<IEnumerable<string>> GetLanguagesAsync()
        {
            return (await FetchLanguagesAsync().ConfigureAwait(false)).Select(x => x.Name);
        }

        public async Task<string> GetReferenceLanguage()
        {
            if (!string.IsNullOrWhiteSpace(_referenceLanguage))
                return _referenceLanguage;

            var languages = (await FetchLanguagesAsync().ConfigureAwait(false))
                .ToArray();

            if (languages.All(l => !l.IsReference))
            {
                throw new LanguagesNotAvailableException("Reference language not found");
            }

            _referenceLanguage = languages.Single(l => l.IsReference).Name;

            return _referenceLanguage;
        }

        public Task ClearCache()
        {
            return _cachingService.ClearCache();
        }

        private async Task<IEnumerable<Language>> FetchLanguagesAsync()
        {
            IEnumerable<Language>? foundList = null;
            var cachedListResponse = await _cachingService.GetLanguages().ConfigureAwait(false);

            if (cachedListResponse.FoundInCache)
            {
                foundList = cachedListResponse.Result;
            }
            else
            {
                if (!_settings.UseCacheOnly)
                {
                    foundList = await _consumerHttpClient.GetLanguagesAsync().ConfigureAwait(false);

                    if (foundList != null && foundList.Any())
                    {
                        await _cachingService.StoreLanguagesAsync(foundList);
                    }
                }
            }

            if (foundList == null || !foundList.Any())
            {
                throw new LanguagesNotAvailableException(
                    $"Languages not found, path: {_settings.Organization}, {_settings.Team}, {_settings.Project}");
            }

            return foundList;
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
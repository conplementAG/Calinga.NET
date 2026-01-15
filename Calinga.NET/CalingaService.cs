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
        private string? _referenceLanguage;

        /// <summary>
        /// Initializes a new instance of the <see cref="CalingaService"/> class with all dependencies.
        /// </summary>
        /// <param name="cachingService">The caching service to use for translations and languages.</param>
        /// <param name="consumerHttpClient">The HTTP client for fetching translations and languages from the API.</param>
        /// <param name="settings">The Calinga service settings.</param>
        /// <param name="logger">The logger instance.</param>
        public CalingaService(ICachingService cachingService, IConsumerHttpClient consumerHttpClient, CalingaServiceSettings settings, ILogger logger)
        {
            ValidateSettings(settings);
            _cachingService = cachingService;
            _consumerHttpClient = consumerHttpClient;
            _settings = settings;
            _logger = logger;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CalingaService"/> class with a default logger.
        /// </summary>
        /// <param name="cachingService">The caching service to use for translations and languages.</param>
        /// <param name="consumerHttpClient">The HTTP client for fetching translations and languages from the API.</param>
        /// <param name="settings">The Calinga service settings.</param>
        public CalingaService(ICachingService cachingService, IConsumerHttpClient consumerHttpClient, CalingaServiceSettings settings) : this(
            cachingService, consumerHttpClient, settings, new DefaultLogger())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CalingaService"/> class with default caching and HTTP client implementations.
        /// </summary>
        /// <param name="settings">The Calinga service settings.</param>
        public CalingaService(CalingaServiceSettings settings)
            : this(
                new CascadedCachingService(new InMemoryCachingService(new DateTimeService(), settings),
                    new FileCachingService(settings, new DefaultLogger())),
                new ConsumerHttpClient(settings), settings, new DefaultLogger())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CalingaService"/> class with a custom logger.
        /// </summary>
        /// <param name="settings">The Calinga service settings.</param>
        /// <param name="logger">The logger instance.</param>
        public CalingaService(CalingaServiceSettings settings, ILogger logger)
            : this(
                new CascadedCachingService(new InMemoryCachingService(new DateTimeService(), settings),
                    new FileCachingService(settings, logger)),
                new ConsumerHttpClient(settings), settings, logger)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CalingaService"/> class with a custom HTTP client.
        /// </summary>
        /// <param name="settings">The Calinga service settings.</param>
        /// <param name="httpClient">The HTTP client instance.</param>
        public CalingaService(CalingaServiceSettings settings, HttpClient httpClient)
            : this(
                new CascadedCachingService(new InMemoryCachingService(new DateTimeService(), settings),
                    new FileCachingService(settings, new DefaultLogger())),
                new ConsumerHttpClient(settings, httpClient), settings, new DefaultLogger())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CalingaService"/> class with a custom HTTP client and logger.
        /// </summary>
        /// <param name="settings">The Calinga service settings.</param>
        /// <param name="httpClient">The HTTP client instance.</param>
        /// <param name="logger">The logger instance.</param>
        public CalingaService(CalingaServiceSettings settings, HttpClient httpClient, ILogger logger)
            : this(
                new CascadedCachingService(new InMemoryCachingService(new DateTimeService(), settings),
                    new FileCachingService(settings, logger)),
                new ConsumerHttpClient(settings, httpClient), settings, logger)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CalingaService"/> class with a custom caching service.
        /// </summary>
        /// <param name="cachingService">The caching service to use for translations and languages.</param>
        /// <param name="settings">The Calinga service settings.</param>
        public CalingaService(ICachingService cachingService, CalingaServiceSettings settings)
            : this(cachingService, new ConsumerHttpClient(settings), settings, new DefaultLogger())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CalingaService"/> class with a custom caching service and logger.
        /// </summary>
        /// <param name="cachingService">The caching service to use for translations and languages.</param>
        /// <param name="settings">The Calinga service settings.</param>
        /// <param name="logger">The logger instance.</param>
        public CalingaService(ICachingService cachingService, CalingaServiceSettings settings, ILogger logger)
            : this(cachingService, new ConsumerHttpClient(settings), settings, logger)
        {
        }

        /// <summary>
        /// Creates a language context for the specified language.
        /// </summary>
        /// <param name="language">The language code.</param>
        /// <returns>A language context for translation operations.</returns>
        public ILanguageContext CreateContext(string language)
        {
            Guard.IsNotNullOrWhiteSpace(language);

            return new LanguageContext(language, this);
        }

        /// <summary>
        /// Translates a key into the specified language.
        /// </summary>
        /// <param name="key">The translation key.</param>
        /// <param name="language">The language code.</param>
        /// <returns>The translated string or the key if not found.</returns>
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

        /// <summary>
        /// Gets all translations for the specified language.
        /// </summary>
        /// <param name="language">The language code.</param>
        /// <param name="invalidateCache">If true, bypasses the cache and fetches from the API. Do not use in combination with "UseCacheOnly"</param>
        /// <returns>A dictionary of translation keys and values.</returns>
        public async Task<IReadOnlyDictionary<string, string>> GetTranslationsAsync(string language, bool invalidateCache)
        {
            Guard.IsNotNullOrWhiteSpace(language);
        
            if (invalidateCache && _settings.UseCacheOnly)
            {
                throw new ArgumentException("Cannot invalidate cache when global Setting 'UseCacheOnly' is set to true.", nameof(invalidateCache));
            }
        
            while (true)
            {
                var translations = await TryGetFromCache(language, invalidateCache).ConfigureAwait(false);
                if (translations != null)
                    return translations;
        
                translations = await TryGetFromApi(language).ConfigureAwait(false);
                if (translations != null)
                    return translations;
        
                var referenceLanguage = await GetReferenceLanguage().ConfigureAwait(false);
        
                if (!_settings.FallbackToReferenceLanguage || referenceLanguage == language)
                {
                    throw new TranslationsNotAvailableException(
                        $"Translation not found, path: {_settings.Organization}, {_settings.Team}, {_settings.Project}, {language}");
                }
        
                _logger.Warn("Translations not found, trying to fetch reference language");
                language = referenceLanguage;
            }
        }
        
        /// <summary>
        /// Gets all translations for the specified language.
        /// </summary>
        /// <param name="language">The language code.</param>
        /// <returns>A dictionary of translation keys and values.</returns>
        public async Task<IReadOnlyDictionary<string, string>> GetTranslationsAsync(string language)
        {
            return await GetTranslationsAsync(language, false);
        }
        
        private async Task<IReadOnlyDictionary<string, string>?> TryGetFromCache(string language, bool invalidateCache)
        {
            if (invalidateCache)
                return null;
        
            try
            {
                var cacheResponse = await _cachingService.GetTranslations(language, _settings.IncludeDrafts).ConfigureAwait(false);
                if (cacheResponse is { FoundInCache: true })
                {
                    _logger.Info($"Translations for language {language} fetched from cache");
                    var result = cacheResponse.Result;
                    return _settings.IsDevMode ? result.ToDictionary(k => k.Key, k => k.Key) : result;
                }
            }
            catch (Exception e)
            {
                _logger.Warn($"Error while fetching translations for language {language} from cache. Trying to fetch from consumer API. Error: {e.Message}");
            }
            return null;
        }
        
        private async Task<IReadOnlyDictionary<string, string>?> TryGetFromApi(string language)
        {
            if (_settings.UseCacheOnly)
                return null;
        
            try
            {
                var foundTranslations = await _consumerHttpClient.GetTranslationsAsync(language).ConfigureAwait(false);
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
            return null;
        }

        /// <summary>
        /// Gets the list of available languages.
        /// </summary>
        /// <returns>A list of language codes.</returns>
        public async Task<IEnumerable<string>> GetLanguagesAsync()
        {
            return (await FetchLanguagesAsync().ConfigureAwait(false)).Select(x => x.Name);
        }

        /// <summary>
        /// Gets the reference language for the current project.
        /// </summary>
        /// <returns>The reference language code.</returns>
        public async Task<string> GetReferenceLanguage()
        {
            if (!string.IsNullOrWhiteSpace(_referenceLanguage))
                return _referenceLanguage!;

            var languages = (await FetchLanguagesAsync().ConfigureAwait(false))
                .ToArray();

            if (languages.All(l => !l.IsReference))
            {
                throw new LanguagesNotAvailableException("Reference language not found");
            }

            _referenceLanguage = languages.Single(l => l.IsReference).Name;

            return _referenceLanguage;
        }

        /// <summary>
        /// Clears the translation and language cache.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
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

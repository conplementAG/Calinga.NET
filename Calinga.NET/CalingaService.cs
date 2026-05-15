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
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="invalidateCache"/> is true while
        /// <see cref="CalingaServiceSettings.UseCacheOnly"/> is true.
        /// </exception>
        /// <exception cref="TranslationsNotAvailableException">
        /// Thrown when translations cannot be retrieved from cache or API and either
        /// <see cref="CalingaServiceSettings.FallbackToReferenceLanguage"/> is false or the reference-language
        /// fallback could not produce translations either. When the failure originates from the language list
        /// being unavailable during fallback, the underlying <see cref="LanguagesNotAvailableException"/> is
        /// preserved as the inner exception.
        /// </exception>
        public async Task<IReadOnlyDictionary<string, string>> GetTranslationsAsync(string language, bool invalidateCache)
        {
            Guard.IsNotNullOrWhiteSpace(language);
        
            if (invalidateCache && _settings.UseCacheOnly)
            {
                throw new ArgumentException("Cannot invalidate cache when global Setting 'UseCacheOnly' is set to true.", nameof(invalidateCache));
            }
        
            while (true)
            {
                // Always read the cache: invalidateCache only suppresses the
                // fast-path return. The cached ETag is still useful for
                // If-None-Match revalidation, which lets the server answer 304
                // and save a full body transfer when nothing has changed.
                var cacheResponse = await TryReadCache(language).ConfigureAwait(false);

                if (!invalidateCache && cacheResponse.FoundTranslationsInCache && !cacheResponse.IsStale)
                {
                    _logger.Info($"Translations for language {language} fetched from cache");
                    var fresh = cacheResponse.Result;
                    return _settings.IsDevMode ? fresh.ToDictionary(k => k.Key, k => k.Key) : fresh;
                }

                var translations = await TryGetFromApi(language, cacheResponse).ConfigureAwait(false);
                if (translations != null)
                    return translations;

                if (!_settings.FallbackToReferenceLanguage)
                {
                    throw new TranslationsNotAvailableException(
                        $"Translation not found, path: {_settings.Organization}, {_settings.Team}, {_settings.Project}, {language}");
                }

                var referenceLanguage = await GetReferenceLanguage().ConfigureAwait(false);

                if (referenceLanguage == language)
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
        /// <exception cref="TranslationsNotAvailableException">
        /// Thrown when translations cannot be retrieved from cache or API and either
        /// <see cref="CalingaServiceSettings.FallbackToReferenceLanguage"/> is false or the reference-language
        /// fallback could not produce translations either. When the failure originates from the language list
        /// being unavailable during fallback, the underlying <see cref="LanguagesNotAvailableException"/> is
        /// preserved as the inner exception.
        /// </exception>
        public async Task<IReadOnlyDictionary<string, string>> GetTranslationsAsync(string language)
        {
            return await GetTranslationsAsync(language, false);
        }

        /// <summary>
        /// Gets the subset of translations for the specified keys by issuing a POST to the Consumer API.
        /// The cache is never consulted and never written — every call returns server-fresh data.
        ///
        /// The current state of never using the cache and always using server-fresh data is currently in testing and
        /// can be subject to change.
        ///
        /// In normal mode, keys absent from the server response are silently omitted.
        /// In <see cref="CalingaServiceSettings.IsDevMode"/>, the server response is validated:
        /// if any requested key is missing, a <see cref="KeysNotFoundException"/> is thrown so
        /// developers see typos and unknown keys at integration time rather than at runtime.
        /// </summary>
        /// <param name="language">The language code.</param>
        /// <param name="keys">The translation keys to fetch.</param>
        /// <returns>A dictionary containing only the requested keys that were found on the server.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="keys"/> is null.</exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when <see cref="CalingaServiceSettings.UseCacheOnly"/> is true. Keyed calls always
        /// require HTTP; they cannot be served from the cache, so this setting is incompatible with
        /// the keyed overload regardless of whether the key collection is empty or not.
        /// </exception>
        /// <exception cref="KeysNotFoundException">
        /// Thrown when <see cref="CalingaServiceSettings.IsDevMode"/> is true and the server response
        /// does not include every requested key. The exception's <see cref="KeysNotFoundException.MissingKeys"/>
        /// property exposes the missing keys for diagnostic purposes.
        /// </exception>
        public async Task<IReadOnlyDictionary<string, string>> GetTranslationsAsync(string language, IEnumerable<string> keys)
        {
            Guard.IsNotNullOrWhiteSpace(language);
            if (keys == null) throw new ArgumentNullException(nameof(keys));

            if (_settings.UseCacheOnly)
            {
                throw new InvalidOperationException(
                    $"Keyed translations cannot be fetched while {nameof(CalingaServiceSettings.UseCacheOnly)} is true; the keyed overload always requires HTTP. Path: {_settings.Organization}, {_settings.Team}, {_settings.Project}, {language}");
            }

            var keySet = new HashSet<string>(keys, StringComparer.Ordinal);

            if (keySet.Count == 0)
            {
                return new Dictionary<string, string>();
            }

            _logger.Info($"Fetching filtered translations for language {language} ({keySet.Count} key(s)) from consumer API");
            var subset = await _consumerHttpClient.GetTranslationsAsync(language, keySet).ConfigureAwait(false);

            if (_settings.IsDevMode)
            {
                var missingKeys = keySet.Where(k => !subset.ContainsKey(k)).ToList();
                if (missingKeys.Count > 0)
                {
                    throw new KeysNotFoundException(
                        missingKeys,
                        $"DevMode: {missingKeys.Count} of {keySet.Count} requested key(s) not found on server. Missing: {string.Join(", ", missingKeys)}. Path: {_settings.Organization}, {_settings.Team}, {_settings.Project}, {language}");
                }
                return subset.ToDictionary(k => k.Key, k => k.Key);
            }

            return subset;
        }

        private async Task<CacheResponse> TryReadCache(string language)
        {
            try
            {
                return await _cachingService.GetTranslations(language, _settings.IncludeDrafts).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _logger.Warn($"Error while fetching translations for language {language} from cache. Trying to fetch from consumer API. Error: {e.Message}");
                return CacheResponse.Empty;
            }
        }

        private async Task<IReadOnlyDictionary<string, string>?> TryGetFromApi(string language, CacheResponse cacheResponse)
        {
            if (_settings.UseCacheOnly)
            {
                // No HTTP allowed — surface whatever cache holds (fresh or stale).
                if (cacheResponse.FoundTranslationsInCache)
                {
                    var cached = cacheResponse.Result;
                    return _settings.IsDevMode ? cached.ToDictionary(k => k.Key, k => k.Key) : cached;
                }
                return null;
            }

            var ifNoneMatch = cacheResponse.FoundTranslationsInCache ? cacheResponse.ETag : null;

            try
            {
                var httpResponse = ifNoneMatch == null
                    ? await _consumerHttpClient.GetTranslationsAsync(language).ConfigureAwait(false)
                    : await _consumerHttpClient.GetTranslationsAsync(language, ifNoneMatch).ConfigureAwait(false);

                if (httpResponse.NotModified && cacheResponse.FoundTranslationsInCache)
                {
                    _logger.Info($"Translations for language {language} unchanged (304); reusing cached entry and refreshing expiration");
                    var etagToStore = cacheResponse.ETag ?? httpResponse.ETag;
                    await _cachingService.StoreTranslationsAsync(language, cacheResponse.Result, etagToStore).ConfigureAwait(false);
                    var reused = cacheResponse.Result;
                    return _settings.IsDevMode ? reused.ToDictionary(k => k.Key, k => k.Key) : reused;
                }

                var foundTranslations = httpResponse.Translations;
                if (foundTranslations != null && foundTranslations.Any())
                {
                    _logger.Info($"Translations for language {language} fetched from consumer API");
                    await _cachingService.StoreTranslationsAsync(language, foundTranslations, httpResponse.ETag).ConfigureAwait(false);
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
        /// <exception cref="TranslationsNotAvailableException">
        /// Thrown when the reference language cannot be determined — either because the language list is
        /// unavailable (inner exception is <see cref="LanguagesNotAvailableException"/>) or because the
        /// list contains no language flagged as reference. Reported as a translations failure because the
        /// reference language exists to drive translation fallback.
        /// </exception>
        public async Task<string> GetReferenceLanguage()
        {
            if (!string.IsNullOrWhiteSpace(_referenceLanguage))
                return _referenceLanguage!;

            Language[] languages;
            try
            {
                languages = (await FetchLanguagesAsync().ConfigureAwait(false)).ToArray();
            }
            catch (LanguagesNotAvailableException ex)
            {
                throw new TranslationsNotAvailableException(
                    $"Reference language could not be determined, path: {_settings.Organization}, {_settings.Team}, {_settings.Project}", ex);
            }

            if (languages.All(l => !l.IsReference))
            {
                throw new TranslationsNotAvailableException(
                    $"No reference language found, path: {_settings.Organization}, {_settings.Team}, {_settings.Project}");
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

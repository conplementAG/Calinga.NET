﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

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
            : this(new CachingService(new FileSystemService(settings)), new ConsumerHttpClient(settings), settings)
        {
        }

        public CalingaService(CalingaServiceSettings settings, HttpClient httpClient)
            : this(new CachingService(new FileSystemService(settings)), new ConsumerHttpClient(settings, httpClient), settings)
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

            try
            {
                var cachedTranslations = await _cachingService.GetTranslations(language, _settings.IncludeDrafts).ConfigureAwait(false);

                return !_settings.IsDevMode ? cachedTranslations : cachedTranslations.ToDictionary(k => k.Key, k => k.Key);
            }
            catch (TranslationsNotAvailableException)
            {
                var translations = await _consumerHttpClient.GetTranslationsAsync(language).ConfigureAwait(false);

                await _cachingService.StoreTranslationsAsync(language, translations).ConfigureAwait(false);

                return !_settings.IsDevMode ? translations : translations.ToDictionary(k => k.Key, k => k.Key);
            }
        }

        public async Task<IEnumerable<string>> GetLanguagesAsync()
        {
            return _languages ??= await _consumerHttpClient.GetLanguagesAsync().ConfigureAwait(false);
        }

        public void ClearCache()
        {
            _languages = null;
            _cachingService.ClearCache();
        }

        private void ValidateSettings(CalingaServiceSettings setting)
        {
            Guard.IsNotNull(setting, nameof(setting));
            Guard.IsNotNullOrWhiteSpace(setting.Project);
            Guard.IsNotNullOrWhiteSpace(setting.Organization);
            Guard.IsNotNullOrWhiteSpace(setting.CacheDirectory);
        }
    }
}

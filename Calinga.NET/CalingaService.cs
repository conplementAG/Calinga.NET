using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Calinga.Infrastructure;
using Calinga.Infrastructure.Exceptions;
using KeyNotFoundException = Calinga.Infrastructure.Exceptions.KeyNotFoundException;

namespace Calinga.NET
{
    public class CalingaService : ICalingaService
    {
        private readonly ICachingService _cachingService;

        public CalingaService(ICachingService cachingService, CalingaServiceSettings settings)
        {
            ValidateSettings(settings);
            _cachingService = cachingService;
        }

        public CalingaService(CalingaServiceSettings settings)
            : this(new CachingService(new ConsumerHttpClient(settings), new FileService(settings)), settings)
        {
            ValidateSettings(settings);
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

            var container = await _cachingService.GetTranslations(language).ConfigureAwait(false);
            if (container == null) throw new TranslationsNotAvailableException(FormattableString.Invariant($"Translations are not available"));

            var translation = container.FirstOrDefault(k => k.Key == key);
            if (translation.Equals(default(KeyValuePair<string, string>))) throw new KeyNotFoundException(FormattableString.Invariant($"Key {key} was not found!"));

            return translation.Value;
        }

        public Task<IEnumerable<string>> GetLanguagesAsync()
        {
            return _cachingService.GetLanguages();
        }

        public void ClearCache()
        {
            _cachingService.ClearCache();
        }

        private void ValidateSettings(CalingaServiceSettings setting)
        {
            Guard.IsNotNull(setting, nameof(setting));
            Guard.IsNotNullOrWhiteSpace(setting.Project);
            Guard.IsNotNullOrWhiteSpace(setting.Tenant);
            Guard.IsNotNullOrWhiteSpace(setting.CacheDirectory);
        }
    }
}

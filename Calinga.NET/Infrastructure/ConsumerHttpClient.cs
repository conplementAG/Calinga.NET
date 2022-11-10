using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Calinga.NET.Caching;
using Calinga.NET.Infrastructure.Exceptions;
using Newtonsoft.Json;
using static System.FormattableString;

namespace Calinga.NET.Infrastructure
{
    public class ConsumerHttpClient : IConsumerHttpClient
    {
        private const string API_TOKEN_HEADER_NAME = "api-token";
        private readonly HttpClient _httpClient;

        private readonly CalingaServiceSettings _settings;

        public ConsumerHttpClient(CalingaServiceSettings settings)
            : this(settings, new HttpClient())
        {
            _settings = settings;
        }

        public ConsumerHttpClient(CalingaServiceSettings settings, HttpClient httpClient)
        {
            _settings = settings;
            _httpClient = httpClient;

            EnsureApiTokenHeaderIsSet();
        }

        public async Task<IReadOnlyDictionary<string, string>> GetTranslationsAsync(string language)
        {
            var queryParameter = _settings.IncludeDrafts ? Invariant($"?includeDrafts={_settings.IncludeDrafts}") : string.Empty;
            var url = Invariant(
                $"{_settings.ConsumerApiBaseUrl}/{_settings.Organization}/{_settings.Team}/{_settings.Project}/languages/{language}{queryParameter}");

            var response = await _httpClient.GetAsync(url).ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new AuthorizationFailedException();
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                throw new TranslationsNotFoundException(
                    $"Translations not found for Organization = '{_settings.Organization}', Team = '{_settings.Team}', Project = '{_settings.Project}', Language = '{language}'");
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new TranslationsNotAvailableException("Failed to fetch translations");
            }

            var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            return CreateTranslationsDictionary(body);
        }

        public async Task<IEnumerable<Language>> GetLanguagesAsync()
        {
            try
            {
                var url = Invariant($"{_settings.ConsumerApiBaseUrl}/{_settings.Organization}/{_settings.Team}/{_settings.Project}/languages");
                var responseBody = await GetResponseBody(url).ConfigureAwait(false);

                return DeserializeLanguages(responseBody);
            }
            catch (HttpRequestException ex)
            {
                throw new LanguagesNotAvailableException("Failed to fetch languages", ex);
            }
        }

        public async Task<string> GetReferenceLanguageAsync()
        {
            var languages = await GetLanguagesAsync();

            if (languages.All(l => l.IsReference == false))
            {
                throw new LanguagesNotAvailableException("Reference language not found");
            }

            return languages.Single(l => l.IsReference).Name;
        }

        #region private static Methods

        private async Task<string> GetResponseBody(string url)
        {
            var response = await _httpClient.GetAsync(url).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        }

        private static Dictionary<string, string> CreateTranslationsDictionary(string json)
        {
            return JsonConvert.DeserializeObject<Dictionary<string, string>>(json)!;
        }

        private static IEnumerable<Language> DeserializeLanguages(string json)
        {
            return JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(json)!
                .Select(l =>
                {
                    var languageTag = l["tag"];
                    var isRefernece = l["isReference"];

                    return new Language
                    {
                        Name = string.IsNullOrEmpty(languageTag) ? l["name"] : $"{l["name"]}~{languageTag}",
                        IsReference = Convert.ToBoolean(isRefernece)
                    };
                });
        }

        private void EnsureApiTokenHeaderIsSet()
        {
            if (!_httpClient.DefaultRequestHeaders.Contains(API_TOKEN_HEADER_NAME))
            {
                _httpClient.DefaultRequestHeaders.Add(API_TOKEN_HEADER_NAME, _settings.ApiToken);
            }
        }

        #endregion private static Methods
    }
}
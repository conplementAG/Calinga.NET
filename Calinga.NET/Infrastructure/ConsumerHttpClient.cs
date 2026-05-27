using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Calinga.NET.Caching;
using Calinga.NET.Infrastructure.Exceptions;
using System.Text.Json;
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
        }

        public ConsumerHttpClient(CalingaServiceSettings settings, HttpClient httpClient)
        {
            _settings = settings;
            _httpClient = httpClient;

            EnsureApiTokenHeaderIsSet();
            AddClientVersionHeader();
        }
        
        private void AddClientVersionHeader()
        {
            const string clientVersionHeaderName = "Client-Version";
  
            var clientVersion = $"Calinga.Net/{typeof(ConsumerHttpClient).Assembly.GetName().Version}";
            
            if (!_httpClient.DefaultRequestHeaders.Contains(clientVersionHeaderName))
            {
                _httpClient.DefaultRequestHeaders.Add(clientVersionHeaderName, clientVersion);
            }
        }

        public Task<TranslationsHttpResponse> GetTranslationsAsync(string language) => GetTranslationsAsync(language, (string?)null);

        public async Task<TranslationsHttpResponse> GetTranslationsAsync(string language, string? ifNoneMatch)
        {
            var queryParameter = _settings.IncludeDrafts ? Invariant($"?includeDrafts={_settings.IncludeDrafts}") : string.Empty;
            var url = Invariant(
                $"{_settings.ConsumerApiBaseUrl}/{_settings.Organization}/{_settings.Team}/{_settings.Project}/languages/{language}{queryParameter}");

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            if (!string.IsNullOrEmpty(ifNoneMatch))
            {
                // TryAddWithoutValidation lets us echo the server's tag byte-for-byte, including
                // any weak prefix or quoting — the server's filter compares strings literally.
                request.Headers.TryAddWithoutValidation("If-None-Match", ifNoneMatch);
            }

            var response = await _httpClient.SendAsync(request).ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.NotModified)
            {
                return TranslationsHttpResponse.NotModifiedResponse(GetResponseETag(response) ?? ifNoneMatch); //We fall back to the etag we sent, when the server did not resend it
            }

            switch (response.StatusCode)
            {
                case HttpStatusCode.Unauthorized:
                    throw new AuthorizationFailedException();
                case HttpStatusCode.NotFound:
                    throw new TranslationsNotFoundException(
                        $"Translations not found for Organization = '{_settings.Organization}', Team = '{_settings.Team}', Project = '{_settings.Project}', Language = '{language}'");
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new TranslationsNotAvailableException("Failed to fetch translations");
            }

            var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            return new TranslationsHttpResponse(CreateTranslationsDictionary(body), GetResponseETag(response), notModified: false);
        }

        public async Task<IReadOnlyDictionary<string, string>> GetTranslationsAsync(string language, IEnumerable<string> keys)
        {
            var queryParameter = _settings.IncludeDrafts ? Invariant($"?includeDrafts={_settings.IncludeDrafts}") : string.Empty;
            var url = Invariant(
                $"{_settings.ConsumerApiBaseUrl}/{_settings.Organization}/{_settings.Team}/{_settings.Project}/languages/{language}{queryParameter}");

            var requestBody = JsonSerializer.Serialize(new { keyNames = keys });
            using var content = new StringContent(requestBody, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content).ConfigureAwait(false);

            switch (response.StatusCode)
            {
                case HttpStatusCode.Unauthorized:
                    throw new AuthorizationFailedException();
                case HttpStatusCode.NotFound:
                    throw new TranslationsNotFoundException(
                        $"Translations not found for Organization = '{_settings.Organization}', Team = '{_settings.Team}', Project = '{_settings.Project}', Language = '{language}'");
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new TranslationsNotAvailableException("Failed to fetch filtered translations");
            }

            var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            return CreateTranslationsDictionary(responseBody);
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

        #region private static Methods

        private async Task<string> GetResponseBody(string url)
        {
            var response = await _httpClient.GetAsync(url).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        }

        private static Dictionary<string, string> CreateTranslationsDictionary(string json)
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                ?? new Dictionary<string, string>();
        }

        private static string? GetResponseETag(HttpResponseMessage response)
        {
            // Use .Tag (just the quoted opaque value) and drop any weak prefix —
            // the server compares If-None-Match using the same .Tag string, so
            // weak vs strong never enters the equality check.
            return response.Headers.ETag?.Tag;
        }

        private static IEnumerable<Language> DeserializeLanguages(string json)
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.EnumerateArray().Select(l =>
            {
                var languageTag = l.GetProperty("tag").GetString();
                var languageName = l.GetProperty("name").GetString();
                return new Language
                {
                    Name = string.IsNullOrEmpty(languageTag) ? languageName! : $"{languageName}~{languageTag}",
                    IsReference = l.GetProperty("isReference").GetBoolean()
                };
            }).ToList();
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
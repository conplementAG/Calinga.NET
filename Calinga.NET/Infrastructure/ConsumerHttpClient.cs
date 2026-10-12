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
        private const string CLIENT_VERSION_HEADER_NAME = "Client-Version";

        private static readonly string ClientVersion =
            $"Calinga.Net/{typeof(ConsumerHttpClient).Assembly.GetName().Version}";

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
        }

        // Set auth/version headers per request rather than on DefaultRequestHeaders: the
        // HttpClient may be caller-owned and shared across hosts, and headers left on it
        // would ride along to every host it talks to.
        private HttpRequestMessage CreateRequest(HttpMethod method, string url)
        {
            var request = new HttpRequestMessage(method, url);
            request.Headers.TryAddWithoutValidation(API_TOKEN_HEADER_NAME, _settings.ApiToken);
            request.Headers.TryAddWithoutValidation(CLIENT_VERSION_HEADER_NAME, ClientVersion);
            return request;
        }

        public Task<TranslationsHttpResponse> GetTranslationsAsync(string language) => GetTranslationsAsync(language, (string?)null);

        public async Task<TranslationsHttpResponse> GetTranslationsAsync(string language, string? ifNoneMatch)
        {
            var queryParameter = _settings.IncludeDrafts ? Invariant($"?includeDrafts={_settings.IncludeDrafts}") : string.Empty;
            var url = Invariant(
                $"{_settings.ConsumerApiBaseUrl}/{_settings.Organization}/{_settings.Team}/{_settings.Project}/languages/{Uri.EscapeDataString(language)}{queryParameter}");

            using var request = CreateRequest(HttpMethod.Get, url);
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
                $"{_settings.ConsumerApiBaseUrl}/{_settings.Organization}/{_settings.Team}/{_settings.Project}/languages/{Uri.EscapeDataString(language)}{queryParameter}");

            var requestBody = JsonSerializer.Serialize(new { keyNames = keys });
            using var request = CreateRequest(HttpMethod.Post, url);
            request.Content = new StringContent(requestBody, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request).ConfigureAwait(false);

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
            string responseBody;
            try
            {
                var url = Invariant($"{_settings.ConsumerApiBaseUrl}/{_settings.Organization}/{_settings.Team}/{_settings.Project}/languages");
                responseBody = await GetResponseBody(url).ConfigureAwait(false);
            }
            catch (HttpRequestException ex)
            {
                throw new LanguagesNotAvailableException(
                    "Failed to fetch languages. The Consumer API could not be reached or returned an error status. "
                    + "Check your network connection, the ConsumerApiBaseUrl, and that your ApiToken is valid.", ex);
            }
            
            try
            {
                return DeserializeLanguages(responseBody);
            }
            catch (JsonException ex)
            {
                throw new LanguagesNotAvailableException(
                    "Failed to fetch languages. The server response was not valid JSON (e.g. an error page or proxy output). "
                    + "Check that ConsumerApiBaseUrl points at the Calinga Consumer API.", ex);
            }
            catch (KeyNotFoundException ex)
            {
                throw new LanguagesNotAvailableException(
                    "Failed to fetch languages. The response was missing an expected field (tag, name, or isReference). "
                    + "Check that Organization, Team, and Project are correct and that this client version matches the API.", ex);
            }
            catch (InvalidOperationException ex)
            {
                throw new LanguagesNotAvailableException(
                    "Failed to fetch languages. The response was not in the expected format (an array of language objects). "
                    + "Check that Organization, Team, and Project are correct and that this client version matches the API.", ex);
            }
        }

        #region private static Methods

        private async Task<string> GetResponseBody(string url)
        {
            using var request = CreateRequest(HttpMethod.Get, url);
            var response = await _httpClient.SendAsync(request).ConfigureAwait(false);
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

        #endregion private static Methods
    }
}
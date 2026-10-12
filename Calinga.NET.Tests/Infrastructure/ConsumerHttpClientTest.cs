using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Calinga.NET.Caching;
using Calinga.NET.Infrastructure;
using Calinga.NET.Infrastructure.Exceptions;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.Json;
using RichardSzalay.MockHttp;

namespace Calinga.NET.Tests.Infrastructure
{
    [TestClass]
    public class ConsumerHttpClientTest
    {
        private static CalingaServiceSettings _settings;

        [TestInitialize]
        public void Init()
        {
            _settings = CreateSettings();
        }

        [TestMethod]
        public async Task GetLanguages_ShouldReturnLanguageList_WhenResponseContainsValidJson()
        {
            // Arrange
            var mockMessageHandler = new MockHttpMessageHandler();
            mockMessageHandler
                .When($"https://api.calinga.io/v3/{_settings.Organization}/{_settings.Team}/{_settings.Project}/languages*")
                .Respond("application/json",
                    @"[ { ""name"": ""en"", ""tag"": """", ""isReference"": true }, { ""name"": ""en-GB"", ""tag"": """", ""isReference"": false }, { ""name"": ""en-GB"", ""tag"": ""Intranet"", ""isReference"": false } ]");

            var sut = new ConsumerHttpClient(_settings, new HttpClient(mockMessageHandler));

            // Act
            var languagesList = await sut.GetLanguagesAsync().ConfigureAwait(false);

            // Assert
            languagesList.Should().BeEquivalentTo(new List<Language>
            {
                new Language { Name = "en", IsReference = true },
                new Language { Name = "en-GB", IsReference = false },
                new Language { Name = "en-GB~Intranet", IsReference = false }
            });
        }

        [TestMethod]
        public async Task GetTranslationsAsync_WithKeyList_UsesPost_ToV3LanguagesUrl()
        {
            // Arrange
            var expectedUrl = $"{_settings.ConsumerApiBaseUrl}/{_settings.Organization}/{_settings.Team}/{_settings.Project}/languages/de";
            var mockMessageHandler = new MockHttpMessageHandler();
            mockMessageHandler
                .Expect(HttpMethod.Post, expectedUrl)
                .Respond("application/json", "{}");
            var sut = new ConsumerHttpClient(_settings, new HttpClient(mockMessageHandler));

            // Act
            await sut.GetTranslationsAsync("de", new[] { "k1" }).ConfigureAwait(false);

            // Assert
            mockMessageHandler.VerifyNoOutstandingExpectation();
        }

        [TestMethod]
        public async Task GetTranslationsAsync_WithKeyList_SendsJsonBody_WithKeyNames()
        {
            // Arrange
            var expectedUrl = $"{_settings.ConsumerApiBaseUrl}/{_settings.Organization}/{_settings.Team}/{_settings.Project}/languages/de";
            var mockMessageHandler = new MockHttpMessageHandler();
            mockMessageHandler
                .Expect(HttpMethod.Post, expectedUrl)
                .With(request =>
                {
                    if (request.Content == null) return false;
                    if (request.Content.Headers.ContentType?.MediaType != "application/json") return false;
                    var body = request.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    using var parsed = JsonDocument.Parse(body);
                    if (!parsed.RootElement.TryGetProperty("keyNames", out var keyNamesElement)) return false;
                    var keyNames = keyNamesElement.EnumerateArray().Select(e => e.GetString()).ToList();
                    return keyNames.SequenceEqual(new[] { "k1", "k2" });
                })
                .Respond("application/json", "{}");
            var sut = new ConsumerHttpClient(_settings, new HttpClient(mockMessageHandler));

            // Act
            await sut.GetTranslationsAsync("de", new[] { "k1", "k2" }).ConfigureAwait(false);

            // Assert
            mockMessageHandler.VerifyNoOutstandingExpectation();
        }

        [TestMethod]
        public async Task GetTranslationsAsync_WithKeyList_IncludeDrafts_AddsQueryString()
        {
            // Arrange
            var settings = CreateSettings();
            settings.IncludeDrafts = true;
            var expectedUrl =
                $"{settings.ConsumerApiBaseUrl}/{settings.Organization}/{settings.Team}/{settings.Project}/languages/de?includeDrafts=True";
            var mockMessageHandler = new MockHttpMessageHandler();
            mockMessageHandler
                .Expect(HttpMethod.Post, expectedUrl)
                .Respond("application/json", "{}");
            var sut = new ConsumerHttpClient(settings, new HttpClient(mockMessageHandler));

            // Act
            await sut.GetTranslationsAsync("de", new[] { "k1" }).ConfigureAwait(false);

            // Assert
            mockMessageHandler.VerifyNoOutstandingExpectation();
        }

        [TestMethod]
        public async Task GetTranslationsAsync_WithKeyList_On404_ThrowsTranslationsNotFound()
        {
            // Arrange
            var mockMessageHandler = new MockHttpMessageHandler();
            mockMessageHandler
                .When(HttpMethod.Post, "*")
                .Respond(HttpStatusCode.NotFound);
            var sut = new ConsumerHttpClient(_settings, new HttpClient(mockMessageHandler));

            // Act
            Func<Task> act = async () => await sut.GetTranslationsAsync("de", new[] { "k1" }).ConfigureAwait(false);

            // Assert
            await act.Should().ThrowAsync<TranslationsNotFoundException>();
        }

        [TestMethod]
        public async Task GetTranslationsAsync_WithKeyList_On401_ThrowsAuthorizationFailed()
        {
            // Arrange
            var mockMessageHandler = new MockHttpMessageHandler();
            mockMessageHandler
                .When(HttpMethod.Post, "*")
                .Respond(HttpStatusCode.Unauthorized);
            var sut = new ConsumerHttpClient(_settings, new HttpClient(mockMessageHandler));

            // Act
            Func<Task> act = async () => await sut.GetTranslationsAsync("de", new[] { "k1" }).ConfigureAwait(false);

            // Assert
            await act.Should().ThrowAsync<AuthorizationFailedException>();
        }

        [TestMethod]
        public async Task GetTranslationsAsync_WithKeyList_On500_ThrowsTranslationsNotAvailable()
        {
            // Arrange
            var mockMessageHandler = new MockHttpMessageHandler();
            mockMessageHandler
                .When(HttpMethod.Post, "*")
                .Respond(HttpStatusCode.InternalServerError);
            var sut = new ConsumerHttpClient(_settings, new HttpClient(mockMessageHandler));

            // Act
            Func<Task> act = async () => await sut.GetTranslationsAsync("de", new[] { "k1" }).ConfigureAwait(false);

            // Assert
            await act.Should().ThrowAsync<TranslationsNotAvailableException>();
        }

        [TestMethod]
        public async Task GetTranslationsAsync_WithKeyList_OnNullJsonBody_ReturnsEmptyDictionary()
        {
            // Arrange — the API responds 200 OK with the literal JSON value "null".
            // System.Text.Json deserialises that to a CLR null; the client must surface
            // an empty dictionary instead of letting null propagate to callers.
            var mockMessageHandler = new MockHttpMessageHandler();
            mockMessageHandler
                .When(HttpMethod.Post, "*")
                .Respond("application/json", "null");
            var sut = new ConsumerHttpClient(_settings, new HttpClient(mockMessageHandler));

            // Act
            var result = await sut.GetTranslationsAsync("de", new[] { "k1" }).ConfigureAwait(false);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [TestMethod]
        public async Task GetTranslationsAsync_OnNullJsonBody_ReturnsEmptyDictionary()
        {
            // Arrange — same null-body scenario for the existing GET overload, since both
            // paths share the CreateTranslationsDictionary helper.
            var mockMessageHandler = new MockHttpMessageHandler();
            mockMessageHandler
                .When(HttpMethod.Get, "*")
                .Respond("application/json", "null");
            var sut = new ConsumerHttpClient(_settings, new HttpClient(mockMessageHandler));

            // Act
            var result = await sut.GetTranslationsAsync("de").ConfigureAwait(false);

            // Assert
            result.Should().NotBeNull();
            result.Translations.Should().BeEmpty();
        }

        [TestMethod]
        public async Task GetTranslationsAsync_SendsIfNoneMatch_WhenCachedETagProvided()
        {
            // Arrange — when the caller supplies a cached ETag, ConsumerHttpClient must
            // place it in the outgoing If-None-Match header so the server can answer 304.
            const string cachedETag = "\"abc123\"";
            HttpRequestMessage? capturedRequest = null;
            var mockMessageHandler = new MockHttpMessageHandler();
            mockMessageHandler
                .When(HttpMethod.Get, "*")
                .With(req => { capturedRequest = req; return true; })
                .Respond("application/json", "{}");
            var sut = new ConsumerHttpClient(_settings, new HttpClient(mockMessageHandler));

            // Act
            await sut.GetTranslationsAsync("de", cachedETag).ConfigureAwait(false);

            // Assert
            capturedRequest.Should().NotBeNull();
            var ifNoneMatchTags = capturedRequest!.Headers.IfNoneMatch.ToList();
            ifNoneMatchTags.Should().HaveCount(1);
            ifNoneMatchTags[0].Tag.Should().Be(cachedETag);
        }

        [TestMethod]
        public async Task GetTranslationsAsync_On304_ReturnsNotModifiedFlag_WithEmptyTranslations()
        {
            // Arrange — a 304 response means the cached body is still fresh.
            // The client must surface NotModified=true rather than throwing
            // TranslationsNotAvailableException (the legacy behaviour).
            var mockMessageHandler = new MockHttpMessageHandler();
            mockMessageHandler
                .When(HttpMethod.Get, "*")
                .Respond(HttpStatusCode.NotModified);
            var sut = new ConsumerHttpClient(_settings, new HttpClient(mockMessageHandler));

            // Act
            var response = await sut.GetTranslationsAsync("de", "\"abc123\"").ConfigureAwait(false);

            // Assert
            response.Should().NotBeNull();
            response.NotModified.Should().BeTrue();
            response.Translations.Should().BeEmpty();
            response.ETag.Should().Be("\"abc123\"");
        }

        [TestMethod]
        public async Task GetTranslationsAsync_On200_ReturnsETagFromResponseHeader()
        {
            // Arrange — fresh 200 response carries an ETag header. The client must
            // surface that ETag so the caching layer can store it for next time.
            const string serverETag = "\"deadbeef\"";
            var mockMessageHandler = new MockHttpMessageHandler();
            mockMessageHandler
                .When(HttpMethod.Get, "*")
                .Respond(_ =>
                {
                    var resp = new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("{\"k1\":\"v1\"}", Encoding.UTF8, "application/json")
                    };
                    resp.Headers.ETag = new EntityTagHeaderValue(serverETag, true);
                    return resp;
                });
            var sut = new ConsumerHttpClient(_settings, new HttpClient(mockMessageHandler));

            // Act
            var response = await sut.GetTranslationsAsync("de").ConfigureAwait(false);

            // Assert
            response.NotModified.Should().BeFalse();
            response.ETag.Should().Be(serverETag);
            response.Translations.Should().BeEquivalentTo(new Dictionary<string, string> { { "k1", "v1" } });
        }

        [TestMethod]
        public async Task GetTranslationsAsync_On200WithoutETagHeader_ReturnsNullETag()
        {
            // Arrange — server is reachable but did not emit an ETag (e.g. cache
            // bypass, proxy stripping). The client must not invent one; downstream
            // logic relies on null to mean "no ETag known".
            var mockMessageHandler = new MockHttpMessageHandler();
            mockMessageHandler
                .When(HttpMethod.Get, "*")
                .Respond("application/json", "{\"k1\":\"v1\"}");
            var sut = new ConsumerHttpClient(_settings, new HttpClient(mockMessageHandler));

            // Act
            var response = await sut.GetTranslationsAsync("de").ConfigureAwait(false);

            // Assert
            response.NotModified.Should().BeFalse();
            response.ETag.Should().BeNull();
            response.Translations.Should().BeEquivalentTo(new Dictionary<string, string> { { "k1", "v1" } });
        }

        [TestMethod]
        public async Task GetTranslationsAsync_SendsApiToken_AsPerRequestHeader()
        {
            // Arrange — the secret api-token must travel on the request, so the server authenticates.
            HttpRequestMessage? capturedRequest = null;
            var mockMessageHandler = new MockHttpMessageHandler();
            mockMessageHandler
                .When(HttpMethod.Get, "*")
                .With(req => { capturedRequest = req; return true; })
                .Respond("application/json", "{}");
            var sut = new ConsumerHttpClient(_settings, new HttpClient(mockMessageHandler));

            // Act
            await sut.GetTranslationsAsync("de").ConfigureAwait(false);

            // Assert
            capturedRequest.Should().NotBeNull();
            capturedRequest!.Headers.GetValues("api-token").Should().ContainSingle()
                .Which.Should().Be(_settings.ApiToken);
            capturedRequest.Headers.GetValues("Client-Version").Should().ContainSingle()
                .Which.Should().StartWith("Calinga.Net/");
        }

        [TestMethod]
        public void Constructor_DoesNotBrandCallerHttpClient_WithApiToken()
        {
            // Arrange — a caller-supplied HttpClient (e.g. from IHttpClientFactory) may be shared
            // with other hosts. The library must never write the secret to DefaultRequestHeaders,
            // or the token would leak on every request that client makes to any host.
            var httpClient = new HttpClient(new MockHttpMessageHandler());

            // Act
            _ = new ConsumerHttpClient(_settings, httpClient);

            // Assert
            httpClient.DefaultRequestHeaders.Contains("api-token").Should().BeFalse();
            httpClient.DefaultRequestHeaders.Contains("Client-Version").Should().BeFalse();
        }

        [TestMethod]
        public async Task GetTranslationsAsync_EscapesLanguage_InRequestUrl()
        {
            // Arrange — a language value carrying URL metacharacters must be percent-encoded so it
            // cannot inject query parameters (e.g. forcing includeDrafts) or manipulate the path.
            HttpRequestMessage? capturedRequest = null;
            var mockMessageHandler = new MockHttpMessageHandler();
            mockMessageHandler
                .When(HttpMethod.Get, "*")
                .With(req => { capturedRequest = req; return true; })
                .Respond("application/json", "{}");
            var sut = new ConsumerHttpClient(_settings, new HttpClient(mockMessageHandler));

            // Act
            await sut.GetTranslationsAsync("en?includeDrafts=true").ConfigureAwait(false);

            // Assert — the injected query delimiter is encoded, keeping everything in the path segment.
            capturedRequest.Should().NotBeNull();
            capturedRequest!.RequestUri!.AbsoluteUri.Should().EndWith("/languages/en%3FincludeDrafts%3Dtrue");
            capturedRequest.RequestUri.Query.Should().BeEmpty();
        }

        [TestMethod]
        public async Task GetLanguagesAsync_OnMalformedJson_ThrowsLanguagesNotAvailable()
        {
            // Arrange — a malformed/hostile body must not escape as a raw JsonException;
            // GetLanguagesAsync must wrap it so the reference-language fallback still works.
            var mockMessageHandler = new MockHttpMessageHandler();
            mockMessageHandler
                .When(HttpMethod.Get, "*")
                .Respond("application/json", "{ not json");
            var sut = new ConsumerHttpClient(_settings, new HttpClient(mockMessageHandler));

            // Act
            Func<Task> act = async () => await sut.GetLanguagesAsync().ConfigureAwait(false);

            // Assert
            await act.Should().ThrowAsync<LanguagesNotAvailableException>();
        }

        [TestMethod]
        public async Task GetLanguagesAsync_OnMissingField_ThrowsLanguagesNotAvailable()
        {
            // Arrange — well-formed JSON array but the documents lack the expected fields, so the
            // rigid GetProperty access throws KeyNotFoundException. It must be wrapped as well.
            var mockMessageHandler = new MockHttpMessageHandler();
            mockMessageHandler
                .When(HttpMethod.Get, "*")
                .Respond("application/json", @"[ { ""unexpected"": ""value"" } ]");
            var sut = new ConsumerHttpClient(_settings, new HttpClient(mockMessageHandler));

            // Act
            Func<Task> act = async () => await sut.GetLanguagesAsync().ConfigureAwait(false);

            // Assert
            await act.Should().ThrowAsync<LanguagesNotAvailableException>();
        }

        private static CalingaServiceSettings CreateSettings(bool isDevMode = false)
        {
            return new CalingaServiceSettings
            {
                Organization = "SDK",

                Team = "Default Team",

                Project = "SampleSDK",

                ApiToken = "test-api-token",

                IncludeDrafts = false,

                IsDevMode = isDevMode,

                CacheDirectory = AppDomain.CurrentDomain.BaseDirectory
            };
        }
    }
}
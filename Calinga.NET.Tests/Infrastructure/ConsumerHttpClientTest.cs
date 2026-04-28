using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
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
            result.Should().BeEmpty();
        }

        private static CalingaServiceSettings CreateSettings(bool isDevMode = false)
        {
            return new CalingaServiceSettings
            {
                Organization = "SDK",

                Team = "Default Team",

                Project = "SampleSDK",

                ApiToken = "761dc484a4051e1290c7d48574e09978",

                IncludeDrafts = false,

                IsDevMode = isDevMode,

                CacheDirectory = AppDomain.CurrentDomain.BaseDirectory
            };
        }
    }
}
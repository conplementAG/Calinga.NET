using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Calinga.NET.Infrastructure;
using System.Net.Http;
using RichardSzalay.MockHttp;
using Calinga.NET.Infrastructure.Exceptions;

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
        public void GetReferenceLanguage_ShouldThrow_WhenResponseContainsNoReferenceLanguage()
        {
            // Arrange
            var mockMessageHandler = new MockHttpMessageHandler();
            mockMessageHandler
                .When($"https://api.calinga.io/v3/{_settings.Organization}/{_settings.Team}/{_settings.Project}/languages*")
                .Respond("application/json", "[ { 'name': 'en', 'tag': '', 'isReference': false }, { 'name': 'en-GB', 'tag': '', 'isReference': false }, { 'name': 'en-GB', 'tag': 'Intranet', 'isReference': false } ]");

            var client = new ConsumerHttpClient(_settings, new HttpClient(mockMessageHandler));

            // Act
            Func<Task<string>> referenceLanguageFunc = async () => await client.GetReferenceLanguageAsync().ConfigureAwait(false);

            // Assert
            referenceLanguageFunc.Should().ThrowAsync<LanguagesNotAvailableException>();
        }

        [TestMethod]
        public async Task GetReferenceLanguage_ShouldReturnReference_WhenResponseContainsValidJson()
        {
            // Arrange
            var mockMessageHandler = new MockHttpMessageHandler();
            mockMessageHandler
                .When($"https://api.calinga.io/v3/{_settings.Organization}/{_settings.Team}/{_settings.Project}/languages*")
                .Respond("application/json", "[ { 'name': 'en', 'tag': '', 'isReference': true }, { 'name': 'en-GB', 'tag': '', 'isReference': false }, { 'name': 'en-GB', 'tag': 'Intranet', 'isReference': false } ]");

            var client = new ConsumerHttpClient(_settings, new HttpClient(mockMessageHandler));

            // Act
            var referenceLanguage = await client.GetReferenceLanguageAsync().ConfigureAwait(false);

            // Assert
            referenceLanguage.Should().Be("en");
        }       

        private static CalingaServiceSettings CreateSettings(bool isDevMode = false) => new CalingaServiceSettings
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

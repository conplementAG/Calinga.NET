using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Calinga.NET.Caching;
using Calinga.NET.Infrastructure;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
                    "[ { 'name': 'en', 'tag': '', 'isReference': true }, { 'name': 'en-GB', 'tag': '', 'isReference': false }, { 'name': 'en-GB', 'tag': 'Intranet', 'isReference': false } ]");

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
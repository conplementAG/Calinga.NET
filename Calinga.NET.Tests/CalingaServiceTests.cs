﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Calinga.NET.Caching;
using Calinga.NET.Infrastructure;
using Calinga.NET.Infrastructure.Exceptions;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using static System.FormattableString;

namespace Calinga.NET.Tests
{
    [TestClass]
    public class CalingaServiceTests
    {
        private static CalingaServiceSettings _testCalingaServiceSettings;
        private Mock<ICachingService> _cachingService;
        private Mock<IConsumerHttpClient> _consumerHttpClient;

        [TestInitialize]
        public void Init()
        {
            _testCalingaServiceSettings = CreateSettings();
            _cachingService = new Mock<ICachingService>();
            _consumerHttpClient = new Mock<IConsumerHttpClient>();
            _cachingService.Setup(x => x.GetTranslations(TestData.Language_DE, _testCalingaServiceSettings.IncludeDrafts))
                .Returns(Task.FromResult(TestData.Cache_Translations_De));
            _cachingService.Setup(x => x.GetTranslations(TestData.Language_EN, _testCalingaServiceSettings.IncludeDrafts))
                .Returns(Task.FromResult(TestData.Cache_Translations_En));
            _cachingService.Setup(x => x.GetLanguages()).Returns(Task.FromResult(new CachedLanguageListResponse(new List<Language>(), false)));

            _consumerHttpClient.Setup(x => x.GetLanguagesAsync()).Returns(Task.FromResult(TestData.Languages));
        }

        [TestMethod]
        public void Constructor_ShouldThrow_WhenSettingsNull()
        {
            // Arrange
            Action constructor = () => new CalingaService(null!);

            // Assert
            constructor.Should().Throw<Exception>();
        }

        [TestMethod]
        public void Translate_ShouldThrow_WhenKeyEmpty()
        {
            // Arrange
            var service = new CalingaService(_testCalingaServiceSettings);

            // Act
            Func<Task> getTranslations = async () => await service.TranslateAsync("", TestData.Language_DE).ConfigureAwait(false);

            // Assert
            getTranslations.Should().ThrowAsync<ArgumentNullException>();
        }

        [TestMethod]
        public void Translate_ShouldThrow_WhenKeyLanguageEmpty()
        {
            // Arrange
            var service = new CalingaService(_testCalingaServiceSettings);

            // Act
            Func<Task> getTranslations = async () => await service.TranslateAsync(TestData.Key_1, "").ConfigureAwait(false);

            // Assert
            getTranslations.Should().ThrowAsync<ArgumentNullException>();
        }

        [TestMethod]
        public void CreateContext_ShouldThrow_WhenKeyLanguageEmpty()
        {
            // Arrange
            var service = new CalingaService(_testCalingaServiceSettings);

            // Act
            Action createContext = () => service.CreateContext("");

            // Assert
            createContext.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void ContextTranslate_ShouldThrow_WhenKeyLanguageEmpty()
        {
            // Arrange
            var service = new CalingaService(_testCalingaServiceSettings);
            var context = new LanguageContext(TestData.Language_DE, service);

            // Act
            Func<Task> translate = async () => await context.TranslateAsync("").ConfigureAwait(false);

            // Assert
            translate.Should().ThrowAsync<ArgumentNullException>();
        }

        [TestMethod]
        public async Task Translate_ShouldReturnTranslationFromTestData()
        {
            // Arrange
            var service = new CalingaService(_cachingService.Object, _consumerHttpClient.Object, _testCalingaServiceSettings);

            // Act
            var translation = await service.TranslateAsync(TestData.Key_1, TestData.Language_DE).ConfigureAwait(false);

            // Assert
            translation.Should().Be(Invariant($"{TestData.Language_DE} {TestData.Translation_Key_1}"));
            translation.Should().NotBe(Invariant($"{TestData.Language_EN} {TestData.Translation_Key_1}"));
        }

        [TestMethod]
        public async Task GetLanguages_ShouldReturnLanguagesFromCache()
        {
            // Arrange
            var service = new CalingaService(_cachingService.Object, _consumerHttpClient.Object, _testCalingaServiceSettings);
            _cachingService.Setup(x => x.GetLanguages()).Returns(Task.FromResult(new CachedLanguageListResponse(new List<Language>
            {
                new Language { Name = TestData.Language_FR, IsReference = true }
            }, true)));
            _consumerHttpClient.Setup(x => x.GetLanguagesAsync()).Returns(Task.FromResult<IEnumerable<Language>>(new List<Language>
            {
                new Language
                {
                    Name = TestData.Language_EN,
                    IsReference = true
                },
                new Language
                {
                    Name = TestData.Language_DE,
                    IsReference = true
                }
            }));

            // Act
            var languages = await service.GetLanguagesAsync().ConfigureAwait(false);

            // Assert
            languages.Should().BeEquivalentTo(new List<string> { TestData.Language_FR });
        }

        [TestMethod]
        public async Task GetLanguages_ShouldReturnLanguagesFromHttpClient_WhenNotFoundInCache()
        {
            // Arrange
            var service = new CalingaService(_cachingService.Object, _consumerHttpClient.Object, _testCalingaServiceSettings);
            _consumerHttpClient.Setup(x => x.GetLanguagesAsync()).Returns(Task.FromResult<IEnumerable<Language>>(new List<Language>
            {
                new Language
                {
                    Name = TestData.Language_EN,
                    IsReference = true
                },
                new Language
                {
                    Name = TestData.Language_DE,
                    IsReference = true
                }
            }));
            _cachingService.Setup(x => x.GetLanguages()).Returns(Task.FromResult(CachedLanguageListResponse.Empty));

            // Act
            var languages = await service.GetLanguagesAsync().ConfigureAwait(false);

            // Assert
            languages.Should().BeEquivalentTo(new List<string> { TestData.Language_EN, TestData.Language_DE });
        }


        [TestMethod]
        public async Task GetReferenceLanguage_ShouldReturnReferenceLanguageFromTestData()
        {
            // Arrange
            var service = new CalingaService(_cachingService.Object, _consumerHttpClient.Object, _testCalingaServiceSettings);

            // Act
            var referenceLanguage = await service.GetReferenceLanguage().ConfigureAwait(false);

            // Assert
            referenceLanguage.Should().Be(TestData.Language_EN);
        }

        [TestMethod]
        public async Task Translate_ShouldReturnKey_WhenKeyNotExists()
        {
            // Arrange
            var service = new CalingaService(_cachingService.Object, _consumerHttpClient.Object, _testCalingaServiceSettings);
            var key = Invariant($"{TestData.Key_1}_Test");

            // Act
            var result = await service.TranslateAsync(key, TestData.Language_DE).ConfigureAwait(false);

            // Assert
            result.Should().Be(key);
        }

        [TestMethod]
        public async Task Translate_ShouldReturnKey_WhenNoTranslations()
        {
            // Arrange
            var cachingService = new Mock<ICachingService>();
            cachingService.Setup(x => x.GetTranslations(TestData.Language_DE, _testCalingaServiceSettings.IncludeDrafts))
                .Throws<TranslationsNotAvailableException>();
            _consumerHttpClient.Setup(x => x.GetTranslationsAsync(TestData.Language_DE)).Throws<TranslationsNotAvailableException>();
            var service = new CalingaService(cachingService.Object, _consumerHttpClient.Object, _testCalingaServiceSettings);

            // Act
            var result = await service.TranslateAsync(TestData.Key_1, TestData.Language_DE).ConfigureAwait(false);

            // Assert
            result.Should().Be(TestData.Key_1);
        }

        [TestMethod]
        public async Task GetTranslations_ShouldReturnTranslationsFromTestData()
        {
            // Arrange
            var service = new CalingaService(_cachingService.Object, _consumerHttpClient.Object, _testCalingaServiceSettings);

            // Act
            var translations = await service.GetTranslationsAsync(TestData.Language_DE).ConfigureAwait(false);

            // Assert
            translations.Count.Should().Be(2);
            translations.Should().Contain(t => t.Key.Equals(TestData.Key_1));
            translations.Should().Contain(t => t.Value.Contains(TestData.Translation_Key_1));
        }

        [TestMethod]
        public async Task GetTranslations_ShouldNotFail_WhenCachingReturnsNull()
        {
            // Arrange
            _cachingService.Setup(x => x.GetTranslations(TestData.Language_DE, false)).ReturnsAsync(CacheResponse.Empty);
            _consumerHttpClient.Setup(x => x.GetTranslationsAsync(TestData.Language_DE)).ReturnsAsync(TestData.Translations_De);
            var service = new CalingaService(_cachingService.Object, _consumerHttpClient.Object, _testCalingaServiceSettings);

            // Act
            var translations = await service.GetTranslationsAsync(TestData.Language_DE).ConfigureAwait(false);

            // Assert
            translations.Any().Should().BeTrue();
        }

        [TestMethod]
        public async Task GetTranslations_ShouldReturnKeysFromTestData_WhenDevMode()
        {
            // Arrange
            var setting = CreateSettings(true);
            var service = new CalingaService(_cachingService.Object, _consumerHttpClient.Object, setting);

            // Act
            var translations = await service.GetTranslationsAsync(TestData.Language_DE).ConfigureAwait(false);

            // Assert
            translations.Count.Should().Be(2);
            translations.Should().Contain(t => t.Key.Equals(TestData.Key_1));
            translations.Should().Contain(t => t.Value.Equals(TestData.Key_1));
            translations.Should().Contain(t => t.Key.Equals(TestData.Key_2));
            translations.Should().Contain(t => t.Value.Equals(TestData.Key_2));
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
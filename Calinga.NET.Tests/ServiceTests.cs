using static System.FormattableString;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

using Calinga.NET.Infrastructure;
using Calinga.NET.Infrastructure.Exceptions;
using KeyNotFoundException = Calinga.NET.Infrastructure.Exceptions.KeyNotFoundException;

namespace Calinga.NET.Tests
{
    [TestClass]
    public class ServiceTests
    {
        private ICachingService _cachingService;
        private static CalingaServiceSettings _testCalingaServiceSettings;

       [TestInitialize]
        public void Init()
        {
            _testCalingaServiceSettings = CreateSettings();
            _cachingService = Substitute.For<ICachingService>();
            _cachingService.GetTranslations(TestData.Language_DE, _testCalingaServiceSettings.IncludeDrafts).Returns(TestData.Translations_De);
            _cachingService.GetTranslations(TestData.Language_EN, _testCalingaServiceSettings.IncludeDrafts).Returns(TestData.Translations_En);
            _cachingService.GetLanguages().Returns(TestData.Languages);
        }

        [TestMethod]
        public void Constructor_ShouldThrown_WhenSettingsNull()
        {
            // Arrange
            Action constructor = () => new CalingaService(null!);

            // Assert
            constructor.Should().Throw<Exception>();
        }

        [TestMethod]
        public void Translate_ShouldThrown_WhenKeyEmpty()
        {
            // Arrange
            var service = new CalingaService(_testCalingaServiceSettings);

            // Act
            Func<Task> getTranslations = async () => await service.TranslateAsync("", TestData.Language_DE).ConfigureAwait(false);

            // Assert
            getTranslations.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void Translate_ShouldThrown_WhenKeyLanguageEmpty()
        {
            // Arrange
            var service = new CalingaService(_testCalingaServiceSettings);

            // Act
            Func<Task> getTranslations = async () => await service.TranslateAsync(TestData.Key_1, "").ConfigureAwait(false);

            // Assert
            getTranslations.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void CreateContext_ShouldThrown_WhenKeyLanguageEmpty()
        {
            // Arrange
            var service = new CalingaService(_testCalingaServiceSettings);

            // Act
            Action createContext = () => service.CreateContext("");

            // Assert
            createContext.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void ContextTranslate_ShouldThrown_WhenKeyLanguageEmpty()
        {
            // Arrange
            var service = new CalingaService(_testCalingaServiceSettings);
            var context = new LanguageContext(TestData.Language_DE, service);

            // Act
            Func<Task> translate = async () => await context.TranslateAsync("").ConfigureAwait(false);

            // Assert
            translate.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public async Task Translate_ShouldReturnTranslationFromTestData()
        {
            // Arrange
            var service = new CalingaService(_cachingService, _testCalingaServiceSettings);

            // Act
            var translation = await service.TranslateAsync(TestData.Key_1, TestData.Language_DE).ConfigureAwait(false);

            // Assert
            translation.Should().Be(Invariant($"{TestData.Language_DE} {TestData.Translation_Key_1}"));
            translation.Should().NotBe(Invariant($"{TestData.Language_EN} {TestData.Translation_Key_1}"));
        }

        [TestMethod]
        public async Task GetLanguages_ShouldReturnLanguagesFromTestData()
        {
            // Arrange
            var service = new CalingaService(_cachingService, _testCalingaServiceSettings);

            // Act
            var languages = await service.GetLanguagesAsync().ConfigureAwait(false);

            // Assert
            languages.Count().Should().Be(2);
            languages.Should().Contain(TestData.Language_DE);
            languages.Should().Contain(TestData.Language_EN);
        }

        [TestMethod]
        public void Translate_ShouldThrowException_WhenKeyNotExists()
        {
            // Arrange
            var service = new CalingaService(_cachingService, _testCalingaServiceSettings);
            var key = Invariant($"{TestData.Key_1}_Test");

            // Act
            Func<Task> translate = async () => await service.TranslateAsync(key, TestData.Language_DE).ConfigureAwait(false);

            // Assert
            translate.Should().Throw<KeyNotFoundException>().WithMessage(FormattableString.Invariant($"Key {key} was not found!"));
        }

        [TestMethod]
        public void Translate_ShouldThrowException_WhenNoTranslations()
        {
            // Arrange
            var cachingService = Substitute.For<ICachingService>();
            cachingService.GetTranslations(TestData.Language_DE, _testCalingaServiceSettings.IncludeDrafts).Returns((IReadOnlyDictionary<string, string>)null!);
            var service = new CalingaService(cachingService, _testCalingaServiceSettings);

            // Act
            Func<Task> translate = async () => await service.TranslateAsync(TestData.Key_1, TestData.Language_DE).ConfigureAwait(false);

            // Assert
            translate.Should().Throw<TranslationsNotAvailableException>();
        }

        [TestMethod]
        public async Task GetTranslations_ShouldReturnTranslationsFromTestData()
        {
            // Arrange
            var service = new CalingaService(_cachingService, _testCalingaServiceSettings);

            // Act
            var translations = await service.GetTranslationsAsync(TestData.Language_DE).ConfigureAwait(false);

            // Assert
            translations.Count.Should().Be(2);
            translations.Should().Contain(t => t.Key.Equals(TestData.Key_1));
            translations.Should().Contain(t => t.Value.Contains(TestData.Translation_Key_1));
        }

        [TestMethod]
        public async Task GetTranslations_ShouldReturnKeysFromTestData_WhenDevMode()
        {
            // Arrange
            var setting = CreateSettings(true);
            var service = new CalingaService(_cachingService, setting);

            // Act
            var translations = await service.GetTranslationsAsync(TestData.Language_DE).ConfigureAwait(false);

            // Assert
            translations.Count.Should().Be(2);
            translations.Should().Contain(t => t.Key.Equals(TestData.Key_1));
            translations.Should().Contain(t => t.Value.Equals(TestData.Key_1));
            translations.Should().Contain(t => t.Key.Equals(TestData.Key_2));
            translations.Should().Contain(t => t.Value.Equals(TestData.Key_2));
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

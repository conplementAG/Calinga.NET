using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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
        private Mock<ILogger> _logger;

        [TestInitialize]
        public void Init()
        {
            _testCalingaServiceSettings = CreateSettings();
            _cachingService = new Mock<ICachingService>();
            _consumerHttpClient = new Mock<IConsumerHttpClient>();
            _logger = new Mock<ILogger>();
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
        public void Constructor_WithCachingServiceAndConsumerHttpClientAndSettingsAndLogger_ShouldCreateInstance()
        {
            // Arrange
            var cachingService = new Mock<ICachingService>();
            var consumerHttpClient = new Mock<IConsumerHttpClient>();
            var settings = CreateSettings();
            var logger = new Mock<ILogger>();

            // Act
            var service = new CalingaService(cachingService.Object, consumerHttpClient.Object, settings, logger.Object);

            // Assert
            service.Should().NotBeNull();
        }

        [TestMethod]
        public void Constructor_WithCachingServiceAndConsumerHttpClientAndSettings_ShouldCreateInstance()
        {
            // Arrange
            var cachingService = new Mock<ICachingService>();
            var consumerHttpClient = new Mock<IConsumerHttpClient>();
            var settings = CreateSettings();

            // Act
            var service = new CalingaService(cachingService.Object, consumerHttpClient.Object, settings);

            // Assert
            service.Should().NotBeNull();
        }

        [TestMethod]
        public void Constructor_WithSettings_ShouldCreateInstance()
        {
            // Arrange
            var settings = CreateSettings();

            // Act
            var service = new CalingaService(settings);

            // Assert
            service.Should().NotBeNull();
        }

        [TestMethod]
        public void Constructor_WithSettingsAndLogger_ShouldCreateInstance()
        {
            // Arrange
            var settings = CreateSettings();
            var logger = new Mock<ILogger>();

            // Act
            var service = new CalingaService(settings, logger.Object);

            // Assert
            service.Should().NotBeNull();
        }

        [TestMethod]
        public void Constructor_WithSettingsAndHttpClient_ShouldCreateInstance()
        {
            // Arrange
            var settings = CreateSettings();
            var httpClient = new HttpClient();

            // Act
            var service = new CalingaService(settings, httpClient);

            // Assert
            service.Should().NotBeNull();
        }

        [TestMethod]
        public void Constructor_WithSettingsAndHttpClientAndLogger_ShouldCreateInstance()
        {
            // Arrange
            var settings = CreateSettings();
            var httpClient = new HttpClient();
            var logger = new Mock<ILogger>();

            // Act
            var service = new CalingaService(settings, httpClient, logger.Object);

            // Assert
            service.Should().NotBeNull();
        }

        [TestMethod]
        public void Constructor_WithCachingServiceAndSettings_ShouldCreateInstance()
        {
            // Arrange
            var cachingService = new Mock<ICachingService>();
            var settings = CreateSettings();

            // Act
            var service = new CalingaService(cachingService.Object, settings);

            // Assert
            service.Should().NotBeNull();
        }

        [TestMethod]
        public void Constructor_WithCachingServiceAndSettingsAndLogger_ShouldCreateInstance()
        {
            // Arrange
            var cachingService = new Mock<ICachingService>();
            var settings = CreateSettings();
            var logger = new Mock<ILogger>();

            // Act
            var service = new CalingaService(cachingService.Object, settings, logger.Object);

            // Assert
            service.Should().NotBeNull();
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
            _cachingService.Setup(x => x.GetLanguages()).Returns(Task.FromResult(
                new CachedLanguageListResponse(new List<Language> { new Language { Name = TestData.Language_FR, IsReference = true } }, true)));
            _consumerHttpClient.Setup(x => x.GetLanguagesAsync()).Returns(Task.FromResult<IEnumerable<Language>>(new List<Language>
            {
                new Language { Name = TestData.Language_EN, IsReference = true }, new Language { Name = TestData.Language_DE, IsReference = true }
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
                new Language { Name = TestData.Language_EN, IsReference = true }, new Language { Name = TestData.Language_DE, IsReference = true }
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
        public async Task GetReferenceLanguage_ShouldThrow_WhenUseCacheOnlyIsTrueAndNoReferenceLanguageInCache()
        {
            // Arrange
            _testCalingaServiceSettings.UseCacheOnly = true;
            var service = new CalingaService(_cachingService.Object, _consumerHttpClient.Object, _testCalingaServiceSettings);
            _cachingService.Setup(x => x.GetLanguages()).ReturnsAsync(CachedLanguageListResponse.Empty);

            // Act
            Func<Task> getReferenceLanguage = async () => await service.GetReferenceLanguage().ConfigureAwait(false);

            // Assert
            await getReferenceLanguage.Should().ThrowAsync<LanguagesNotAvailableException>();
            _consumerHttpClient.Verify(x => x.GetLanguagesAsync(), Times.Never);
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

        [TestMethod]
        public async Task GetTranslationsAsync_ShouldFallbackToReferenceLanguage_WhenFallbackToReferenceLanguageIsTrue()
        {
            // Arrange
            var settings = CreateSettings();
            settings.FallbackToReferenceLanguage = true;
            var service = new CalingaService(_cachingService.Object, _consumerHttpClient.Object, settings, _logger.Object);
            var referenceLanguage = TestData.Language_EN;
            _cachingService.Setup(x => x.GetTranslations(TestData.Language_DE, settings.IncludeDrafts)).Throws<TranslationsNotAvailableException>();
            _cachingService.Setup(x => x.GetTranslations(referenceLanguage, settings.IncludeDrafts)).ReturnsAsync(TestData.Cache_Translations_En);
            _consumerHttpClient.Setup(x => x.GetTranslationsAsync(TestData.Language_DE)).Throws<TranslationsNotAvailableException>();
            _consumerHttpClient.Setup(x => x.GetTranslationsAsync(referenceLanguage)).ReturnsAsync(TestData.Translations_En);
            _cachingService.Setup(x => x.GetLanguages())
                .ReturnsAsync(new CachedLanguageListResponse(new List<Language> { new Language { Name = referenceLanguage, IsReference = true } },
                    true));

            // Act
            var translations = await service.GetTranslationsAsync(TestData.Language_DE).ConfigureAwait(false);

            // Assert
            translations.Should().BeEquivalentTo(TestData.Cache_Translations_En.Result);
        }

        [TestMethod]
        public async Task GetTranslationsAsync_ShouldNotFetchFromHttpClient_WhenUseCacheOnlyIsTrue()
        {
            // Arrange
            var settings = CreateSettings();
            settings.UseCacheOnly = true;
            var service = new CalingaService(_cachingService.Object, _consumerHttpClient.Object, settings, _logger.Object);
            _cachingService.Setup(x => x.GetTranslations(TestData.Language_DE, settings.IncludeDrafts)).ReturnsAsync(TestData.Cache_Translations_De);
            _consumerHttpClient.Setup(x => x.GetTranslationsAsync(TestData.Language_DE)).Throws<Exception>(); // Should not be called

            // Act
            var translations = await service.GetTranslationsAsync(TestData.Language_DE).ConfigureAwait(false);

            // Assert
            translations.Should().BeEquivalentTo(TestData.Cache_Translations_De.Result);
            _consumerHttpClient.Verify(x => x.GetTranslationsAsync(It.IsAny<string>()), Times.Never);
        }
        
        [TestMethod]
        public async Task GetReferenceLanguage_ShouldThrow_WhenNoReferenceLanguageFound()
        {
            // Arrange
            var service = new CalingaService(_cachingService.Object, _consumerHttpClient.Object, _testCalingaServiceSettings);
            _cachingService.Setup(x => x.GetLanguages()).ReturnsAsync(new CachedLanguageListResponse(new List<Language>(), false));
            _consumerHttpClient.Setup(x => x.GetLanguagesAsync()).ReturnsAsync(new List<Language>());

            // Act
            Func<Task> getReferenceLanguage = async () => await service.GetReferenceLanguage().ConfigureAwait(false);

            // Assert
            await getReferenceLanguage.Should().ThrowAsync<LanguagesNotAvailableException>();
        }
        
        [TestMethod]
        public async Task GetTranslationsAsync_ShouldThrow_WhenTranslationsNotAvailableAndFallbackToReferenceLanguageIsFalse()
        {
            // Arrange
            var settings = CreateSettings();
            settings.FallbackToReferenceLanguage = false;
            var service = new CalingaService(_cachingService.Object, _consumerHttpClient.Object, settings, _logger.Object);
            _cachingService.Setup(x => x.GetTranslations(TestData.Language_DE, settings.IncludeDrafts)).Throws<TranslationsNotAvailableException>();
            _consumerHttpClient.Setup(x => x.GetTranslationsAsync(TestData.Language_DE)).Throws<TranslationsNotAvailableException>();

            // Act
            Func<Task> getTranslations = async () => await service.GetTranslationsAsync(TestData.Language_DE).ConfigureAwait(false);

            // Assert
            await getTranslations.Should().ThrowAsync<TranslationsNotAvailableException>();
        }
        
        [TestMethod]
        public async Task GetTranslationsAsync_ShouldThrow_WhenFallbackToReferenceLanguageIsFalseOrReferenceLanguageIsSame()
        {
            // Arrange
            var settings = CreateSettings();
            settings.FallbackToReferenceLanguage = false;
            var service = new CalingaService(_cachingService.Object, _consumerHttpClient.Object, settings, _logger.Object);
            _cachingService.Setup(x => x.GetTranslations(TestData.Language_DE, settings.IncludeDrafts)).Throws<TranslationsNotAvailableException>();
            _consumerHttpClient.Setup(x => x.GetTranslationsAsync(TestData.Language_DE)).Throws<TranslationsNotAvailableException>();
            _cachingService.Setup(x => x.GetLanguages()).ReturnsAsync(new CachedLanguageListResponse(new List<Language> { new Language { Name = TestData.Language_DE, IsReference = true } }, true));

            // Act
            Func<Task> getTranslations = async () => await service.GetTranslationsAsync(TestData.Language_DE).ConfigureAwait(false);

            // Assert
            await getTranslations.Should().ThrowAsync<TranslationsNotAvailableException>();
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
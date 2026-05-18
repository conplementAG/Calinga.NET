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
            Func<Task> getTranslations = async () => await service.TranslateAsync("", TestData.Language_DE);

            // Assert
            getTranslations.Should().ThrowAsync<ArgumentNullException>();
        }

        [TestMethod]
        public void Translate_ShouldThrow_WhenKeyLanguageEmpty()
        {
            // Arrange
            var service = new CalingaService(_testCalingaServiceSettings);

            // Act
            Func<Task> getTranslations = async () => await service.TranslateAsync(TestData.Key_1, "");

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
            Func<Task> translate = async () => await context.TranslateAsync("");

            // Assert
            translate.Should().ThrowAsync<ArgumentNullException>();
        }

        [TestMethod]
        public async Task Translate_ShouldReturnTranslationFromTestData()
        {
            // Arrange
            var service = new CalingaService(_cachingService.Object, _consumerHttpClient.Object, _testCalingaServiceSettings);

            // Act
            var translation = await service.TranslateAsync(TestData.Key_1, TestData.Language_DE);

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
            var languages = await service.GetLanguagesAsync();

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
            var languages = await service.GetLanguagesAsync();

            // Assert
            languages.Should().BeEquivalentTo(new List<string> { TestData.Language_EN, TestData.Language_DE });
        }

        [TestMethod]
        public async Task GetReferenceLanguage_ShouldReturnReferenceLanguageFromTestData()
        {
            // Arrange
            var service = new CalingaService(_cachingService.Object, _consumerHttpClient.Object, _testCalingaServiceSettings);

            // Act
            var referenceLanguage = await service.GetReferenceLanguage();

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
            Func<Task> getReferenceLanguage = async () => await service.GetReferenceLanguage();

            // Assert
            var assertion = await getReferenceLanguage.Should().ThrowAsync<TranslationsNotAvailableException>();
            assertion.WithInnerException<LanguagesNotAvailableException>();
            _consumerHttpClient.Verify(x => x.GetLanguagesAsync(), Times.Never);
        }

        [TestMethod]
        public async Task Translate_ShouldReturnKey_WhenKeyNotExists()
        {
            // Arrange
            var service = new CalingaService(_cachingService.Object, _consumerHttpClient.Object, _testCalingaServiceSettings);
            var key = Invariant($"{TestData.Key_1}_Test");

            // Act
            var result = await service.TranslateAsync(key, TestData.Language_DE);

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
            var result = await service.TranslateAsync(TestData.Key_1, TestData.Language_DE);

            // Assert
            result.Should().Be(TestData.Key_1);
        }

        [TestMethod]
        public async Task GetTranslations_ShouldReturnTranslationsFromTestData()
        {
            // Arrange
            var service = new CalingaService(_cachingService.Object, _consumerHttpClient.Object, _testCalingaServiceSettings);

            // Act
            var translations = await service.GetTranslationsAsync(TestData.Language_DE);

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
            _consumerHttpClient.Setup(x => x.GetTranslationsAsync(TestData.Language_DE)).ReturnsAsync(TestData.Http_Translations_De);
            var service = new CalingaService(_cachingService.Object, _consumerHttpClient.Object, _testCalingaServiceSettings);

            // Act
            var translations = await service.GetTranslationsAsync(TestData.Language_DE);

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
            var translations = await service.GetTranslationsAsync(TestData.Language_DE);

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
            _consumerHttpClient.Setup(x => x.GetTranslationsAsync(referenceLanguage)).ReturnsAsync(new TranslationsHttpResponse(TestData.Translations_En, null, false));
            _cachingService.Setup(x => x.GetLanguages())
                .ReturnsAsync(new CachedLanguageListResponse(new List<Language> { new Language { Name = referenceLanguage, IsReference = true } },
                    true));

            // Act
            var translations = await service.GetTranslationsAsync(TestData.Language_DE);

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
            var translations = await service.GetTranslationsAsync(TestData.Language_DE);

            // Assert
            translations.Should().BeEquivalentTo(TestData.Cache_Translations_De.Result);
            _consumerHttpClient.Verify(x => x.GetTranslationsAsync(It.IsAny<string>()), Times.Never);
        }
        
        [TestMethod]
        public async Task GetReferenceLanguage_ShouldThrow_WhenNoReferenceLanguageFound()
        {
            // Arrange — non-empty language list with no reference flag. FetchLanguagesAsync succeeds,
            // so there is no inner LanguagesNotAvailableException — only the outer translations failure.
            var service = new CalingaService(_cachingService.Object, _consumerHttpClient.Object, _testCalingaServiceSettings);
            _cachingService.Setup(x => x.GetLanguages()).ReturnsAsync(new CachedLanguageListResponse(new List<Language>(), false));
            _consumerHttpClient.Setup(x => x.GetLanguagesAsync()).ReturnsAsync(new List<Language>
            {
                new Language { Name = TestData.Language_DE, IsReference = false }
            });

            // Act
            Func<Task> getReferenceLanguage = async () => await service.GetReferenceLanguage();

            // Assert
            await getReferenceLanguage.Should().ThrowAsync<TranslationsNotAvailableException>();
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
            Func<Task> getTranslations = async () => await service.GetTranslationsAsync(TestData.Language_DE);

            // Assert
            await getTranslations.Should().ThrowAsync<TranslationsNotAvailableException>();
        }
        
        [TestMethod]
        public async Task GetTranslationsAsync_ShouldThrowTranslationsNotAvailable_WhenLanguageListUnavailableDuringFallback()
        {
            // Arrange — UseCacheOnly with empty caches forces FetchLanguagesAsync to throw
            // LanguagesNotAvailableException. Callers of GetTranslationsAsync expect a
            // TranslationsNotAvailableException, with the language failure as the inner cause.
            var settings = CreateSettings();
            settings.UseCacheOnly = true;
            settings.FallbackToReferenceLanguage = true;
            var service = new CalingaService(_cachingService.Object, _consumerHttpClient.Object, settings, _logger.Object);
            _cachingService.Setup(x => x.GetTranslations(TestData.Language_DE, settings.IncludeDrafts))
                .ReturnsAsync(new CacheResponse(TestData.EmptyTranslations, false));
            _cachingService.Setup(x => x.GetLanguages()).ReturnsAsync(CachedLanguageListResponse.Empty);

            // Act
            Func<Task> getTranslations = async () => await service.GetTranslationsAsync(TestData.Language_DE);

            // Assert
            var assertion = await getTranslations.Should().ThrowAsync<TranslationsNotAvailableException>();
            assertion.WithInnerException<LanguagesNotAvailableException>();
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
            Func<Task> getTranslations = async () => await service.GetTranslationsAsync(TestData.Language_DE);

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

        [TestMethod]
        public async Task GetTranslationsAsync_InvalidateCache_ReturnsBodyFromHttp_NotFromCache()
        {
            // Arrange — invalidateCache=true skips the fast-path return so the
            // body comes from HTTP. The cache is still read (to surface a
            // possible ETag), but its body is not returned directly.
            // Default Init() makes the cache return Translations_De with no ETag.
            var service = new CalingaService(_cachingService.Object, _consumerHttpClient.Object, _testCalingaServiceSettings);
            _consumerHttpClient.Setup(x => x.GetTranslationsAsync(TestData.Language_DE))
                .ReturnsAsync(new TranslationsHttpResponse(TestData.Translations_En, etag: null, notModified: false));

            // Act
            var translations = await service.GetTranslationsAsync(TestData.Language_DE, invalidateCache: true);

            // Assert
            translations.Should().BeEquivalentTo(TestData.Translations_En);
            _consumerHttpClient.Verify(x => x.GetTranslationsAsync(TestData.Language_DE), Times.Once);
        }

        [TestMethod]
        public async Task GetTranslationsAsync_ShouldThrow_WhenInvalidateCacheIsTrue_AndHttpClientFails()
        {
            // Arrange
            var service = new CalingaService(_cachingService.Object, _consumerHttpClient.Object, _testCalingaServiceSettings);
            _consumerHttpClient.Setup(x => x.GetTranslationsAsync(TestData.Language_DE)).ThrowsAsync(new TranslationsNotAvailableException("fail"));
            // Act
            Func<Task> act = async () => await service.GetTranslationsAsync(TestData.Language_DE, invalidateCache: true);
            // Assert
            await act.Should().ThrowAsync<TranslationsNotAvailableException>();
            _consumerHttpClient.Verify(x => x.GetTranslationsAsync(TestData.Language_DE), Times.Once);
        }

 
        [TestMethod]
        public async Task GetTranslationsAsync_ShouldThrowArgumentException_WhenInvalidateCacheIsTrue_AndUseCacheOnlyIsTrue()
        {
            // Arrange
            var settings = CreateSettings();
            settings.UseCacheOnly = true;
            var service = new CalingaService(_cachingService.Object, _consumerHttpClient.Object, settings, _logger.Object);
            // Act
            Func<Task> act = async () => await service.GetTranslationsAsync(TestData.Language_DE, invalidateCache: true);
            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("Cannot invalidate cache when global Setting 'UseCacheOnly' is set to true.*");
            _consumerHttpClient.Verify(x => x.GetTranslationsAsync(It.IsAny<string>()), Times.Never);
            _cachingService.Verify(x => x.GetTranslations(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
        }

        #region Keyed GetTranslationsAsync

        [TestMethod]
        public async Task GetTranslationsAsync_WithKeyList_WarmCache_StillPostsToServer()
        {
            // Arrange — warm cache must not rescue the call; keyed requests always go to the server.
            var serverSubset = new Dictionary<string, string> { { TestData.Key_1, "server value for key 1" } };
            _consumerHttpClient
                .Setup(x => x.GetTranslationsAsync(TestData.Language_DE, It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(serverSubset);
            var service = new CalingaService(_cachingService.Object, _consumerHttpClient.Object, _testCalingaServiceSettings, _logger.Object);

            // Act
            var result = await service.GetTranslationsAsync(TestData.Language_DE, new[] { TestData.Key_1 });

            // Assert
            result.Should().BeEquivalentTo(serverSubset);
            _consumerHttpClient.Verify(x => x.GetTranslationsAsync(TestData.Language_DE, It.IsAny<IEnumerable<string>>()), Times.Once);
            _cachingService.Verify(x => x.GetTranslations(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
            _cachingService.Verify(x => x.StoreTranslationsAsync(It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, string>>()), Times.Never);
        }

        [TestMethod]
        public async Task GetTranslationsAsync_WithKeyList_MissingFromServer_Omitted()
        {
            // Arrange — server omits Key_2 from its response; client surfaces that as a missing entry.
            var serverSubset = new Dictionary<string, string>
            {
                { TestData.Key_1, "from server 1" }
                // Key_2 intentionally omitted.
            };
            _consumerHttpClient
                .Setup(x => x.GetTranslationsAsync(TestData.Language_DE, It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(serverSubset);
            var service = new CalingaService(_cachingService.Object, _consumerHttpClient.Object, _testCalingaServiceSettings, _logger.Object);

            // Act
            var result = await service.GetTranslationsAsync(TestData.Language_DE, new[] { TestData.Key_1, TestData.Key_2 });

            // Assert
            result.Should().HaveCount(1);
            result.Should().ContainKey(TestData.Key_1);
            result.Should().NotContainKey(TestData.Key_2);
        }

        [TestMethod]
        public async Task GetTranslationsAsync_WithKeyList_ColdCache_CallsKeyedHttp_NotStored()
        {
            // Arrange
            var serverSubset = new Dictionary<string, string> { { TestData.Key_1, "server value for key 1" } };
            _consumerHttpClient
                .Setup(x => x.GetTranslationsAsync(TestData.Language_DE, It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(serverSubset);
            var service = new CalingaService(_cachingService.Object, _consumerHttpClient.Object, _testCalingaServiceSettings, _logger.Object);

            // Act
            var result = await service.GetTranslationsAsync(TestData.Language_DE, new[] { TestData.Key_1 });

            // Assert
            result.Should().BeEquivalentTo(serverSubset);
            _consumerHttpClient.Verify(x => x.GetTranslationsAsync(TestData.Language_DE, It.IsAny<IEnumerable<string>>()), Times.Once);
            _consumerHttpClient.Verify(x => x.GetTranslationsAsync(It.IsAny<string>()), Times.Never);
            _cachingService.Verify(x => x.GetTranslations(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
            _cachingService.Verify(x => x.StoreTranslationsAsync(It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, string>>()), Times.Never);
        }

        [TestMethod]
        public async Task GetTranslationsAsync_WithKeyList_ColdCache_ReturnsServerSubset()
        {
            // Arrange
            var serverSubset = new Dictionary<string, string>
            {
                { TestData.Key_1, "from server 1" }
            };
            _consumerHttpClient
                .Setup(x => x.GetTranslationsAsync(TestData.Language_DE, It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(serverSubset);
            var service = new CalingaService(_cachingService.Object, _consumerHttpClient.Object, _testCalingaServiceSettings, _logger.Object);

            // Act
            var result = await service.GetTranslationsAsync(TestData.Language_DE, new[] { TestData.Key_1, TestData.Key_2 });

            // Assert
            result.Should().HaveCount(1);
            result.Should().ContainKey(TestData.Key_1);
            result.Should().NotContainKey(TestData.Key_2);
        }

        [TestMethod]
        public async Task GetTranslationsAsync_WithKeyList_UseCacheOnly_ColdCache_ThrowsInvalidOperation()
        {
            // Arrange
            var settings = CreateSettings();
            settings.UseCacheOnly = true;
            var service = new CalingaService(_cachingService.Object, _consumerHttpClient.Object, settings, _logger.Object);

            // Act
            Func<Task> act = async () => await service.GetTranslationsAsync(TestData.Language_DE, new[] { TestData.Key_1 });

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>();
            _consumerHttpClient.Verify(x => x.GetTranslationsAsync(It.IsAny<string>(), It.IsAny<IEnumerable<string>>()), Times.Never);
            _consumerHttpClient.Verify(x => x.GetTranslationsAsync(It.IsAny<string>()), Times.Never);
            _consumerHttpClient.Verify(x => x.GetLanguagesAsync(), Times.Never);
            _cachingService.Verify(x => x.GetTranslations(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
        }

        [TestMethod]
        public async Task GetTranslationsAsync_WithKeyList_UseCacheOnly_WarmCache_ThrowsInvalidOperation()
        {
            // Arrange — populate the cache, then call the keyed overload under UseCacheOnly.
            // The UseCacheOnly check must reject the call before any cache lookup happens,
            // even if the cache holds the requested key. A future change that consulted the
            // cache before checking UseCacheOnly would silently make this call succeed —
            // the warm cache is what makes that regression observable.
            var settings = CreateSettings();
            settings.UseCacheOnly = true;
            _cachingService
                .Setup(x => x.GetTranslations(TestData.Language_DE, It.IsAny<bool>()))
                .ReturnsAsync(TestData.Cache_Translations_De);
            var service = new CalingaService(_cachingService.Object, _consumerHttpClient.Object, settings, _logger.Object);

            // Act
            Func<Task> act = async () => await service.GetTranslationsAsync(TestData.Language_DE, new[] { TestData.Key_1 });

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>();
            _consumerHttpClient.Verify(x => x.GetTranslationsAsync(It.IsAny<string>(), It.IsAny<IEnumerable<string>>()), Times.Never);
            _consumerHttpClient.Verify(x => x.GetTranslationsAsync(It.IsAny<string>()), Times.Never);
            _cachingService.Verify(x => x.GetTranslations(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
        }

        [TestMethod]
        public async Task GetTranslationsAsync_WithKeyList_NullKeys_ThrowsArgumentNullException()
        {
            // Arrange
            var service = new CalingaService(_cachingService.Object, _consumerHttpClient.Object, _testCalingaServiceSettings, _logger.Object);

            // Act
            Func<Task> act = async () => await service.GetTranslationsAsync(TestData.Language_DE, (IEnumerable<string>)null!);

            // Assert
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [TestMethod]
        public async Task GetTranslationsAsync_WithKeyList_EmptyKeys_ReturnsEmpty_NoHttp_NoCacheAccess()
        {
            // Arrange
            var service = new CalingaService(_cachingService.Object, _consumerHttpClient.Object, _testCalingaServiceSettings, _logger.Object);

            // Act
            var result = await service.GetTranslationsAsync(TestData.Language_DE, Array.Empty<string>());

            // Assert
            result.Should().BeEmpty();
            _consumerHttpClient.Verify(x => x.GetTranslationsAsync(It.IsAny<string>(), It.IsAny<IEnumerable<string>>()), Times.Never);
            _consumerHttpClient.Verify(x => x.GetTranslationsAsync(It.IsAny<string>()), Times.Never);
            _cachingService.Verify(x => x.GetTranslations(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
            _cachingService.Verify(x => x.StoreTranslationsAsync(It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, string>>()), Times.Never);
        }

        [TestMethod]
        public async Task GetTranslationsAsync_WithKeyList_EmptyKeys_UseCacheOnly_ThrowsInvalidOperation()
        {
            // Arrange — UseCacheOnly is incompatible with the keyed overload regardless of whether the key
            // collection is empty. The UseCacheOnly check runs before the empty-keys short-circuit.
            var settings = CreateSettings();
            settings.UseCacheOnly = true;
            var service = new CalingaService(_cachingService.Object, _consumerHttpClient.Object, settings, _logger.Object);

            // Act
            Func<Task> act = async () => await service.GetTranslationsAsync(TestData.Language_DE, Array.Empty<string>());

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>();
            _consumerHttpClient.Verify(x => x.GetTranslationsAsync(It.IsAny<string>(), It.IsAny<IEnumerable<string>>()), Times.Never);
            _consumerHttpClient.Verify(x => x.GetTranslationsAsync(It.IsAny<string>()), Times.Never);
            _cachingService.Verify(x => x.GetTranslations(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
        }

        [TestMethod]
        public async Task GetTranslationsAsync_WithKeyList_IsDevMode_EchoesKeys()
        {
            // Arrange — DevMode echoes the keys returned by the server as their own values.
            var settings = CreateSettings(isDevMode: true);
            var serverSubset = new Dictionary<string, string> { { TestData.Key_1, "some translation" } };
            _consumerHttpClient
                .Setup(x => x.GetTranslationsAsync(TestData.Language_DE, It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(serverSubset);
            var service = new CalingaService(_cachingService.Object, _consumerHttpClient.Object, settings, _logger.Object);

            // Act
            var result = await service.GetTranslationsAsync(TestData.Language_DE, new[] { TestData.Key_1 });

            // Assert
            result.Should().ContainKey(TestData.Key_1).WhoseValue.Should().Be(TestData.Key_1);
        }

        [TestMethod]
        public async Task GetTranslationsAsync_WithKeyList_IsDevMode_AllKeysPresent_EchoesAll()
        {
            // Arrange — every requested key is present in the server response.
            // DevMode echoes each key as its own value; no exception.
            var settings = CreateSettings(isDevMode: true);
            var serverSubset = new Dictionary<string, string>
            {
                { TestData.Key_1, "translation 1" },
                { TestData.Key_2, "translation 2" }
            };
            _consumerHttpClient
                .Setup(x => x.GetTranslationsAsync(TestData.Language_DE, It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(serverSubset);
            var service = new CalingaService(_cachingService.Object, _consumerHttpClient.Object, settings, _logger.Object);

            // Act
            var result = await service.GetTranslationsAsync(TestData.Language_DE, new[] { TestData.Key_1, TestData.Key_2 });

            // Assert
            result.Should().HaveCount(2);
            result.Should().ContainKey(TestData.Key_1).WhoseValue.Should().Be(TestData.Key_1);
            result.Should().ContainKey(TestData.Key_2).WhoseValue.Should().Be(TestData.Key_2);
        }

        [TestMethod]
        public async Task GetTranslationsAsync_WithKeyList_IsDevMode_ServerOmitsKey_ThrowsKeysNotFound()
        {
            // Arrange — caller asks for two keys; server returns only one.
            // DevMode must throw KeysNotFoundException listing the missing key(s)
            // so typos and unknown keys surface at integration time rather than as
            // silent omissions at runtime.
            var settings = CreateSettings(isDevMode: true);
            var serverSubset = new Dictionary<string, string>
            {
                { TestData.Key_1, "some translation" }
                // Key_2 intentionally omitted by the server.
            };
            _consumerHttpClient
                .Setup(x => x.GetTranslationsAsync(TestData.Language_DE, It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(serverSubset);
            var service = new CalingaService(_cachingService.Object, _consumerHttpClient.Object, settings, _logger.Object);

            // Act
            Func<Task> act = async () => await service.GetTranslationsAsync(TestData.Language_DE, new[] { TestData.Key_1, TestData.Key_2 });

            // Assert
            var assertion = await act.Should().ThrowAsync<KeysNotFoundException>();
            assertion.Which.MissingKeys.Should().ContainSingle().Which.Should().Be(TestData.Key_2);
            assertion.Which.Message.Should().Contain(TestData.Key_2);
        }

        [TestMethod]
        public async Task GetTranslationsAsync_WithKeyList_NotDevMode_ServerOmitsKey_StillSilentlyOmits()
        {
            // Arrange — outside DevMode, the existing "silently omit" contract stays.
            // The validation behaviour is DevMode-only.
            var settings = CreateSettings(isDevMode: false);
            var serverSubset = new Dictionary<string, string>
            {
                { TestData.Key_1, "some translation" }
                // Key_2 intentionally omitted by the server.
            };
            _consumerHttpClient
                .Setup(x => x.GetTranslationsAsync(TestData.Language_DE, It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(serverSubset);
            var service = new CalingaService(_cachingService.Object, _consumerHttpClient.Object, settings, _logger.Object);

            // Act
            var result = await service.GetTranslationsAsync(TestData.Language_DE, new[] { TestData.Key_1, TestData.Key_2 });

            // Assert
            result.Should().HaveCount(1);
            result.Should().ContainKey(TestData.Key_1);
            result.Should().NotContainKey(TestData.Key_2);
        }

        #endregion Keyed GetTranslationsAsync

        #region ETag revalidation

        [TestMethod]
        public async Task GetTranslationsAsync_StaleCache_ServerReturns304_ReturnsCachedAndRefreshesExpiration()
        {
            // Arrange — cache hit but expired; the entry's stored ETag drives
            // a conditional GET. Server confirms "still fresh" with 304, so we
            // reuse the cached translations and call StoreTranslationsAsync to
            // refresh the expiration timer.
            const string cachedETag = "\"abc\"";
            var staleCacheResponse = new CacheResponse(TestData.Translations_De, foundTranslationsInCache: true, etag: cachedETag, isStale: true);
            _cachingService.Setup(x => x.GetTranslations(TestData.Language_DE, _testCalingaServiceSettings.IncludeDrafts))
                .ReturnsAsync(staleCacheResponse);
            _consumerHttpClient.Setup(x => x.GetTranslationsAsync(TestData.Language_DE, cachedETag))
                .ReturnsAsync(TranslationsHttpResponse.NotModifiedResponse(cachedETag));
            var service = new CalingaService(_cachingService.Object, _consumerHttpClient.Object, _testCalingaServiceSettings);

            // Act
            var result = await service.GetTranslationsAsync(TestData.Language_DE);

            // Assert
            result.Should().BeEquivalentTo(TestData.Translations_De);
            _consumerHttpClient.Verify(x => x.GetTranslationsAsync(TestData.Language_DE, cachedETag), Times.Once);
            _cachingService.Verify(x => x.StoreTranslationsAsync(TestData.Language_DE, TestData.Translations_De, cachedETag), Times.Once);
        }

        [TestMethod]
        public async Task GetTranslationsAsync_StaleCache_ServerReturns200_StoresNewTranslationsWithNewETag()
        {
            // Arrange — cache stale with old ETag; server returns fresh body and
            // a new ETag. We must use the new data and persist the new ETag,
            // not the old one (otherwise the next revalidation sends a stale tag).
            const string oldETag = "\"old\"";
            const string newETag = "\"new\"";
            var staleCacheResponse = new CacheResponse(TestData.Translations_De, foundTranslationsInCache: true, etag: oldETag, isStale: true);
            _cachingService.Setup(x => x.GetTranslations(TestData.Language_DE, _testCalingaServiceSettings.IncludeDrafts))
                .ReturnsAsync(staleCacheResponse);
            _consumerHttpClient.Setup(x => x.GetTranslationsAsync(TestData.Language_DE, oldETag))
                .ReturnsAsync(new TranslationsHttpResponse(TestData.Translations_En, newETag, notModified: false));
            var service = new CalingaService(_cachingService.Object, _consumerHttpClient.Object, _testCalingaServiceSettings);

            // Act
            var result = await service.GetTranslationsAsync(TestData.Language_DE);

            // Assert
            result.Should().BeEquivalentTo(TestData.Translations_En);
            _cachingService.Verify(x => x.StoreTranslationsAsync(TestData.Language_DE, TestData.Translations_En, newETag), Times.Once);
        }

        [TestMethod]
        public async Task GetTranslationsAsync_CacheMiss_DoesNotSendIfNoneMatch()
        {
            // Arrange — empty cache: no ETag to send. Must hit the no-revalidation
            // overload, not the 2-arg one with a null/empty ETag (the server-side
            // contract is "include If-None-Match only if you have one").
            _cachingService.Setup(x => x.GetTranslations(TestData.Language_DE, _testCalingaServiceSettings.IncludeDrafts))
                .ReturnsAsync(CacheResponse.Empty);
            _consumerHttpClient.Setup(x => x.GetTranslationsAsync(TestData.Language_DE))
                .ReturnsAsync(TestData.Http_Translations_De);
            var service = new CalingaService(_cachingService.Object, _consumerHttpClient.Object, _testCalingaServiceSettings);

            // Act
            await service.GetTranslationsAsync(TestData.Language_DE);

            // Assert
            _consumerHttpClient.Verify(x => x.GetTranslationsAsync(TestData.Language_DE), Times.Once);
            _consumerHttpClient.Verify(x => x.GetTranslationsAsync(It.IsAny<string>(), It.IsAny<string?>()), Times.Never);
        }

        [TestMethod]
        public async Task GetTranslationsAsync_FreshCache_DoesNotHitHttp()
        {
            // Arrange — fresh cache hit must short-circuit; no HTTP at all.
            _cachingService.Setup(x => x.GetTranslations(TestData.Language_DE, _testCalingaServiceSettings.IncludeDrafts))
                .ReturnsAsync(new CacheResponse(TestData.Translations_De, foundTranslationsInCache: true, etag: "\"abc\"", isStale: false));
            var service = new CalingaService(_cachingService.Object, _consumerHttpClient.Object, _testCalingaServiceSettings);

            // Act
            await service.GetTranslationsAsync(TestData.Language_DE);

            // Assert
            _consumerHttpClient.Verify(x => x.GetTranslationsAsync(It.IsAny<string>()), Times.Never);
            _consumerHttpClient.Verify(x => x.GetTranslationsAsync(It.IsAny<string>(), It.IsAny<string?>()), Times.Never);
        }

        [TestMethod]
        public async Task GetTranslationsAsync_InvalidateCache_StillSendsIfNoneMatch_WhenCachedETagAvailable()
        {
            // Arrange — invalidateCache means "refresh the body", not "skip
            // revalidation". The cached ETag is still useful: if the server
            // returns 304, we know our cache body is the current truth and
            // can serve it without a full download.
            const string cachedETag = "\"abc\"";
            _cachingService.Setup(x => x.GetTranslations(TestData.Language_DE, _testCalingaServiceSettings.IncludeDrafts))
                .ReturnsAsync(new CacheResponse(TestData.Translations_De, foundTranslationsInCache: true, etag: cachedETag, isStale: false));
            _consumerHttpClient.Setup(x => x.GetTranslationsAsync(TestData.Language_DE, cachedETag))
                .ReturnsAsync(new TranslationsHttpResponse(TestData.Translations_En, "\"new\"", notModified: false));
            var service = new CalingaService(_cachingService.Object, _consumerHttpClient.Object, _testCalingaServiceSettings);

            // Act
            await service.GetTranslationsAsync(TestData.Language_DE, invalidateCache: true);

            // Assert
            _consumerHttpClient.Verify(x => x.GetTranslationsAsync(TestData.Language_DE, cachedETag), Times.Once);
            _consumerHttpClient.Verify(x => x.GetTranslationsAsync(TestData.Language_DE), Times.Never);
        }

        [TestMethod]
        public async Task GetTranslationsAsync_UseCacheOnly_StaleData_ReturnsStaleWithoutHttp()
        {
            // Arrange — UseCacheOnly forbids HTTP. If the cache holds anything
            // (fresh or stale), surface it. Skipping it would force callers
            // offline to lose all translations after the first expiry.
            var settings = CreateSettings();
            settings.UseCacheOnly = true;
            var staleCacheResponse = new CacheResponse(TestData.Translations_De, foundTranslationsInCache: true, etag: "\"abc\"", isStale: true);
            _cachingService.Setup(x => x.GetTranslations(TestData.Language_DE, settings.IncludeDrafts))
                .ReturnsAsync(staleCacheResponse);
            var service = new CalingaService(_cachingService.Object, _consumerHttpClient.Object, settings);

            // Act
            var result = await service.GetTranslationsAsync(TestData.Language_DE);

            // Assert
            result.Should().BeEquivalentTo(TestData.Translations_De);
            _consumerHttpClient.Verify(x => x.GetTranslationsAsync(It.IsAny<string>()), Times.Never);
            _consumerHttpClient.Verify(x => x.GetTranslationsAsync(It.IsAny<string>(), It.IsAny<string?>()), Times.Never);
        }

        [TestMethod]
        public async Task GetTranslationsAsync_CacheReportsMiss_DoesNotCrash_AndSkipsIfNoneMatch()
        {
            // Arrange — simulates the on-disk orphan-ETag scenario at the
            // service level: even if a sidecar exists, FileCachingService
            // returns a clean miss when the .json is gone. CalingaService
            // must accept that, fall through to a plain GET (no
            // If-None-Match), and return the server's response without
            // throwing.
            _cachingService.Setup(x => x.GetTranslations(TestData.Language_DE, _testCalingaServiceSettings.IncludeDrafts))
                .ReturnsAsync(CacheResponse.Empty);
            _consumerHttpClient.Setup(x => x.GetTranslationsAsync(TestData.Language_DE))
                .ReturnsAsync(TestData.Http_Translations_De);
            var service = new CalingaService(_cachingService.Object, _consumerHttpClient.Object, _testCalingaServiceSettings);

            // Act
            Func<Task> act = async () => await service.GetTranslationsAsync(TestData.Language_DE);

            // Assert
            await act.Should().NotThrowAsync();
            _consumerHttpClient.Verify(x => x.GetTranslationsAsync(TestData.Language_DE), Times.Once);
            _consumerHttpClient.Verify(x => x.GetTranslationsAsync(It.IsAny<string>(), It.IsAny<string?>()), Times.Never);
        }

        #endregion ETag revalidation
    }
}

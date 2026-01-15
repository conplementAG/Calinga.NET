using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Calinga.NET.Caching;
using Calinga.NET.Infrastructure;
using Moq;

namespace Calinga.NET.Tests
{
    [TestClass]
    public class CascadedCachingServiceTests
    {
        private Mock<ICachingService> _firstLevelCachingService;
        private Mock<ICachingService> _secondLevelCachingService;
        private ICachingService _sut;

        [TestInitialize]
        public void Init()
        {
            _firstLevelCachingService = new Mock<ICachingService>();
            _secondLevelCachingService = new Mock<ICachingService>();
            _sut = new CascadedCachingService(_firstLevelCachingService.Object, _secondLevelCachingService.Object);
        }

        [TestMethod]
        public async Task GetTranslations_ShouldGetTranslationsFromFirstCacheLevel_WhenAvailable()
        {
            // Arrange
            _firstLevelCachingService.Setup(x => x.GetTranslations(TestData.Language_DE, false)).Returns(Task.FromResult(TestData.Cache_Translations_De));
            _secondLevelCachingService.Setup(x => x.GetTranslations(TestData.Language_DE, false)).Returns(Task.FromResult(TestData.Cache_Translations_En));

            // Act
            var actual = await _sut.GetTranslations(TestData.Language_DE, false);

            // Assert
            _firstLevelCachingService.Verify(x => x.GetTranslations(TestData.Language_DE, false), Times.Once);
            _secondLevelCachingService.Verify(x => x.GetTranslations(TestData.Language_DE, false), Times.Never);
            actual.Result.Should().BeEquivalentTo(TestData.Translations_De);
        }

        [TestMethod]
        public async Task GetTranslations_ShouldGetTranslationsFromSecondCacheLevel_WhenNotAvailableInFirst()
        {
            // Arrange
            _firstLevelCachingService.Setup(x => x.GetTranslations(TestData.Language_DE, false)).Returns(Task.FromResult(CacheResponse.Empty));
            _secondLevelCachingService.Setup(x => x.GetTranslations(TestData.Language_DE, false)).Returns(Task.FromResult(TestData.Cache_Translations_De));

            // Act
            var actual = await _sut.GetTranslations(TestData.Language_DE, false);

            // Assert
            _firstLevelCachingService.Verify(x => x.GetTranslations(TestData.Language_DE, false), Times.Once);
            _secondLevelCachingService.Verify(x => x.GetTranslations(TestData.Language_DE, false), Times.Once);
            actual.Result.Should().BeEquivalentTo(TestData.Translations_De);
        }

        [TestMethod]
        public async Task GetTranslations_ShouldNotFail_WhenNoCacheHitInAnyLevel()
        {
            // Arrange
            _firstLevelCachingService.Setup(x => x.GetTranslations(TestData.Language_DE, false)).Returns(Task.FromResult(CacheResponse.Empty));
            _secondLevelCachingService.Setup(x => x.GetTranslations(TestData.Language_DE, false)).Returns(Task.FromResult(CacheResponse.Empty));

            // Act
            var actual = await _sut.GetTranslations(TestData.Language_DE, false);

            // Assert
            _firstLevelCachingService.Verify(x => x.GetTranslations(TestData.Language_DE, false), Times.Once);
            _secondLevelCachingService.Verify(x => x.GetTranslations(TestData.Language_DE, false), Times.Once);
            actual.Result.Should().BeEquivalentTo(TestData.EmptyTranslations);
        }

        [TestMethod]
        public async Task StoreTranslation_ShouldAddTranslationToAllLevels()
        {
            // Arrange
            _firstLevelCachingService.Setup(x => x.StoreTranslationsAsync(It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, string>>()));
            _secondLevelCachingService.Setup(x => x.StoreTranslationsAsync(It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, string>>()));

            // Act
            await _sut.StoreTranslationsAsync(TestData.Language_DE, TestData.Translations_De);

            // Assert
            _firstLevelCachingService.Verify(x => x.StoreTranslationsAsync(It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, string>>()), Times.Once);
            _secondLevelCachingService.Verify(x => x.StoreTranslationsAsync(It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, string>>()), Times.Once);
        }

        [TestMethod]
        public async Task ClearCache_ShouldClearAllLevels()
        {
            // Arrange
            _firstLevelCachingService.Setup(x => x.ClearCache());
            _secondLevelCachingService.Setup(x => x.ClearCache());

            // Act
            await _sut.ClearCache();

            // Assert
            _firstLevelCachingService.Verify(x => x.ClearCache(), Times.Once);
            _secondLevelCachingService.Verify(x => x.ClearCache(), Times.Once);
        }

        [TestMethod]
        public async Task GetTranslations_ShouldBackfillFirstLevel_WhenSecondLevelHasData()
        {
            // Arrange
            _firstLevelCachingService.Setup(x => x.GetTranslations(TestData.Language_DE, false))
                .ReturnsAsync(CacheResponse.Empty);
            _secondLevelCachingService.Setup(x => x.GetTranslations(TestData.Language_DE, false))
                .ReturnsAsync(TestData.Cache_Translations_De);
            _firstLevelCachingService.Setup(x => x.StoreTranslationsAsync(TestData.Language_DE, It.IsAny<IReadOnlyDictionary<string, string>>()))
                .Returns(Task.CompletedTask);

            // Act
            var actual = await _sut.GetTranslations(TestData.Language_DE, false);

            // Assert
            actual.Result.Should().BeEquivalentTo(TestData.Translations_De);
            _firstLevelCachingService.Verify(
                x => x.StoreTranslationsAsync(TestData.Language_DE, It.Is<IReadOnlyDictionary<string, string>>(
                    dict => dict.Count == TestData.Translations_De.Count)),
                Times.Once,
                "First level cache should be backfilled when second level has data");
        }

        [TestMethod]
        public async Task GetTranslations_ShouldNotBackfill_WhenFirstLevelHasData()
        {
            // Arrange
            _firstLevelCachingService.Setup(x => x.GetTranslations(TestData.Language_DE, false))
                .ReturnsAsync(TestData.Cache_Translations_De);

            // Act
            var actual = await _sut.GetTranslations(TestData.Language_DE, false);

            // Assert
            actual.Result.Should().BeEquivalentTo(TestData.Translations_De);
            _firstLevelCachingService.Verify(
                x => x.StoreTranslationsAsync(It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, string>>()),
                Times.Never,
                "No backfill should occur when first level already has data");
            _secondLevelCachingService.Verify(
                x => x.GetTranslations(It.IsAny<string>(), It.IsAny<bool>()),
                Times.Never);
        }

        [TestMethod]
        public async Task GetLanguages_ShouldReturnFromFirstLevel_WhenAvailable()
        {
            // Arrange
            var cachedLanguages = new CachedLanguageListResponse(new List<Language>(TestData.Languages), true);
            _firstLevelCachingService.Setup(x => x.GetLanguages())
                .ReturnsAsync(cachedLanguages);

            // Act
            var actual = await _sut.GetLanguages();

            // Assert
            actual.Result.Should().BeEquivalentTo(TestData.Languages);
            _firstLevelCachingService.Verify(x => x.GetLanguages(), Times.Once);
            _secondLevelCachingService.Verify(x => x.GetLanguages(), Times.Never);
        }

        [TestMethod]
        public async Task GetLanguages_ShouldReturnFromSecondLevel_WhenFirstLevelEmpty()
        {
            // Arrange
            var cachedLanguages = new CachedLanguageListResponse(new List<Language>(TestData.Languages), true);
            _firstLevelCachingService.Setup(x => x.GetLanguages())
                .ReturnsAsync(CachedLanguageListResponse.Empty);
            _secondLevelCachingService.Setup(x => x.GetLanguages())
                .ReturnsAsync(cachedLanguages);

            // Act
            var actual = await _sut.GetLanguages();

            // Assert
            actual.Result.Should().BeEquivalentTo(TestData.Languages);
            _firstLevelCachingService.Verify(x => x.GetLanguages(), Times.Once);
            _secondLevelCachingService.Verify(x => x.GetLanguages(), Times.Once);
        }

        [TestMethod]
        public async Task GetLanguages_ShouldBackfillFirstLevel_WhenSecondLevelHasData()
        {
            // Arrange
            var cachedLanguages = new CachedLanguageListResponse(new List<Language>(TestData.Languages), true);
            _firstLevelCachingService.Setup(x => x.GetLanguages())
                .ReturnsAsync(CachedLanguageListResponse.Empty);
            _secondLevelCachingService.Setup(x => x.GetLanguages())
                .ReturnsAsync(cachedLanguages);
            _firstLevelCachingService.Setup(x => x.StoreLanguagesAsync(It.IsAny<IEnumerable<Language>>()))
                .Returns(Task.CompletedTask);

            // Act
            var actual = await _sut.GetLanguages();

            // Assert
            actual.Result.Should().BeEquivalentTo(TestData.Languages);
            _firstLevelCachingService.Verify(
                x => x.StoreLanguagesAsync(It.Is<IEnumerable<Language>>(
                    langs => langs.Count() == TestData.Languages.Count())),
                Times.Once,
                "First level cache should be backfilled when second level has data");
        }

        [TestMethod]
        public async Task GetLanguages_ShouldNotBackfill_WhenFirstLevelHasData()
        {
            // Arrange
            var cachedLanguages = new CachedLanguageListResponse(new List<Language>(TestData.Languages), true);
            _firstLevelCachingService.Setup(x => x.GetLanguages())
                .ReturnsAsync(cachedLanguages);

            // Act
            var actual = await _sut.GetLanguages();

            // Assert
            actual.Result.Should().BeEquivalentTo(TestData.Languages);
            _firstLevelCachingService.Verify(
                x => x.StoreLanguagesAsync(It.IsAny<IEnumerable<Language>>()),
                Times.Never,
                "No backfill should occur when first level already has data");
            _secondLevelCachingService.Verify(x => x.GetLanguages(), Times.Never);
        }

        [TestMethod]
        public async Task GetTranslations_Integration_ShouldUseInMemoryAfterBackfill()
        {
            // Arrange - Use real InMemoryCachingService with mock file cache
            var settings = new CalingaServiceSettings
            {
                MemoryCacheExpirationIntervalInSeconds = 60,
                CacheDirectory = "test"
            };
            var inMemoryCache = new InMemoryCachingService(new DateTimeService(), settings);
            var fileCacheMock = new Mock<ICachingService>();

            // File cache returns data on first call, then empty (to verify in-memory is used)
            fileCacheMock.SetupSequence(x => x.GetTranslations(TestData.Language_DE, false))
                .ReturnsAsync(TestData.Cache_Translations_De)
                .ReturnsAsync(CacheResponse.Empty);
            fileCacheMock.Setup(x => x.StoreTranslationsAsync(It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, string>>()))
                .Returns(Task.CompletedTask);

            var cascadedCache = new CascadedCachingService(inMemoryCache, fileCacheMock.Object);

            // Act - First call: in-memory miss, file hit, backfill
            var firstResult = await cascadedCache.GetTranslations(TestData.Language_DE, false);

            // Act - Second call: should come from in-memory (file mock returns empty now)
            var secondResult = await cascadedCache.GetTranslations(TestData.Language_DE, false);

            // Assert
            firstResult.Result.Should().BeEquivalentTo(TestData.Translations_De);
            secondResult.Result.Should().BeEquivalentTo(TestData.Translations_De,
                "Second call should return data from in-memory cache after backfill");

            // Verify file cache was only called once (second call should use in-memory)
            fileCacheMock.Verify(x => x.GetTranslations(TestData.Language_DE, false), Times.Once,
                "File cache should only be called once - second request should use in-memory");
        }

        [TestMethod]
        public async Task GetLanguages_Integration_ShouldUseInMemoryAfterBackfill()
        {
            // Arrange - Use real InMemoryCachingService with mock file cache
            var settings = new CalingaServiceSettings
            {
                MemoryCacheExpirationIntervalInSeconds = 60,
                CacheDirectory = "test"
            };
            var inMemoryCache = new InMemoryCachingService(new DateTimeService(), settings);
            var fileCacheMock = new Mock<ICachingService>();

            var cachedLanguages = new CachedLanguageListResponse(new List<Language>(TestData.Languages), true);

            // File cache returns data on first call, then empty
            fileCacheMock.SetupSequence(x => x.GetLanguages())
                .ReturnsAsync(cachedLanguages)
                .ReturnsAsync(CachedLanguageListResponse.Empty);
            fileCacheMock.Setup(x => x.StoreLanguagesAsync(It.IsAny<IEnumerable<Language>>()))
                .Returns(Task.CompletedTask);

            var cascadedCache = new CascadedCachingService(inMemoryCache, fileCacheMock.Object);

            // Act - First call: in-memory miss, file hit, backfill
            var firstResult = await cascadedCache.GetLanguages();

            // Act - Second call: should come from in-memory
            var secondResult = await cascadedCache.GetLanguages();

            // Assert
            firstResult.Result.Should().BeEquivalentTo(TestData.Languages);
            secondResult.Result.Should().BeEquivalentTo(TestData.Languages,
                "Second call should return data from in-memory cache after backfill");

            // Verify file cache was only called once
            fileCacheMock.Verify(x => x.GetLanguages(), Times.Once,
                "File cache should only be called once - second request should use in-memory");
        }

        [TestMethod]
        public async Task GetTranslations_Integration_ShouldUseInMemoryAfterBackfill_WhenInitialExpirationPassed()
        {
            // Arrange - Simulate scenario where service was constructed and initial expiration has passed
            var timeServiceMock = new Mock<IDateTimeService>();
            var baseTime = System.DateTime.Now;

            // Construction time - expiration will be set to baseTime + 60
            timeServiceMock.Setup(x => x.GetCurrentDateTime()).Returns(baseTime);

            var settings = new CalingaServiceSettings
            {
                MemoryCacheExpirationIntervalInSeconds = 60,
                CacheDirectory = "test"
            };
            var inMemoryCache = new InMemoryCachingService(timeServiceMock.Object, settings);
            var fileCacheMock = new Mock<ICachingService>();

            // File cache returns data
            fileCacheMock.Setup(x => x.GetTranslations(TestData.Language_DE, false))
                .ReturnsAsync(TestData.Cache_Translations_De);
            fileCacheMock.Setup(x => x.StoreTranslationsAsync(It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, string>>()))
                .Returns(Task.CompletedTask);

            var cascadedCache = new CascadedCachingService(inMemoryCache, fileCacheMock.Object);

            // Now simulate time passing - initial expiration has passed (baseTime + 120 > baseTime + 60)
            timeServiceMock.Setup(x => x.GetCurrentDateTime()).Returns(baseTime.AddSeconds(120));

            // Act - First call: in-memory is "expired" (initial expiration passed), file hit, backfill
            var firstResult = await cascadedCache.GetTranslations(TestData.Language_DE, false);

            // Act - Second call: should come from in-memory (expiration was reset during backfill)
            var secondResult = await cascadedCache.GetTranslations(TestData.Language_DE, false);

            // Assert
            firstResult.Result.Should().BeEquivalentTo(TestData.Translations_De);
            secondResult.Result.Should().BeEquivalentTo(TestData.Translations_De,
                "Second call should return data from in-memory cache after backfill, even when initial expiration had passed");

            // File cache should only be called once - second request should use in-memory
            fileCacheMock.Verify(x => x.GetTranslations(TestData.Language_DE, false), Times.Once,
                "File cache should only be called once - second request should use in-memory");
        }
    }
}

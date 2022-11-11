using System;
using System.Threading.Tasks;
using Calinga.NET.Caching;
using Calinga.NET.Infrastructure;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Calinga.NET.Tests
{
    [TestClass]
    public class InMemoryCachingServiceTests
    {
        private ICachingService _sut;

        [TestInitialize]
        public void Init()
        {
            _sut = new InMemoryCachingService(new DateTimeService(), GetSettings());
        }

        [TestMethod]
        public async Task GetTranslations_ShouldGetTranslations_WhenCached()
        {
            // Arrange
            await _sut.StoreTranslationsAsync(TestData.Language_DE, TestData.Translations_De);

            // Act
            var actual = await _sut.GetTranslations(TestData.Language_DE, false);

            // Assert
            actual.Result.Should().BeEquivalentTo(TestData.Translations_De);
        }

        [TestMethod]
        public async Task GetTranslations_ShouldClearCache_WhenCacheExpired()
        {
            // Arrange
            var timeService = new Mock<IDateTimeService>();
            var sut = new InMemoryCachingService(timeService.Object, GetSettings(2));
            await sut.StoreTranslationsAsync(TestData.Language_DE, TestData.Translations_De);

            // Act
            timeService.Setup(t => t.GetCurrentDateTime()).Returns(DateTime.Now.AddSeconds(7));
            var actual = await sut.GetTranslations(TestData.Language_DE, false);

            // Assert
            actual.Result.Should().BeEquivalentTo(TestData.EmptyTranslations);
        }

        [TestMethod]
        public async Task GetLanguages_ShouldReturnCachedLanguages_WhenCacheNotExpired()
        {
            // Arrange
            var timeService = new Mock<IDateTimeService>();
            var sut = new InMemoryCachingService(timeService.Object, GetSettings(2));
            await sut.StoreLanguagesAsync(TestData.Languages);

            // Act
            var actual = await sut.GetLanguages();

            // Assert
            actual.FoundInCache.Should().BeTrue();
            actual.Result.Should().BeEquivalentTo(TestData.Languages);
        }

        [TestMethod]
        public async Task GetLanguages_ShouldClearCache_WhenCacheExpired()
        {
            // Arrange
            var timeService = new Mock<IDateTimeService>();
            var sut = new InMemoryCachingService(timeService.Object, GetSettings(2));
            await sut.StoreLanguagesAsync(TestData.Languages);

            // Act
            timeService.Setup(t => t.GetCurrentDateTime()).Returns(DateTime.Now.AddSeconds(7));
            var actual = await sut.GetLanguages();

            // Assert
            actual.Should().BeEquivalentTo(CachedLanguageListResponse.Empty);
        }

        [TestMethod]
        public async Task GetLanguages_ShouldReturnCachedLanguages_WhenCacheHasBeenRenewed()
        {
            // Arrange
            var timeService = new Mock<IDateTimeService>();
            var sut = new InMemoryCachingService(timeService.Object, GetSettings(2));
            timeService.Setup(t => t.GetCurrentDateTime()).Returns(DateTime.Now.AddSeconds(5));
            await sut.StoreLanguagesAsync(TestData.Languages);

            // Act
            var actual = await sut.GetLanguages();

            // Assert
            actual.FoundInCache.Should().BeTrue();
            actual.Result.Should().BeEquivalentTo(TestData.Languages);
        }

        [TestMethod]
        public async Task GetTranslations_ShouldReturnTranslations_WhenCacheIsNotExpired()
        {
            // Arrange
            var sut = new InMemoryCachingService(new DateTimeService(), GetSettings(5));

            await sut.StoreTranslationsAsync(TestData.Language_DE, TestData.Translations_De);

            // Act
            var actual = await sut.GetTranslations(TestData.Language_DE, false);

            // Assert
            actual.Result.Should().BeEquivalentTo(TestData.Translations_De);
        }

        [TestMethod]
        public async Task GetTranslations_ShouldNotFail_WhenNoCacheHit()
        {
            // Act
            var actual = await _sut.GetTranslations(TestData.Language_DE, false);

            // Assert
            actual.Result.Should().BeEquivalentTo(TestData.EmptyTranslations);
        }

        [TestMethod]
        public async Task ClearCache_ShouldClearAllItems()
        {
            // Arrange
            await _sut.StoreTranslationsAsync(TestData.Language_DE, TestData.Translations_De);
            await _sut.StoreTranslationsAsync(TestData.Language_EN, TestData.Translations_En);

            // Act
            await _sut.ClearCache();

            // Assert
            (await _sut.GetTranslations(TestData.Language_DE, false)).Result.Should().BeEquivalentTo(TestData.EmptyTranslations);
            (await _sut.GetTranslations(TestData.Language_EN, false)).Result.Should().BeEquivalentTo(TestData.EmptyTranslations);
        }

        private CalingaServiceSettings GetSettings(uint? expiration = null)
        {
            return new CalingaServiceSettings { MemoryCacheExpirationIntervalInSeconds = expiration == null ? default : expiration.Value };
        }
    }
}
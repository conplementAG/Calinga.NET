using System;
using System.Threading.Tasks;

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

using Calinga.NET.Caching;
using Calinga.NET.Infrastructure;

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
        public async Task GetTranslations_ShoulClearCache_WhenExpired()
        {
            // Arrange
            var timeService = new Mock<IDateTimeService>();
            var service = new InMemoryCachingService(timeService.Object, GetSettings(2));
            await service.StoreTranslationsAsync(TestData.Language_DE, TestData.Translations_De);

            // Act
            timeService.Setup(t => t.GetCurrentDateTime()).Returns(DateTime.Now.AddSeconds(7));
            var actual = await service.GetTranslations(TestData.Language_DE, false);

            // Assert
            actual.Result.Should().BeEquivalentTo(TestData.EmptyTranslations);
        }

        [TestMethod]
        public async Task GetTranslations_ShoulReturnTranslations_WhenCacheIsNotExpired()
        {
            // Arrange
            var service = new InMemoryCachingService(new DateTimeService(), GetSettings(5));

            await service.StoreTranslationsAsync(TestData.Language_DE, TestData.Translations_De);

            // Act
            var actual = await service.GetTranslations(TestData.Language_DE, false);

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

        private CalingaServiceSettings GetSettings(uint? expiration = null) => new CalingaServiceSettings() { MemoryCacheExpirationIntervalInSeconds = expiration == null ? default : expiration.Value };
    }
}

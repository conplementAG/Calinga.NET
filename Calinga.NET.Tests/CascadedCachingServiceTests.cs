using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Calinga.NET.Infrastructure;
using Calinga.NET.Caching;
using Moq;
using NSubstitute;

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
            _firstLevelCachingService.Setup(x => x.GetTranslations(TestData.Language_DE, false)).Returns(Task.FromResult(TestData.Translations_De));
            _secondLevelCachingService.Setup(x => x.GetTranslations(TestData.Language_DE, false)).Returns(Task.FromResult(TestData.EmptyTranslations));

            // Act
            var actual = await _sut.GetTranslations(TestData.Language_DE, false);

            // Assert
            _firstLevelCachingService.Verify(x => x.GetTranslations(TestData.Language_DE, false), Times.Once);
            _secondLevelCachingService.Verify(x => x.GetTranslations(TestData.Language_DE, false), Times.Never);
            actual.Should().BeEquivalentTo(TestData.Translations_De);
        }

        [TestMethod]
        public async Task GetTranslations_ShouldGetTranslationsFromSecondCacheLevel_WhenNotAvailableInFirst()
        {
            // Arrange
            _firstLevelCachingService.Setup(x => x.GetTranslations(TestData.Language_DE, false)).Returns(Task.FromResult(TestData.EmptyTranslations));
            _secondLevelCachingService.Setup(x => x.GetTranslations(TestData.Language_DE, false)).Returns(Task.FromResult(TestData.Translations_De));

            // Act
            var actual = await _sut.GetTranslations(TestData.Language_DE, false);

            // Assert
            _firstLevelCachingService.Verify(x => x.GetTranslations(TestData.Language_DE, false), Times.Once);
            _secondLevelCachingService.Verify(x => x.GetTranslations(TestData.Language_DE, false), Times.Once);
            actual.Should().BeEquivalentTo(TestData.Translations_De);
        }

        [TestMethod]
        public async Task GetTranslations_ShouldNotFail_WhenNoCacheHitInAnyLevel()
        {
            // Arrange
            _firstLevelCachingService.Setup(x => x.GetTranslations(TestData.Language_DE, false)).Returns(Task.FromResult(TestData.EmptyTranslations));
            _secondLevelCachingService.Setup(x => x.GetTranslations(TestData.Language_DE, false)).Returns(Task.FromResult(TestData.EmptyTranslations));

            // Act
            var actual = await _sut.GetTranslations(TestData.Language_DE, false);

            // Assert
            _firstLevelCachingService.Verify(x => x.GetTranslations(TestData.Language_DE, false), Times.Once);
            _secondLevelCachingService.Verify(x => x.GetTranslations(TestData.Language_DE, false), Times.Once);
            actual.Should().BeEquivalentTo(TestData.EmptyTranslations);
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
    }
}

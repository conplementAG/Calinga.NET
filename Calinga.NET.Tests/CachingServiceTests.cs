using System.Threading.Tasks;

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

using Calinga.NET.Infrastructure;
using System.Collections.Generic;
using System;
using Calinga.NET.Infrastructure.Exceptions;
using Moq;

namespace Calinga.NET.Tests
{
    [TestClass]
    public class CachingServiceTests
    {
        private CachingService _cachingService;
        private Mock<IFileSystemService> _fileService;

        [TestInitialize]
        public void Init()
        {
            _fileService = new Mock<IFileSystemService>();

            _cachingService = new CachingService(_fileService.Object);
        }

        [TestMethod]
        public async Task GetTranslations_ShouldGetTranslationsFromCache()
        {
            // Arrange
            _fileService.Setup(x => x.ReadCacheFileAsync(TestData.Language_DE)).Returns(Task.FromResult(TestData.Translations_De));
            var translations = await _cachingService.GetTranslations(TestData.Language_DE, false).ConfigureAwait(false);

            // Act
            var secondCallTranslations = await _cachingService.GetTranslations(TestData.Language_DE, false).ConfigureAwait(false);

            // Assert
            _fileService.Verify(x => x.ReadCacheFileAsync(TestData.Language_DE), Times.Once);

            secondCallTranslations.Should().BeSameAs(translations);
        }

        [TestMethod]
        public void ClearCache_ShouldClearCache()
        {
            // Arrange
            _fileService.Setup(x => x.ReadCacheFileAsync(It.IsAny<string>())).Throws<TranslationsNotAvailableException>();

            // Act
             _cachingService.ClearCache();

            // Assert
            Func<Task<IReadOnlyDictionary<string, string>>> getTranslations = async () => await _cachingService.GetTranslations(TestData.Language_DE, false).ConfigureAwait(false);
            getTranslations.Should().Throw<TranslationsNotAvailableException>();
        }
    }
}

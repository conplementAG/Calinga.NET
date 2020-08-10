using System.Linq;
using System.Threading.Tasks;

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

using Calinga.NET.Infrastructure;

namespace Calinga.NET.Tests
{
    [TestClass]
    public class CachingServiceTests
    {
        private IConsumerHttpClient _consumerHttpClient;
        private ICachingService _cachingService;
        private IFileService _fileService;

        [TestInitialize]
        public void Init()
        {
            _consumerHttpClient = Substitute.For<IConsumerHttpClient>();
            _consumerHttpClient.GetTranslationsAsync(TestData.Language_DE).Returns(TestData.Translations_De);
            _consumerHttpClient.GetTranslationsAsync(TestData.Language_EN).Returns(TestData.Translations_En);
            _consumerHttpClient.GetLanguagesAsync().Returns(TestData.Languages);
            _fileService = Substitute.For<IFileService>();

            _cachingService = new CachingService(_consumerHttpClient, _fileService);
        }

        [TestMethod]
        public async Task GetTranslations_ShouldGetTranslationsFromCalingaService()
        {
            // Arrange & Act
            var translations = await _cachingService.GetTranslations(TestData.Language_DE, false).ConfigureAwait(false);

            // Assert
            translations.ContainsKey(TestData.Key_1).Should().BeTrue();
            translations.ContainsKey(TestData.Key_2).Should().BeTrue();

            await _consumerHttpClient.Received().GetTranslationsAsync(TestData.Language_DE).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task GetTranslations_ShouldGetTranslationsFromCalingaService_AndSaveInFile()
        {
            // Arrange & Act
            var translations = await _cachingService.GetTranslations(TestData.Language_DE, false).ConfigureAwait(false);

            // Assert
            translations.ContainsKey(TestData.Key_1).Should().BeTrue();
            translations.ContainsKey(TestData.Key_2).Should().BeTrue();

            await _consumerHttpClient.Received().GetTranslationsAsync(TestData.Language_DE).ConfigureAwait(false);
            await _fileService.Received().SaveTranslationsAsync(TestData.Language_DE, Arg.Any<string>())
                .ConfigureAwait(false);
        }

        [TestMethod]
        public async Task GetTranslations_ShouldGetTranslationsFromCache()
        {
            // Arrange
            var translations = await _cachingService.GetTranslations(TestData.Language_DE, false).ConfigureAwait(false);
            await _consumerHttpClient.Received().GetTranslationsAsync(Arg.Any<string>()).ConfigureAwait(false);

            // Act
            var secondCallTranslations = await _cachingService.GetTranslations(TestData.Language_DE, false).ConfigureAwait(false);

            // Assert
            _consumerHttpClient.DidNotReceive();
            _fileService.DidNotReceive();

            secondCallTranslations.Should().BeSameAs(translations);
        }

        [TestMethod]
        public async Task GetLanguages_ShouldGetTranslationsFromCalingaService()
        {
            // Arrange & Act
            var languages = await _cachingService.GetLanguages().ConfigureAwait(false);

            // Assert
            await _consumerHttpClient.Received().GetLanguagesAsync().ConfigureAwait(false);

            Assert.IsNotNull(languages.FirstOrDefault(l => l == TestData.Language_DE));
            Assert.IsNotNull(languages.FirstOrDefault(l => l == TestData.Language_EN));
        }

        [TestMethod]
        public async Task GetLanguages_ShouldGetTranslationsFromCache()
        {
            // Arrange
            var languages = await _cachingService.GetLanguages().ConfigureAwait(false);

            await _consumerHttpClient.Received().GetLanguagesAsync().ConfigureAwait(false);

            // Act
            var secondCallLanguages = await _cachingService.GetLanguages().ConfigureAwait(false);

            // Assert
            _consumerHttpClient.DidNotReceive();
            secondCallLanguages.Should().BeSameAs(languages);
        }

        [TestMethod]
        public async Task ClearCache_ShouldClearCache()
        {
            // Arrange
             await _cachingService.GetLanguages().ConfigureAwait(false);
             await _consumerHttpClient.Received().GetLanguagesAsync().ConfigureAwait(false);

            // Act
             _cachingService.ClearCache();

            // Assert
            await _consumerHttpClient.Received().GetLanguagesAsync().ConfigureAwait(false);
        }
    }
}

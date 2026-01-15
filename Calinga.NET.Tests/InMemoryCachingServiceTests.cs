using System;
using System.Collections.Generic;
using System.Linq;
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

        #region Thread Safety Tests

        [TestMethod]
        public async Task StoreTranslationsAsync_ShouldNotThrow_WhenSameLanguageStoredConcurrently()
        {
            // Arrange
            var sut = new InMemoryCachingService(new DateTimeService(), GetSettings(60));
            var tasks = new List<Task>();

            // Act - Store same language concurrently from multiple threads
            for (int i = 0; i < 100; i++)
            {
                tasks.Add(Task.Run(() => sut.StoreTranslationsAsync(TestData.Language_DE, TestData.Translations_De)));
            }

            // Assert - Should not throw
            Func<Task> act = async () => await Task.WhenAll(tasks);
            await act.Should().NotThrowAsync("concurrent stores of the same language should not throw");

            // Verify data is correctly stored
            var result = await sut.GetTranslations(TestData.Language_DE, false);
            result.FoundInCache.Should().BeTrue();
            result.Result.Should().BeEquivalentTo(TestData.Translations_De);
        }

        [TestMethod]
        public async Task StoreTranslationsAsync_ShouldNotThrow_WhenDifferentLanguagesStoredConcurrently()
        {
            // Arrange
            var sut = new InMemoryCachingService(new DateTimeService(), GetSettings(60));
            var languages = Enumerable.Range(0, 100).Select(i => $"lang_{i}").ToList();
            var translations = TestData.Translations_De;

            // Act - Store different languages concurrently
            var tasks = languages.Select(lang =>
                Task.Run(() => sut.StoreTranslationsAsync(lang, translations))).ToList();

            // Assert - Should not throw
            Func<Task> act = async () => await Task.WhenAll(tasks);
            await act.Should().NotThrowAsync("concurrent stores of different languages should not throw");

            // Verify all languages are stored
            foreach (var lang in languages)
            {
                var result = await sut.GetTranslations(lang, false);
                result.FoundInCache.Should().BeTrue($"language {lang} should be cached");
            }
        }

        [TestMethod]
        public async Task GetTranslations_ShouldNotThrow_WhenReadingWhileWriting()
        {
            // Arrange
            var sut = new InMemoryCachingService(new DateTimeService(), GetSettings(60));
            await sut.StoreTranslationsAsync(TestData.Language_DE, TestData.Translations_De);

            var readTasks = new List<Task<CacheResponse>>();
            var writeTasks = new List<Task>();

            // Act - Concurrent reads and writes
            for (int i = 0; i < 100; i++)
            {
                readTasks.Add(Task.Run(() => sut.GetTranslations(TestData.Language_DE, false)));
                writeTasks.Add(Task.Run(() => sut.StoreTranslationsAsync($"lang_{i}", TestData.Translations_En)));
            }

            // Assert - Should not throw
            Func<Task> act = async () =>
            {
                await Task.WhenAll(writeTasks);
                await Task.WhenAll(readTasks);
            };
            await act.Should().NotThrowAsync("concurrent reads and writes should not throw");
        }

        [TestMethod]
        public async Task StoreLanguagesAsync_ShouldNotThrow_WhenCalledConcurrently()
        {
            // Arrange
            var sut = new InMemoryCachingService(new DateTimeService(), GetSettings(60));
            var tasks = new List<Task>();

            // Act - Store languages concurrently
            for (int i = 0; i < 100; i++)
            {
                tasks.Add(Task.Run(() => sut.StoreLanguagesAsync(TestData.Languages)));
            }

            // Assert - Should not throw
            Func<Task> act = async () => await Task.WhenAll(tasks);
            await act.Should().NotThrowAsync("concurrent language stores should not throw");

            // Verify data is correctly stored
            var result = await sut.GetLanguages();
            result.FoundInCache.Should().BeTrue();
            result.Result.Should().BeEquivalentTo(TestData.Languages);
        }

        [TestMethod]
        public async Task GetLanguages_ShouldNotThrow_WhenReadingWhileWriting()
        {
            // Arrange
            var sut = new InMemoryCachingService(new DateTimeService(), GetSettings(60));
            await sut.StoreLanguagesAsync(TestData.Languages);

            var readTasks = new List<Task<CachedLanguageListResponse>>();
            var writeTasks = new List<Task>();

            // Act - Concurrent reads and writes
            for (int i = 0; i < 100; i++)
            {
                readTasks.Add(Task.Run(() => sut.GetLanguages()));
                writeTasks.Add(Task.Run(() => sut.StoreLanguagesAsync(TestData.Languages)));
            }

            // Assert - Should not throw
            Func<Task> act = async () =>
            {
                await Task.WhenAll(writeTasks);
                await Task.WhenAll(readTasks);
            };
            await act.Should().NotThrowAsync("concurrent reads and writes should not throw");
        }

        [TestMethod]
        public async Task ClearCache_ShouldNotThrow_WhenCalledConcurrentlyWithReadsAndWrites()
        {
            // Arrange
            var sut = new InMemoryCachingService(new DateTimeService(), GetSettings(60));
            await sut.StoreTranslationsAsync(TestData.Language_DE, TestData.Translations_De);

            var tasks = new List<Task>();

            // Act - Concurrent clears, reads, and writes
            for (int i = 0; i < 50; i++)
            {
                tasks.Add(Task.Run(() => sut.ClearCache()));
                tasks.Add(Task.Run(() => sut.GetTranslations(TestData.Language_DE, false)));
                tasks.Add(Task.Run(() => sut.StoreTranslationsAsync(TestData.Language_DE, TestData.Translations_De)));
            }

            // Assert - Should not throw
            Func<Task> act = async () => await Task.WhenAll(tasks);
            await act.Should().NotThrowAsync("concurrent clears, reads, and writes should not throw");
        }

        #endregion
    }
}
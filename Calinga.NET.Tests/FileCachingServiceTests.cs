using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Calinga.NET.Caching;
using Calinga.NET.Infrastructure;
using Calinga.NET.Infrastructure.Exceptions;
using FluentAssertions;
using Moq;
using System.Text.Json;

namespace Calinga.NET.Tests
{
    [TestClass]
    public class FileCachingServiceTests
    {
        private Mock<ILogger> _logger;
        private Mock<IFileSystem> _fileSystem;
        private CalingaServiceSettings _settings;
        private FileCachingService _service;

        [TestInitialize]
        public void Init()
        {
            _logger = new Mock<ILogger>();
            _fileSystem = new Mock<IFileSystem>();
            _settings = new CalingaServiceSettings
            {
                DoNotWriteCacheFiles = false,
                CacheDirectory = "test_cache",
                Organization = "org",
                Team = "team",
                Project = "project"
            };
            _service = new FileCachingService(_settings, _logger.Object, _fileSystem.Object);
        }

        [TestMethod]
        public async Task StoreTranslationsAsync_CreatesFileWithValidJson()
        {
            // Arrange
            var translations = new Dictionary<string, string> { { "key1", "value1" } };
            var language = "en";
            var path = Path.Combine(_settings.CacheDirectory, _settings.Organization, _settings.Team, _settings.Project, "EN.json");
            var tempFilePath = Path.Combine(_settings.CacheDirectory, _settings.Organization, _settings.Team, _settings.Project, "EN.json.temp");
            _fileSystem.Setup(fs => fs.CreateDirectory(It.IsAny<string>()));
            _fileSystem.Setup(fs => fs.WriteAllTextAsync(tempFilePath, It.IsAny<string>())).Returns(Task.CompletedTask);
            _fileSystem.Setup(fs => fs.ReadAllTextAsync(tempFilePath)).ReturnsAsync(JsonSerializer.Serialize(translations));
            _fileSystem.Setup(fs => fs.FileExists(path)).Returns(false);
            _fileSystem.Setup(fs => fs.ReplaceFile(tempFilePath, path, null));

            // Act
            await _service.StoreTranslationsAsync(language, translations);

            // Assert
            _fileSystem.Verify(fs => fs.CreateDirectory(It.IsAny<string>()), Times.Once);
            _fileSystem.Verify(fs => fs.WriteAllTextAsync(tempFilePath, It.IsAny<string>()), Times.Once);
            _fileSystem.Verify(fs => fs.ReplaceFile(tempFilePath, path, null), Times.Once);
        }

        [TestMethod]
        public async Task StoreTranslationsAsync_DoesNotCreateFileWhenDoNotWriteCacheFilesIsTrue()
        {
            // Arrange
            _settings.DoNotWriteCacheFiles = true;
            var translations = new Dictionary<string, string> { { "key1", "value1" } };
            var language = "en";

            // Act
            await _service.StoreTranslationsAsync(language, translations);

            // Assert
            _fileSystem.Verify(fs => fs.CreateDirectory(It.IsAny<string>()), Times.Never);
            _fileSystem.Verify(fs => fs.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public async Task StoreTranslationsAsync_LogsWarningOnIOException()
        {
            // Arrange
            var translations = new Dictionary<string, string> { { "key1", "value1" } };
            var language = "en";
            var tempFilePath = Path.Combine(_settings.CacheDirectory, _settings.Organization, _settings.Team, _settings.Project, "EN.json.temp");
            _fileSystem.Setup(fs => fs.CreateDirectory(It.IsAny<string>()));
            _fileSystem.Setup(fs => fs.WriteAllTextAsync(tempFilePath, It.IsAny<string>())).Throws<IOException>();

            // Act
            await _service.StoreTranslationsAsync(language, translations);

            // Assert
            _logger.Verify(l => l.Warn(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public async Task StoreTranslationsAsync_OverwritesExistingFile()
        {
            // Arrange
            var translations = new Dictionary<string, string> { { "key1", "value1" } };
            var language = "en";
            var path = Path.Combine(_settings.CacheDirectory, _settings.Organization, _settings.Team, _settings.Project, "EN.json");
            var tempFilePath = Path.Combine(_settings.CacheDirectory, _settings.Organization, _settings.Team, _settings.Project, "EN.json.temp");
            var prevFilePath = Path.Combine(_settings.CacheDirectory, _settings.Organization, _settings.Team, _settings.Project, "EN.json.prev");
            _fileSystem.Setup(fs => fs.CreateDirectory(It.IsAny<string>()));
            _fileSystem.Setup(fs => fs.WriteAllTextAsync(tempFilePath, It.IsAny<string>())).Returns(Task.CompletedTask);
            _fileSystem.Setup(fs => fs.ReadAllTextAsync(tempFilePath)).ReturnsAsync(JsonSerializer.Serialize(translations));
            _fileSystem.Setup(fs => fs.FileExists(path)).Returns(true);
            _fileSystem.Setup(fs => fs.ReplaceFile(tempFilePath, path, null));

            // Act
            await _service.StoreTranslationsAsync(language, translations);

            // Assert
            _fileSystem.Verify(fs => fs.ReplaceFile(path, prevFilePath, null), Times.Once);
            _fileSystem.Verify(fs => fs.ReplaceFile(tempFilePath, path, null), Times.Once);
        }
        

        [TestMethod]
        public async Task StoreTranslationsAsync_HandlesEmptyTranslations()
        {
            // Arrange
            var translations = new Dictionary<string, string>();
            var language = "en";
            var path = Path.Combine(_settings.CacheDirectory, _settings.Organization, _settings.Team, _settings.Project, "EN.json");
            var tempFilePath = Path.Combine(_settings.CacheDirectory, _settings.Organization, _settings.Team, _settings.Project, "EN.json.temp");
            _fileSystem.Setup(fs => fs.CreateDirectory(It.IsAny<string>()));
            _fileSystem.Setup(fs => fs.WriteAllTextAsync(tempFilePath, It.IsAny<string>())).Returns(Task.CompletedTask);
            _fileSystem.Setup(fs => fs.ReadAllTextAsync(tempFilePath)).ReturnsAsync(JsonSerializer.Serialize(translations));
            _fileSystem.Setup(fs => fs.FileExists(path)).Returns(false);
            _fileSystem.Setup(fs => fs.ReplaceFile(tempFilePath, path, null));

            // Act
            await _service.StoreTranslationsAsync(language, translations);

            // Assert
            _fileSystem.Verify(fs => fs.CreateDirectory(It.IsAny<string>()), Times.Once);
            _fileSystem.Verify(fs => fs.WriteAllTextAsync(tempFilePath, It.IsAny<string>()), Times.Once);
            _fileSystem.Verify(fs => fs.ReplaceFile(tempFilePath, path, null), Times.Once);
        }

        [TestMethod]
        public async Task GetTranslations_FileDoesNotExist_ReturnsEmptyCacheResponse()
        {
            // Arrange
            var language = "en";
            var path = Path.Combine(_settings.CacheDirectory, _settings.Organization, _settings.Team, _settings.Project, "EN.json");
            _fileSystem.Setup(fs => fs.FileExists(path)).Returns(false);

            // Act
            var result = await _service.GetTranslations(language, false);

            // Assert
            Assert.IsFalse(result.FoundTranslationsInCache);
            Assert.AreEqual(0, result.Result.Count);
        }

        [TestMethod]
        public async Task GetTranslations_FileExists_ReturnsValidTranslations()
        {
            // Arrange
            var language = "en";
            var path = Path.Combine(_settings.CacheDirectory, _settings.Organization, _settings.Team, _settings.Project, "EN.json");
            var translations = new Dictionary<string, string> { { "key1", "value1" } };
            _fileSystem.Setup(fs => fs.FileExists(path)).Returns(true);
            _fileSystem.Setup(fs => fs.ReadAllTextAsync(path)).ReturnsAsync(JsonSerializer.Serialize(translations));

            // Act
            var result = await _service.GetTranslations(language, false);

            // Assert
            Assert.IsTrue(result.FoundTranslationsInCache);
            CollectionAssert.AreEquivalent(translations.ToList(), result.Result.ToList());
        }

        [TestMethod]
        public async Task GetTranslations_FileExists_ThrowsIOException()
        {
            // Arrange
            var language = "en";
            var path = Path.Combine(_settings.CacheDirectory, _settings.Organization, _settings.Team, _settings.Project, "EN.json");
            _fileSystem.Setup(fs => fs.FileExists(path)).Returns(true);
            _fileSystem.Setup(fs => fs.ReadAllTextAsync(path)).Throws<IOException>();

            // Act & Assert
            await Assert.ThrowsExactlyAsync<TranslationsNotAvailableException>(() => _service.GetTranslations(language, false));
        }

        [TestMethod]
        public async Task GetLanguages_FileDoesNotExist_ReturnsEmptyCachedLanguageListResponse()
        {
            // Arrange
            var path = Path.Combine(_settings.CacheDirectory, _settings.Organization, _settings.Team, _settings.Project, "Languages.json");
            _fileSystem.Setup(fs => fs.FileExists(path)).Returns(false);

            // Act
            var result = await _service.GetLanguages();

            // Assert
            Assert.IsFalse(result.FoundInCache);
            Assert.AreEqual(0, result.Result.Count);
        }

        [TestMethod]
        public async Task GetLanguages_FileExists_ReturnsValidLanguages()
        {
            // Arrange
            var path = Path.Combine(_settings.CacheDirectory, _settings.Organization, _settings.Team, _settings.Project, "Languages.json");
            var languages = new List<Language> { new Language { Name = "en" } };
            _fileSystem.Setup(fs => fs.FileExists(path)).Returns(true);
            _fileSystem.Setup(fs => fs.ReadAllTextAsync(path)).ReturnsAsync(JsonSerializer.Serialize(languages));

            // Act
            var result = await _service.GetLanguages();

            // Assert
            Assert.IsTrue(result.FoundInCache);
            Assert.AreEqual(1, result.Result.Count);
            Assert.AreEqual("en", result.Result[0].Name);
            Assert.IsFalse(result.Result[0].IsReference);
        }

        [TestMethod]
        public async Task GetLanguages_FileExists_ThrowsIOException()
        {
            // Arrange
            var path = Path.Combine(_settings.CacheDirectory, _settings.Organization, _settings.Team, _settings.Project, "Languages.json");
            _fileSystem.Setup(fs => fs.FileExists(path)).Returns(true);
            _fileSystem.Setup(fs => fs.ReadAllTextAsync(path)).Throws<IOException>();

            // Act & Assert
            await Assert.ThrowsExactlyAsync<TranslationsNotAvailableException>(() => _service.GetLanguages());
        }

        [TestMethod]
        public async Task ClearCache_DoNotWriteCacheFilesIsTrue_DoesNothing()
        {
            // Arrange
            _settings.DoNotWriteCacheFiles = true;

            // Act
            await _service.ClearCache();

            // Assert
            _fileSystem.Verify(fs => fs.DeleteDirectory(It.IsAny<string>()), Times.Never);
        }
        
        [TestMethod]
        public async Task StoreLanguagesAsync_DoNotWriteCacheFilesIsTrue_DoesNothing()
        {
            // Arrange
            _settings.DoNotWriteCacheFiles = true;
            var languages = new List<Language> { new Language { Name = "en" } };

            // Act
            await _service.StoreLanguagesAsync(languages);

            // Assert
            _fileSystem.Verify(fs => fs.CreateDirectory(It.IsAny<string>()), Times.Never);
            _fileSystem.Verify(fs => fs.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public async Task StoreLanguagesAsync_CreatesFileWithValidJson()
        {
            // Arrange
            var languages = new List<Language> { new Language { Name = "en" } };
            var path = Path.Combine(_settings.CacheDirectory, _settings.Organization, _settings.Team, _settings.Project, "Languages.json");
            var tempFilePath = Path.Combine(_settings.CacheDirectory, _settings.Organization, _settings.Team, _settings.Project,
                "Languages.json.temp");
            _fileSystem.Setup(fs => fs.CreateDirectory(It.IsAny<string>()));
            _fileSystem.Setup(fs => fs.WriteAllTextAsync(tempFilePath, It.IsAny<string>())).Returns(Task.CompletedTask);
            _fileSystem.Setup(fs => fs.ReadAllTextAsync(tempFilePath)).ReturnsAsync(JsonSerializer.Serialize(languages));
            _fileSystem.Setup(fs => fs.FileExists(path)).Returns(false);
            _fileSystem.Setup(fs => fs.ReplaceFile(tempFilePath, path, null));

            // Act
            await _service.StoreLanguagesAsync(languages);

            // Assert
            _fileSystem.Verify(fs => fs.CreateDirectory(It.IsAny<string>()), Times.Once);
            _fileSystem.Verify(fs => fs.WriteAllTextAsync(tempFilePath, It.IsAny<string>()), Times.Once);
            _fileSystem.Verify(fs => fs.ReplaceFile(tempFilePath, path, null), Times.Once);
        }

        [TestMethod]
        public async Task StoreLanguagesAsync_ThrowsIOException()
        {
            // Arrange
            var languages = new List<Language> { new Language { Name = "en" } };
            var tempFilePath = Path.Combine(_settings.CacheDirectory, _settings.Organization, _settings.Team, _settings.Project,
                "Languages.json.temp");
            _fileSystem.Setup(fs => fs.CreateDirectory(It.IsAny<string>()));
            _fileSystem.Setup(fs => fs.WriteAllTextAsync(tempFilePath, It.IsAny<string>())).Throws<IOException>();

            // Act
            await _service.StoreLanguagesAsync(languages);

            // Assert
            _logger.Verify(l => l.Warn(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public async Task StoreTranslationsAsync_InvalidLanguage_ThrowsArgumentException()
        {
            // Arrange
            var translations = new Dictionary<string, string> { { "key1", "value1" } };
            var invalidLanguage = "../en";

            // Act & Assert
            await Assert.ThrowsExactlyAsync<ArgumentException>(() => _service.StoreTranslationsAsync(invalidLanguage, translations));
        }

        [TestMethod]
        public async Task StoreTranslationsAsync_NullOrEmptyTranslations_WritesEmptyJson()
        {
            // Arrange
            var language = "en";
            var path = Path.Combine(_settings.CacheDirectory, _settings.Organization, _settings.Team, _settings.Project, "EN.json");
            var tempFilePath = Path.Combine(_settings.CacheDirectory, _settings.Organization, _settings.Team, _settings.Project, "EN.json.temp");
            _fileSystem.Setup(fs => fs.CreateDirectory(It.IsAny<string>()));
            _fileSystem.Setup(fs => fs.WriteAllTextAsync(tempFilePath, It.IsAny<string>())).Returns(Task.CompletedTask);
            _fileSystem.Setup(fs => fs.ReadAllTextAsync(tempFilePath)).ReturnsAsync("{}");
            _fileSystem.Setup(fs => fs.FileExists(path)).Returns(false);
            _fileSystem.Setup(fs => fs.ReplaceFile(tempFilePath, path, null));

            // Act
            await _service.StoreTranslationsAsync(language, new Dictionary<string, string>());

            // Assert
            _fileSystem.Verify(fs => fs.WriteAllTextAsync(tempFilePath, It.IsAny<string>()), Times.Once);
            _fileSystem.Verify(fs => fs.ReplaceFile(tempFilePath, path, null), Times.Once);
        }

        [TestMethod]
        public async Task StoreLanguagesAsync_LogsWarningOnJsonException()
        {
            // Arrange
            var languages = new List<Language> { new Language { Name = "en" } };
            var tempFilePath = Path.Combine(_settings.CacheDirectory, _settings.Organization, _settings.Team, _settings.Project, "Languages.json.temp");
            _fileSystem.Setup(fs => fs.CreateDirectory(It.IsAny<string>()));
            _fileSystem.Setup(fs => fs.WriteAllTextAsync(tempFilePath, It.IsAny<string>())).Returns(Task.CompletedTask);
            _fileSystem.Setup(fs => fs.ReadAllTextAsync(tempFilePath)).Throws<JsonException>();
            _fileSystem.Setup(fs => fs.FileExists(tempFilePath)).Returns(true);

            // Act
            await _service.StoreLanguagesAsync(languages);

            // Assert
            _logger.Verify(l => l.Warn(It.Is<string>(s => s.Contains("Invalid JSON"))), Times.Once);
            _fileSystem.Verify(fs => fs.DeleteFile(tempFilePath), Times.Once);
        }

        [TestMethod]
        public async Task GetTranslations_InvalidJson_ThrowsExceptionAndLogsWarning()
        {
            // Arrange
            var language = "en";
            var path = Path.Combine(_settings.CacheDirectory, _settings.Organization, _settings.Team, _settings.Project, "EN.json");
            _fileSystem.Setup(fs => fs.FileExists(path)).Returns(true);
            _fileSystem.Setup(fs => fs.ReadAllTextAsync(path)).ReturnsAsync("{invalid json}");

            // Act & Assert
            await Assert.ThrowsExactlyAsync<JsonException>(() => _service.GetTranslations(language, false));
        }

        [TestMethod]
        public async Task GetLanguages_InvalidJson_ThrowsExceptionAndLogsWarning()
        {
            // Arrange
            var path = Path.Combine(_settings.CacheDirectory, _settings.Organization, _settings.Team, _settings.Project, "Languages.json");
            _fileSystem.Setup(fs => fs.FileExists(path)).Returns(true);
            _fileSystem.Setup(fs => fs.ReadAllTextAsync(path)).ReturnsAsync("{invalid json}");

            // Act & Assert
            await Assert.ThrowsExactlyAsync<JsonException>(() => _service.GetLanguages());
        }

        [TestMethod]
        public async Task ClearCache_DirectoryDoesNotExist_DoesNotThrow()
        {
            // Arrange
            var dirInfo = new DirectoryInfo("not_existing_dir");
            // Act & Assert
            await _service.ClearCache();
        }

        [TestMethod]
        public async Task StoreTranslationsAsync_DeletesTempFileOnIOException()
        {
            // Arrange
            var translations = new Dictionary<string, string> { { "key1", "value1" } };
            var language = "en";
            var tempFilePath = Path.Combine(_settings.CacheDirectory, _settings.Organization, _settings.Team, _settings.Project, "EN.json.temp");
            _fileSystem.Setup(fs => fs.CreateDirectory(It.IsAny<string>()));
            _fileSystem.Setup(fs => fs.WriteAllTextAsync(tempFilePath, It.IsAny<string>())).Throws<IOException>();
            _fileSystem.Setup(fs => fs.FileExists(tempFilePath)).Returns(true);

            // Act
            await _service.StoreTranslationsAsync(language, translations);

            // Assert
            _fileSystem.Verify(fs => fs.DeleteFile(tempFilePath), Times.Once);
        }

        [TestMethod]
        public async Task StoreLanguagesAsync_DeletesTempFileOnIOException()
        {
            // Arrange
            var languages = new List<Language> { new Language { Name = "en" } };
            var tempFilePath = Path.Combine(_settings.CacheDirectory, _settings.Organization, _settings.Team, _settings.Project, "Languages.json.temp");
            _fileSystem.Setup(fs => fs.CreateDirectory(It.IsAny<string>()));
            _fileSystem.Setup(fs => fs.WriteAllTextAsync(tempFilePath, It.IsAny<string>())).Throws<IOException>();
            _fileSystem.Setup(fs => fs.FileExists(tempFilePath)).Returns(true);

            // Act
            await _service.StoreLanguagesAsync(languages);

            // Assert
            _fileSystem.Verify(fs => fs.DeleteFile(tempFilePath), Times.Once);
        }

        [TestMethod]
        public async Task StoreTranslationsAsync_LogsInfoOnSuccess()
        {
            // Arrange
            var translations = new Dictionary<string, string> { { "key1", "value1" } };
            var language = "en";
            var path = Path.Combine(_settings.CacheDirectory, _settings.Organization, _settings.Team, _settings.Project, "EN.json");
            var tempFilePath = Path.Combine(_settings.CacheDirectory, _settings.Organization, _settings.Team, _settings.Project, "EN.json.temp");
            _fileSystem.Setup(fs => fs.CreateDirectory(It.IsAny<string>()));
            _fileSystem.Setup(fs => fs.WriteAllTextAsync(tempFilePath, It.IsAny<string>())).Returns(Task.CompletedTask);
            _fileSystem.Setup(fs => fs.ReadAllTextAsync(tempFilePath)).ReturnsAsync(JsonSerializer.Serialize(translations));
            _fileSystem.Setup(fs => fs.FileExists(path)).Returns(false);
            _fileSystem.Setup(fs => fs.ReplaceFile(tempFilePath, path, null));

            // Act
            await _service.StoreTranslationsAsync(language, translations);

            // Assert
            _logger.Verify(l => l.Info(It.Is<string>(s => s.Contains("stored in cache"))), Times.Once);
        }

        [TestMethod]
        public async Task StoreLanguagesAsync_LogsWarningOnJsonExceptionAndDeletesTempFile()
        {
            // Arrange
            var languages = new List<Language> { new Language { Name = "en" } };
            var tempFilePath = Path.Combine(_settings.CacheDirectory, _settings.Organization, _settings.Team, _settings.Project, "Languages.json.temp");
            _fileSystem.Setup(fs => fs.CreateDirectory(It.IsAny<string>()));
            _fileSystem.Setup(fs => fs.WriteAllTextAsync(tempFilePath, It.IsAny<string>())).Returns(Task.CompletedTask);
            _fileSystem.Setup(fs => fs.ReadAllTextAsync(tempFilePath)).Throws<JsonException>();
            _fileSystem.Setup(fs => fs.FileExists(tempFilePath)).Returns(true);

            // Act
            await _service.StoreLanguagesAsync(languages);

            // Assert
            _logger.Verify(l => l.Warn(It.Is<string>(s => s.Contains("Invalid JSON"))), Times.Once);
            _fileSystem.Verify(fs => fs.DeleteFile(tempFilePath), Times.Once);
        }

        [TestMethod]
        public async Task StoreTranslationsAsync_NullTranslations_ThrowsArgumentNullException()
        {
            // Arrange
            string language = "en";
            IReadOnlyDictionary<string, string> translations = null;

            // Act & Assert
            await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => _service.StoreTranslationsAsync(language, translations));
        }

        [TestMethod]
        public async Task StoreLanguagesAsync_NullLanguageList_ThrowsArgumentNullException()
        {
            // Arrange
            IEnumerable<Language> languages = null;

            // Act & Assert
            await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => _service.StoreLanguagesAsync(languages));
        }

        [TestMethod]
        public async Task GetTranslations_EmptyFile_ReturnsEmptyDictionary()
        {
            // Arrange
            var language = "en";
            var path = Path.Combine(_settings.CacheDirectory, _settings.Organization, _settings.Team, _settings.Project, "EN.json");
            _fileSystem.Setup(fs => fs.FileExists(path)).Returns(true);
            _fileSystem.Setup(fs => fs.ReadAllTextAsync(path)).ReturnsAsync("");

            // Act
            var result = await _service.GetTranslations(language, false);

            // Assert
            Assert.IsTrue(result.FoundTranslationsInCache);
            Assert.AreEqual(0, result.Result.Count);
        }

        [TestMethod]
        public async Task GetLanguages_EmptyFile_ReturnsEmptyList()
        {
            // Arrange
            var path = Path.Combine(_settings.CacheDirectory, _settings.Organization, _settings.Team, _settings.Project, "Languages.json");
            _fileSystem.Setup(fs => fs.FileExists(path)).Returns(true);
            _fileSystem.Setup(fs => fs.ReadAllTextAsync(path)).ReturnsAsync("");

            // Act
            var result = await _service.GetLanguages();

            // Assert
            Assert.IsTrue(result.FoundInCache);
            Assert.AreEqual(0, result.Result.Count);
        }

        [TestMethod]
        public async Task StoreTranslationsAsync_ReplaceFileThrowsIOException_DeletesTempFileAndLogsWarning()
        {
            // Arrange
            var translations = new Dictionary<string, string> { { "key1", "value1" } };
            var language = "en";
            var path = Path.Combine(_settings.CacheDirectory, _settings.Organization, _settings.Team, _settings.Project, "EN.json");
            var tempFilePath = Path.Combine(_settings.CacheDirectory, _settings.Organization, _settings.Team, _settings.Project, "EN.json.temp");
            _fileSystem.Setup(fs => fs.CreateDirectory(It.IsAny<string>()));
            _fileSystem.Setup(fs => fs.WriteAllTextAsync(tempFilePath, It.IsAny<string>())).Returns(Task.CompletedTask);
            _fileSystem.Setup(fs => fs.ReadAllTextAsync(tempFilePath)).ReturnsAsync(JsonSerializer.Serialize(translations));
            _fileSystem.Setup(fs => fs.FileExists(path)).Returns(false);
            _fileSystem.Setup(fs => fs.ReplaceFile(tempFilePath, path, null)).Throws<IOException>();
            _fileSystem.Setup(fs => fs.FileExists(tempFilePath)).Returns(true);

            // Act
            await _service.StoreTranslationsAsync(language, translations);

            // Assert
            _logger.Verify(l => l.Warn(It.IsAny<string>()), Times.AtLeastOnce);
            _fileSystem.Verify(fs => fs.DeleteFile(tempFilePath), Times.Once);
        }

        [TestMethod]
        public async Task StoreLanguagesAsync_ReplaceFileThrowsIOException_DeletesTempFileAndLogsWarning()
        {
            // Arrange
            var languages = new List<Language> { new Language { Name = "en" } };
            var path = Path.Combine(_settings.CacheDirectory, _settings.Organization, _settings.Team, _settings.Project, "Languages.json");
            var tempFilePath = Path.Combine(_settings.CacheDirectory, _settings.Organization, _settings.Team, _settings.Project, "Languages.json.temp");
            _fileSystem.Setup(fs => fs.CreateDirectory(It.IsAny<string>()));
            _fileSystem.Setup(fs => fs.WriteAllTextAsync(tempFilePath, It.IsAny<string>())).Returns(Task.CompletedTask);
            _fileSystem.Setup(fs => fs.ReadAllTextAsync(tempFilePath)).ReturnsAsync(JsonSerializer.Serialize(languages));
            _fileSystem.Setup(fs => fs.FileExists(path)).Returns(false);
            _fileSystem.Setup(fs => fs.ReplaceFile(tempFilePath, path, null)).Throws<IOException>();
            _fileSystem.Setup(fs => fs.FileExists(tempFilePath)).Returns(true);

            // Act
            await _service.StoreLanguagesAsync(languages);

            // Assert
            _logger.Verify(l => l.Warn(It.IsAny<string>()), Times.AtLeastOnce);
            _fileSystem.Verify(fs => fs.DeleteFile(tempFilePath), Times.Once);
        }

        #region Concurrent Access Tests

        [TestMethod]
        public async Task StoreLanguagesAsync_ShouldNotThrow_WhenCalledConcurrently()
        {
            // Arrange - Use real file system for concurrent access test
            var tempDir = Path.Combine(Path.GetTempPath(), $"CalingaTest_{Guid.NewGuid()}");
            var settings = new CalingaServiceSettings
            {
                DoNotWriteCacheFiles = false,
                CacheDirectory = tempDir,
                Organization = "org",
                Team = "team",
                Project = "project"
            };
            var logger = new Mock<ILogger>();
            var service = new FileCachingService(settings, logger.Object);
            var languages = new List<Language>
            {
                new Language { Name = "en", IsReference = true },
                new Language { Name = "de", IsReference = false }
            };

            try
            {
                // Act - Concurrent calls
                var tasks = Enumerable.Range(0, 50)
                    .Select(_ => Task.Run(() => service.StoreLanguagesAsync(languages)))
                    .ToList();

                Func<Task> act = async () => await Task.WhenAll(tasks);

                // Assert - Should not throw
                await act.Should().NotThrowAsync("concurrent StoreLanguagesAsync calls should not throw");

                // Verify file was written correctly
                var result = await service.GetLanguages();
                result.FoundInCache.Should().BeTrue();
                result.Result.Count.Should().Be(2);
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }

        [TestMethod]
        public async Task StoreTranslationsAsync_ShouldNotThrow_WhenCalledConcurrently()
        {
            // Arrange - Use real file system for concurrent access test
            var tempDir = Path.Combine(Path.GetTempPath(), $"CalingaTest_{Guid.NewGuid()}");
            var settings = new CalingaServiceSettings
            {
                DoNotWriteCacheFiles = false,
                CacheDirectory = tempDir,
                Organization = "org",
                Team = "team",
                Project = "project"
            };
            var logger = new Mock<ILogger>();
            var service = new FileCachingService(settings, logger.Object);
            var translations = new Dictionary<string, string>
            {
                { "key1", "value1" },
                { "key2", "value2" }
            };

            try
            {
                // Act - Concurrent calls for same language
                var tasks = Enumerable.Range(0, 50)
                    .Select(_ => Task.Run(() => service.StoreTranslationsAsync("de", translations)))
                    .ToList();

                Func<Task> act = async () => await Task.WhenAll(tasks);

                // Assert - Should not throw
                await act.Should().NotThrowAsync("concurrent StoreTranslationsAsync calls should not throw");

                // Verify file was written correctly
                var result = await service.GetTranslations("de", false);
                result.FoundTranslationsInCache.Should().BeTrue();
                result.Result.Count.Should().Be(2);
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }

        [TestMethod]
        public async Task StoreLanguagesAsync_AndStoreTranslationsAsync_ShouldNotThrow_WhenCalledConcurrently()
        {
            // Arrange - Use real file system for concurrent access test
            var tempDir = Path.Combine(Path.GetTempPath(), $"CalingaTest_{Guid.NewGuid()}");
            var settings = new CalingaServiceSettings
            {
                DoNotWriteCacheFiles = false,
                CacheDirectory = tempDir,
                Organization = "org",
                Team = "team",
                Project = "project"
            };
            var logger = new Mock<ILogger>();
            var service = new FileCachingService(settings, logger.Object);
            var languages = new List<Language> { new Language { Name = "en", IsReference = true } };
            var translations = new Dictionary<string, string> { { "key1", "value1" } };

            try
            {
                // Act - Mix of concurrent language and translation stores
                var tasks = new List<Task>();
                for (int i = 0; i < 25; i++)
                {
                    tasks.Add(Task.Run(() => service.StoreLanguagesAsync(languages)));
                    tasks.Add(Task.Run(() => service.StoreTranslationsAsync("de", translations)));
                }

                Func<Task> act = async () => await Task.WhenAll(tasks);

                // Assert - Should not throw
                await act.Should().NotThrowAsync("concurrent mixed operations should not throw");
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }

        #endregion

        #region Newtonsoft → System.Text.Json compatibility

        [TestMethod]
        public async Task GetTranslations_ReadsNewtonsoftEraFile_Successfully()
        {
            // Arrange — exact byte shape Newtonsoft 13 produced for Dictionary<string, string>:
            // compact, double-quoted keys/values, no whitespace, no BOM. Pinning a literal here
            // (instead of round-tripping through System.Text.Json) is the whole point — proves
            // existing on-disk caches written by 2.1.x are still readable after the JSON-library
            // swap in 2.2.0.
            const string newtonsoftEraJson = "{\"key1\":\"value1\",\"key2\":\"value2\"}";
            var language = "EN";
            var path = Path.Combine(_settings.CacheDirectory, _settings.Organization, _settings.Team, _settings.Project, "EN.json");
            _fileSystem.Setup(fs => fs.FileExists(path)).Returns(true);
            _fileSystem.Setup(fs => fs.ReadAllTextAsync(path)).ReturnsAsync(newtonsoftEraJson);

            // Act
            var result = await _service.GetTranslations(language, false);

            // Assert
            result.FoundTranslationsInCache.Should().BeTrue();
            result.Result.Should().HaveCount(2);
            result.Result["key1"].Should().Be("value1");
            result.Result["key2"].Should().Be("value2");
        }

        [TestMethod]
        public async Task GetLanguages_ReadsNewtonsoftEraFile_Successfully()
        {
            // Arrange — Newtonsoft 13 default for List<Language>: PascalCase property names,
            // no whitespace, double-quoted strings, JSON booleans lowercase. Same rationale as
            // the translations test — pin a literal so any future serializer-options change
            // (e.g. JsonNamingPolicy.CamelCase) surfaces as a failing test, not a broken cache.
            const string newtonsoftEraJson = "[{\"Name\":\"en\",\"IsReference\":true},{\"Name\":\"de\",\"IsReference\":false}]";
            var path = Path.Combine(_settings.CacheDirectory, _settings.Organization, _settings.Team, _settings.Project, "Languages.json");
            _fileSystem.Setup(fs => fs.FileExists(path)).Returns(true);
            _fileSystem.Setup(fs => fs.ReadAllTextAsync(path)).ReturnsAsync(newtonsoftEraJson);

            // Act
            var result = await _service.GetLanguages();

            // Assert
            result.FoundInCache.Should().BeTrue();
            result.Result.Should().HaveCount(2);
            result.Result.Should().ContainSingle(l => l.Name == "en" && l.IsReference);
            result.Result.Should().ContainSingle(l => l.Name == "de" && !l.IsReference);
        }

        #endregion

        #region ETag sidecar

        [TestMethod]
        public async Task StoreTranslationsAsync_WritesETagSidecar_WhenETagProvided()
        {
            // Arrange — sidecar lives next to the translations file with the same
            // language-derived base name and a .etag extension. We must write the
            // tag verbatim so it round-trips byte-for-byte into the next
            // If-None-Match header.
            const string etag = "\"abc123\"";
            var translations = new Dictionary<string, string> { { "key1", "value1" } };
            var language = "en";
            var jsonPath = Path.Combine(_settings.CacheDirectory, _settings.Organization, _settings.Team, _settings.Project, "EN.json");
            var tempFilePath = Path.Combine(_settings.CacheDirectory, _settings.Organization, _settings.Team, _settings.Project, "EN.json.temp");
            var etagPath = Path.Combine(_settings.CacheDirectory, _settings.Organization, _settings.Team, _settings.Project, "EN.etag");
            _fileSystem.Setup(fs => fs.CreateDirectory(It.IsAny<string>()));
            _fileSystem.Setup(fs => fs.WriteAllTextAsync(tempFilePath, It.IsAny<string>())).Returns(Task.CompletedTask);
            _fileSystem.Setup(fs => fs.WriteAllTextAsync(etagPath, etag)).Returns(Task.CompletedTask);
            _fileSystem.Setup(fs => fs.ReadAllTextAsync(tempFilePath)).ReturnsAsync(JsonSerializer.Serialize(translations));
            _fileSystem.Setup(fs => fs.FileExists(jsonPath)).Returns(false);
            _fileSystem.Setup(fs => fs.ReplaceFile(tempFilePath, jsonPath, null));

            // Act
            await _service.StoreTranslationsAsync(language, translations, etag);

            // Assert
            _fileSystem.Verify(fs => fs.WriteAllTextAsync(etagPath, etag), Times.Once);
        }

        [TestMethod]
        public async Task StoreTranslationsAsync_DoesNotWriteSidecar_WhenETagIsNull()
        {
            // Arrange — server returned 200 but emitted no ETag header. We must
            // not create an empty/garbage sidecar that would later be sent as a
            // bogus If-None-Match.
            var translations = new Dictionary<string, string> { { "key1", "value1" } };
            var language = "en";
            var jsonPath = Path.Combine(_settings.CacheDirectory, _settings.Organization, _settings.Team, _settings.Project, "EN.json");
            var tempFilePath = Path.Combine(_settings.CacheDirectory, _settings.Organization, _settings.Team, _settings.Project, "EN.json.temp");
            var etagPath = Path.Combine(_settings.CacheDirectory, _settings.Organization, _settings.Team, _settings.Project, "EN.etag");
            _fileSystem.Setup(fs => fs.CreateDirectory(It.IsAny<string>()));
            _fileSystem.Setup(fs => fs.WriteAllTextAsync(tempFilePath, It.IsAny<string>())).Returns(Task.CompletedTask);
            _fileSystem.Setup(fs => fs.ReadAllTextAsync(tempFilePath)).ReturnsAsync(JsonSerializer.Serialize(translations));
            _fileSystem.Setup(fs => fs.FileExists(jsonPath)).Returns(false);
            _fileSystem.Setup(fs => fs.ReplaceFile(tempFilePath, jsonPath, null));

            // Act
            await _service.StoreTranslationsAsync(language, translations, null);

            // Assert
            _fileSystem.Verify(fs => fs.WriteAllTextAsync(etagPath, It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public async Task GetTranslations_ReadsETagFromSidecar_WhenSidecarExists()
        {
            // Arrange — translations file and sidecar both present. The cache
            // response must surface both so the caller can revalidate.
            const string etag = "\"deadbeef\"";
            var language = "en";
            var jsonPath = Path.Combine(_settings.CacheDirectory, _settings.Organization, _settings.Team, _settings.Project, "EN.json");
            var etagPath = Path.Combine(_settings.CacheDirectory, _settings.Organization, _settings.Team, _settings.Project, "EN.etag");
            var translations = new Dictionary<string, string> { { "key1", "value1" } };
            _fileSystem.Setup(fs => fs.FileExists(jsonPath)).Returns(true);
            _fileSystem.Setup(fs => fs.ReadAllTextAsync(jsonPath)).ReturnsAsync(JsonSerializer.Serialize(translations));
            _fileSystem.Setup(fs => fs.FileExists(etagPath)).Returns(true);
            _fileSystem.Setup(fs => fs.ReadAllTextAsync(etagPath)).ReturnsAsync(etag);

            // Act
            var result = await _service.GetTranslations(language, false);

            // Assert
            result.FoundTranslationsInCache.Should().BeTrue();
            result.ETag.Should().Be(etag);
        }

        [TestMethod]
        public async Task GetTranslations_ReturnsCacheMiss_WhenJsonMissingButETagSidecarPresent()
        {
            // Arrange — orphan sidecar: the .etag file exists on disk but its
            // companion .json does not. Can happen after a partial write,
            // tampered cache dir, or a crash mid-store. The cache must report
            // a clean miss (no exception) so the higher layer falls through to
            // a normal HTTP GET without trying to send a stale If-None-Match.
            var language = "en";
            var jsonPath = Path.Combine(_settings.CacheDirectory, _settings.Organization, _settings.Team, _settings.Project, "EN.json");
            var etagPath = Path.Combine(_settings.CacheDirectory, _settings.Organization, _settings.Team, _settings.Project, "EN.etag");
            _fileSystem.Setup(fs => fs.FileExists(jsonPath)).Returns(false);
            _fileSystem.Setup(fs => fs.FileExists(etagPath)).Returns(true);
            _fileSystem.Setup(fs => fs.ReadAllTextAsync(etagPath)).ReturnsAsync("\"orphan\"");

            // Act
            var result = await _service.GetTranslations(language, false);

            // Assert
            result.FoundTranslationsInCache.Should().BeFalse();
            result.ETag.Should().BeNull();
            // Sidecar was never read (no point — without a body we can't safely revalidate).
            _fileSystem.Verify(fs => fs.ReadAllTextAsync(etagPath), Times.Never);
        }

        [TestMethod]
        public async Task GetTranslations_ETagInLocalCacheIsNull_WhenSidecarMissing()
        {
            // Arrange — pre-ETag cache directory (older clients): translations
            // file exists, sidecar does not. The cache must still return the
            // translations and report ETag = null rather than erroring.
            var language = "en";
            var jsonPath = Path.Combine(_settings.CacheDirectory, _settings.Organization, _settings.Team, _settings.Project, "EN.json");
            var etagPath = Path.Combine(_settings.CacheDirectory, _settings.Organization, _settings.Team, _settings.Project, "EN.etag");
            var translations = new Dictionary<string, string> { { "key1", "value1" } };
            _fileSystem.Setup(fs => fs.FileExists(jsonPath)).Returns(true);
            _fileSystem.Setup(fs => fs.ReadAllTextAsync(jsonPath)).ReturnsAsync(JsonSerializer.Serialize(translations));
            _fileSystem.Setup(fs => fs.FileExists(etagPath)).Returns(false);

            // Act
            var result = await _service.GetTranslations(language, false);

            // Assert
            result.FoundTranslationsInCache.Should().BeTrue();
            result.ETag.Should().BeNull();
        }

        #endregion
    }
}

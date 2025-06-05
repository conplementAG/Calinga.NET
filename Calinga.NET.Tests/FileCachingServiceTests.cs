using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Calinga.NET.Caching;
using Calinga.NET.Infrastructure;
using Calinga.NET.Infrastructure.Exceptions;
using Moq;
using Newtonsoft.Json;

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
            _fileSystem.Setup(fs => fs.ReadAllTextAsync(tempFilePath)).ReturnsAsync(JsonConvert.SerializeObject(translations));
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
            _fileSystem.Setup(fs => fs.ReadAllTextAsync(tempFilePath)).ReturnsAsync(JsonConvert.SerializeObject(translations));
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
            _fileSystem.Setup(fs => fs.ReadAllTextAsync(tempFilePath)).ReturnsAsync(JsonConvert.SerializeObject(translations));
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
            Assert.IsFalse(result.FoundInCache);
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
            _fileSystem.Setup(fs => fs.ReadAllTextAsync(path)).ReturnsAsync(JsonConvert.SerializeObject(translations));

            // Act
            var result = await _service.GetTranslations(language, false);

            // Assert
            Assert.IsTrue(result.FoundInCache);
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
            await Assert.ThrowsExceptionAsync<TranslationsNotAvailableException>(() => _service.GetTranslations(language, false));
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
            _fileSystem.Setup(fs => fs.ReadAllTextAsync(path)).ReturnsAsync(JsonConvert.SerializeObject(languages));

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
            await Assert.ThrowsExceptionAsync<TranslationsNotAvailableException>(() => _service.GetLanguages());
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
            _fileSystem.Setup(fs => fs.ReadAllTextAsync(tempFilePath)).ReturnsAsync(JsonConvert.SerializeObject(languages));
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
            await Assert.ThrowsExceptionAsync<ArgumentException>(() => _service.StoreTranslationsAsync(invalidLanguage, translations));
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
            await Assert.ThrowsExceptionAsync<Newtonsoft.Json.JsonReaderException>(() => _service.GetTranslations(language, false));
        }

        [TestMethod]
        public async Task GetLanguages_InvalidJson_ThrowsExceptionAndLogsWarning()
        {
            // Arrange
            var path = Path.Combine(_settings.CacheDirectory, _settings.Organization, _settings.Team, _settings.Project, "Languages.json");
            _fileSystem.Setup(fs => fs.FileExists(path)).Returns(true);
            _fileSystem.Setup(fs => fs.ReadAllTextAsync(path)).ReturnsAsync("{invalid json}");

            // Act & Assert
            await Assert.ThrowsExceptionAsync<Newtonsoft.Json.JsonReaderException>(() => _service.GetLanguages());
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
            _fileSystem.Setup(fs => fs.ReadAllTextAsync(tempFilePath)).ReturnsAsync(JsonConvert.SerializeObject(translations));
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
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => _service.StoreTranslationsAsync(language, translations));
        }

        [TestMethod]
        public async Task StoreLanguagesAsync_NullLanguageList_ThrowsArgumentNullException()
        {
            // Arrange
            IEnumerable<Language> languages = null;

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => _service.StoreLanguagesAsync(languages));
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
            Assert.IsTrue(result.FoundInCache);
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
            _fileSystem.Setup(fs => fs.ReadAllTextAsync(tempFilePath)).ReturnsAsync(JsonConvert.SerializeObject(translations));
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
            _fileSystem.Setup(fs => fs.ReadAllTextAsync(tempFilePath)).ReturnsAsync(JsonConvert.SerializeObject(languages));
            _fileSystem.Setup(fs => fs.FileExists(path)).Returns(false);
            _fileSystem.Setup(fs => fs.ReplaceFile(tempFilePath, path, null)).Throws<IOException>();
            _fileSystem.Setup(fs => fs.FileExists(tempFilePath)).Returns(true);

            // Act
            await _service.StoreLanguagesAsync(languages);

            // Assert
            _logger.Verify(l => l.Warn(It.IsAny<string>()), Times.AtLeastOnce);
            _fileSystem.Verify(fs => fs.DeleteFile(tempFilePath), Times.Once);
        }
    }
}

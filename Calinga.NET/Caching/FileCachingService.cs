using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Calinga.NET.Infrastructure;
using Calinga.NET.Infrastructure.Exceptions;
using Newtonsoft.Json;
using static System.FormattableString;

namespace Calinga.NET.Caching
{
    public class FileCachingService : ICachingService
    {
        private readonly string _filePath;
        private readonly string _languagesCacheFile = Invariant($"Languages.json");
        private readonly ILogger _logger;
        private readonly CalingaServiceSettings _settings;
        private readonly IFileSystem _fileSystem;

        public FileCachingService(CalingaServiceSettings settings, ILogger logger, IFileSystem? fileSystem = null)
        {
            _filePath = Path.Combine(settings.CacheDirectory, settings.Organization, settings.Team, settings.Project);
            _settings = settings;
            _logger = logger;
            _fileSystem = fileSystem ?? new FileSystem();
        }

        public async Task<CacheResponse> GetTranslations(string languageName, bool includeDrafts)
        {
            var path = Path.Combine(_filePath, GetFileName(languageName));

            if (!_fileSystem.FileExists(path))
                return CacheResponse.Empty;

            try
            {
                var fileContent = await _fileSystem.ReadAllTextAsync(path).ConfigureAwait(false);

                return new CacheResponse(JsonConvert.DeserializeObject<Dictionary<string, string>>(fileContent), true);
            }
            catch (IOException ex)
            {
                var message = Invariant($"The file could not be read: {ex.Message}, path: {path}");
                _logger.Warn(message);

                throw new TranslationsNotAvailableException(message, ex);
            }
        }

        public async Task<CachedLanguageListResponse> GetLanguages()
        {
            var path = Path.Combine(_filePath, _languagesCacheFile);

            if (_fileSystem.FileExists(path))
            {
                try
                {
                    var fileContent = await _fileSystem.ReadAllTextAsync(path).ConfigureAwait(false);

                    return new CachedLanguageListResponse(JsonConvert.DeserializeObject<List<Language>>(fileContent), true);
                }
                catch (IOException ex)
                {
                    var message = Invariant($"The file could not be read: {ex.Message}, path: {path}");
                    _logger.Warn(message);

                    throw new TranslationsNotAvailableException(message, ex);
                }
            }

            return CachedLanguageListResponse.Empty;
        }

        /// <summary>
        /// Clears the cache by deleting all files and directories in the cache directory.
        /// If `DoNotWriteCacheFiles` is set to true, the method completes without performing any action.
        /// </summary>
        public Task ClearCache()
        {
            if (_settings.DoNotWriteCacheFiles)
                return Task.CompletedTask;

            var directoryInfo = new DirectoryInfo(_filePath);
            DeleteDirectoryRecursively(directoryInfo);

            return Task.CompletedTask;
        }

        // Stores translations in a file. If `DoNotWriteCacheFiles` is true, the method returns without performing any action.
        // Creates a temporary file to store the translations and validates the JSON content.
        // If a previous version of the file exists, it is renamed before replacing it with the new file.
        // Logs warnings if JSON is invalid or if an IOException occurs.
        public async Task StoreTranslationsAsync(string language, IReadOnlyDictionary<string, string> translations)
        {
            if (_settings.DoNotWriteCacheFiles)
                return;

            var path = Path.Combine(_filePath, GetFileName(language));
            _fileSystem.CreateDirectory(_filePath);
            var tempFilePath = Path.Combine(_filePath, $"{Path.GetFileNameWithoutExtension(path)}.temp.json");

            try
            {
                await _fileSystem.WriteAllTextAsync(tempFilePath, JsonConvert.SerializeObject(translations)).ConfigureAwait(false);
                var tempFileContent = await _fileSystem.ReadAllTextAsync(tempFilePath).ConfigureAwait(false);
                JsonConvert.DeserializeObject<Dictionary<string, string>>(tempFileContent);

                if (_fileSystem.FileExists(path))
                {
                    var prevFilePath = Path.Combine(_filePath, $"{Path.GetFileNameWithoutExtension(path)}.prev.json");
                    _fileSystem.ReplaceFile(path, prevFilePath, null);
                    
                    _logger.Info($"Previous version of file {path} was renamed to {prevFilePath}");
                }

                _fileSystem.ReplaceFile(tempFilePath, path, null);
                
                _logger.Info($"Translations for language {language} stored in cache");
            }
            catch (JsonException ex)
            {
                _logger.Warn($"Invalid JSON in temp file: {ex.Message}");
                _fileSystem.DeleteFile(tempFilePath);
            }
            catch (IOException ex)
            {
                _logger.Warn(ex.Message);
            }
        }

        public async Task StoreLanguagesAsync(IEnumerable<Language> languageList)
        {
            if (_settings.DoNotWriteCacheFiles)
                return;

            var path = Path.Combine(_filePath, _languagesCacheFile);
            _fileSystem.CreateDirectory(_filePath);
            var tempFilePath = Path.Combine(_filePath, $"{Path.GetFileNameWithoutExtension(path)}.temp.json");

            try
            {
                await _fileSystem.WriteAllTextAsync(tempFilePath, JsonConvert.SerializeObject(languageList)).ConfigureAwait(false);
                var tempFileContent = await _fileSystem.ReadAllTextAsync(tempFilePath).ConfigureAwait(false);
                JsonConvert.DeserializeObject<List<Language>>(tempFileContent);

                if (_fileSystem.FileExists(path))
                {
                    var prevFilePath = Path.Combine(_filePath, $"{Path.GetFileNameWithoutExtension(path)}.prev.json");
                    _fileSystem.ReplaceFile(path, prevFilePath, null);
                }

                _fileSystem.ReplaceFile(tempFilePath, path, null);
            }
            catch (JsonException ex)
            {
                _logger.Warn($"Invalid JSON in temp file: {ex.Message}");
                _fileSystem.DeleteFile(tempFilePath);
            }
            catch (IOException ex)
            {
                _logger.Warn(ex.Message);
            }
        }

        private static string GetFileName(string language)
        {
            var sanitizedLanguage = System.Text.RegularExpressions.Regex.Replace(language, @"[^a-zA-Z0-9_\-~]", "");

            return Invariant($"{sanitizedLanguage}.json");
        }

        private void DeleteDirectoryRecursively(DirectoryInfo directory)
        {
            if (!directory.Exists)
                return;

            var files = directory.GetFiles();

            if (files.Length > 0)
            {
                foreach (var file in files)
                {
                    if (!IsFileLocked(file))
                    {
                        file.Delete();
                    }
                    else
                    {
                        RewriteLockedFile(file);
                    }
                }
            }

            var subDirectories = directory.GetDirectories();

            foreach (var directoryInfo in subDirectories)
            {
                DeleteDirectoryRecursively(directoryInfo);

                if (directoryInfo.GetFiles().Length == 0 && directoryInfo.GetDirectories().Length == 0)
                {
                    directoryInfo.Delete();
                }
            }
        }

        private static bool IsFileLocked(FileInfo file)
        {
            try
            {
                using var stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None);
                stream.Dispose();
            }
            catch (IOException)
            {
                // file is locked
                return true;
            }

            return false;
        }

        private void RewriteLockedFile(FileInfo file)
        {
            try
            {
                file.IsReadOnly = false;
                using var fs = new FileStream(file.FullName, FileMode.Create, FileAccess.Write);
                fs.Dispose();
            }
            catch (IOException ex)
            {
                _logger.Warn(ex.Message);
            }
        }
    }
}
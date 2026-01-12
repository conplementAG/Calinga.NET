using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
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
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _fileLocks = new ConcurrentDictionary<string, SemaphoreSlim>();
        private static readonly SemaphoreSlim _directoryLock = new SemaphoreSlim(1, 1);

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
                var dict = string.IsNullOrWhiteSpace(fileContent)
                    ? new Dictionary<string, string>()
                    : JsonConvert.DeserializeObject<Dictionary<string, string>>(fileContent) ?? new Dictionary<string, string>();
                
                return new CacheResponse(dict, true);
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

            if (!_fileSystem.FileExists(path))
                return CachedLanguageListResponse.Empty;
            
            try
            {
                var fileContent = await _fileSystem.ReadAllTextAsync(path).ConfigureAwait(false);
                var list = string.IsNullOrWhiteSpace(fileContent)
                    ? new List<Language>()
                    : JsonConvert.DeserializeObject<List<Language>>(fileContent) ?? new List<Language>();
                
                return new CachedLanguageListResponse(list, true);
            }
            catch (IOException ex)
            {
                var message = Invariant($"The file could not be read: {ex.Message}, path: {path}");
                _logger.Warn(message);

                throw new TranslationsNotAvailableException(message, ex);
            }

        }

        /// <summary>
        /// Clears the cache by deleting all files and directories in the cache directory.
        /// If `DoNotWriteCacheFiles` is set to true, the method completes without performing any action.
        /// </summary>
        public async Task ClearCache()
        {
            if (_settings.DoNotWriteCacheFiles)
                return;

            await _directoryLock.WaitAsync();
            try
            {
                var directoryInfo = new DirectoryInfo(_filePath);
                await DeleteDirectoryRecursivelyAsync(directoryInfo);
            }
            finally
            {
                _directoryLock.Release();
            }
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
            var tempFilePath = Path.Combine(_filePath, $"{Path.GetFileNameWithoutExtension(path)}.json.temp");
            SemaphoreSlim? fileLock = null;
            var fileLockAcquired = false;
            await _directoryLock.WaitAsync();
            try
            {
                _fileSystem.CreateDirectory(_filePath);
                fileLock = _fileLocks.GetOrAdd(path, _ => new SemaphoreSlim(1, 1));
                await fileLock.WaitAsync();
                fileLockAcquired = true;
                try
                {
                    await _fileSystem.WriteAllTextAsync(tempFilePath, JsonConvert.SerializeObject(translations)).ConfigureAwait(false);
                    var tempFileContent = await _fileSystem.ReadAllTextAsync(tempFilePath).ConfigureAwait(false);
                    JsonConvert.DeserializeObject<Dictionary<string, string>>(tempFileContent);

                    if (_fileSystem.FileExists(path))
                    {
                        var prevFilePath = Path.Combine(_filePath, $"{Path.GetFileNameWithoutExtension(path)}.json.prev");
                        _fileSystem.ReplaceFile(path, prevFilePath);
                        _logger.Info($"Previous version of file {path} was renamed to {prevFilePath}");
                    }

                    _fileSystem.ReplaceFile(tempFilePath, path);
                    _logger.Info($"Translations for language {language} stored in cache");
                }
                catch (JsonException ex)
                {
                    _logger.Warn($"Invalid JSON in temp file: {ex.Message}");
                }
                catch (IOException ex)
                {
                    _logger.Warn(ex.Message);
                }
            }
            finally
            {
                if (fileLockAcquired && fileLock != null)
                    fileLock.Release();
                
                _directoryLock.Release();
                
                if (_fileSystem.FileExists(tempFilePath))
                    _fileSystem.DeleteFile(tempFilePath);
            }
        }

        public async Task StoreLanguagesAsync(IEnumerable<Language> languageList)
        {
            if (_settings.DoNotWriteCacheFiles)
                return;

            var path = Path.Combine(_filePath, _languagesCacheFile);
            var tempFilePath = Path.Combine(_filePath, $"{Path.GetFileNameWithoutExtension(path)}.json.temp");

            await _directoryLock.WaitAsync();
            try
            {
                _fileSystem.CreateDirectory(_filePath);
                try
                {
                    await _fileSystem.WriteAllTextAsync(tempFilePath, JsonConvert.SerializeObject(languageList)).ConfigureAwait(false);
                    var tempFileContent = await _fileSystem.ReadAllTextAsync(tempFilePath).ConfigureAwait(false);
                    JsonConvert.DeserializeObject<List<Language>>(tempFileContent);

                    if (_fileSystem.FileExists(path))
                    {
                        var prevFilePath = Path.Combine(_filePath, $"{Path.GetFileNameWithoutExtension(path)}.json.prev");
                        _fileSystem.ReplaceFile(path, prevFilePath);
                    }

                    _fileSystem.ReplaceFile(tempFilePath, path);
                }
                catch (JsonException ex)
                {
                    _logger.Warn($"Invalid JSON in temp file: {ex.Message}");
                }
                catch (IOException ex)
                {
                    _logger.Warn(ex.Message);
                }
            }
            finally
            {
                _directoryLock.Release();

                if (_fileSystem.FileExists(tempFilePath))
                    _fileSystem.DeleteFile(tempFilePath);
            }
        }

        private static string GetFileName(string language)
        {
            if (language.Contains("..") || Path.IsPathRooted(language))
                throw new ArgumentException("Invalid language name or path: " + language);
            
            var sanitizedLanguage = System.Text.RegularExpressions.Regex.Replace(language, @"[^a-zA-Z0-9_\-~]", "").ToUpper();

            return Invariant($"{sanitizedLanguage}.json");
        }

        private async Task DeleteDirectoryRecursivelyAsync(DirectoryInfo directory)
        {
            try
            {
                if (!directory.Exists)
                    return;

                var files = directory.GetFiles();

                if (files.Length > 0)
                {
                    foreach (var file in files)
                    {
                        try
                        {
                            if (!IsFileLocked(file))
                            {
                                await Task.Run(() => file.Delete());
                            }
                            else
                            {
                                await Task.Run(() => RewriteLockedFile(file));
                            }
                        }
                        catch (DirectoryNotFoundException ex)
                        {
                            _logger.Warn($"Directory not found while deleting file: {ex.Message}");
                        }
                        catch (UnauthorizedAccessException ex)
                        {
                            _logger.Warn($"Unauthorized access while deleting file: {ex.Message}");
                        }
                        catch (IOException ex)
                        {
                            _logger.Warn($"IO error while deleting file: {ex.Message}");
                        }
                    }
                }

                var subDirectories = directory.GetDirectories();

                foreach (var directoryInfo in subDirectories)
                {
                    try
                    {
                        await DeleteDirectoryRecursivelyAsync(directoryInfo);

                        if (directoryInfo.GetFiles().Length == 0 && directoryInfo.GetDirectories().Length == 0)
                        {
                            await Task.Run(() => directoryInfo.Delete());
                        }
                    }
                    catch (DirectoryNotFoundException ex)
                    {
                        _logger.Warn($"Directory not found while deleting subdirectory: {ex.Message}");
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        _logger.Warn($"Unauthorized access while deleting subdirectory: {ex.Message}");
                    }
                    catch (IOException ex)
                    {
                        _logger.Warn($"IO error while deleting subdirectory: {ex.Message}");
                    }
                }
            }
            catch (DirectoryNotFoundException ex)
            {
                _logger.Warn($"Directory not found: {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.Warn($"Unauthorized access: {ex.Message}");
            }
            catch (IOException ex)
            {
                _logger.Warn($"IO error: {ex.Message}");
            }
        }

        private static bool IsFileLocked(FileInfo file)
        {
            try
            {
                using var stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None);
                return false;
            }
            catch (IOException)
            {
                return true;
            }
        }

        private void RewriteLockedFile(FileInfo file)
        {
            try
            {
                file.IsReadOnly = false;
                using var fs = new FileStream(file.FullName, FileMode.Create, FileAccess.Write);
            }
            catch (DirectoryNotFoundException ex)
            {
                _logger.Warn($"Directory not found while rewriting locked file: {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.Warn($"Unauthorized access while rewriting locked file: {ex.Message}");
            }
            catch (IOException ex)
            {
                _logger.Warn($"IO error while rewriting locked file: {ex.Message}");
            }
        }
    }
}


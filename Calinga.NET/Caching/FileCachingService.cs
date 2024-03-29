﻿using System.Collections.Generic;
using System.IO;
using System.Text;
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

        public FileCachingService(CalingaServiceSettings settings, ILogger logger)
        {
            _filePath = Path.Combine(settings.CacheDirectory, settings.Organization, settings.Team, settings.Project);
            _settings = settings;
            _logger = logger;
        }

        public async Task<CacheResponse> GetTranslations(string language, bool includeDrafts)
        {
            var path = Path.Combine(_filePath, GetFileName(language));

            if (File.Exists(path))
            {
                var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);

                try
                {
                    using (var streamReader = new StreamReader(fileStream, Encoding.UTF8))
                    {
                        var fileContent = await streamReader.ReadToEndAsync().ConfigureAwait(false);

                        return new CacheResponse(JsonConvert.DeserializeObject<Dictionary<string, string>>(fileContent), true);
                    }
                }

                catch (IOException ex)
                {
                    throw new TranslationsNotAvailableException(Invariant($"The file could not be read: {ex.Message}, path: {path}"), ex);
                }
            }

            return CacheResponse.Empty;
        }

        public async Task<CachedLanguageListResponse> GetLanguages()
        {
            var path = Path.Combine(_filePath, _languagesCacheFile);

            if (File.Exists(path))
            {
                var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);

                try
                {
                    using (var streamReader = new StreamReader(fileStream, Encoding.UTF8))
                    {
                        var fileContent = await streamReader.ReadToEndAsync().ConfigureAwait(false);

                        return new CachedLanguageListResponse(JsonConvert.DeserializeObject<List<Language>>(fileContent), true);
                    }
                }
                catch (IOException ex)
                {
                    throw new TranslationsNotAvailableException(Invariant($"The file could not be read: {ex.Message}, path: {path}"), ex);
                }
            }

            return CachedLanguageListResponse.Empty;
        }

        public Task ClearCache()
        {
            if (_settings.DoNotWriteCacheFiles)
                return Task.CompletedTask;

            var directoryInfo = new DirectoryInfo(_filePath);
            DeleteDirectoryRecursively(directoryInfo);

            return Task.CompletedTask;
        }

        public async Task StoreTranslationsAsync(string language, IReadOnlyDictionary<string, string> translations)
        {
            if (_settings.DoNotWriteCacheFiles)
                return;

            var path = Path.Combine(_filePath, GetFileName(language));

            Directory.CreateDirectory(_filePath);

            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                using var outputFile = new StreamWriter(new FileStream(path, FileMode.Create));
                await outputFile.WriteAsync(JsonConvert.SerializeObject(translations)).ConfigureAwait(false);
            }
            catch (IOException ex)
            {
                _logger.Warn(ex.Message);
            }
        }

        public async Task StoreLanguagesAsync(IEnumerable<Language> languagesList)
        {
            if (_settings.DoNotWriteCacheFiles)
                return;

            var path = Path.Combine(_filePath, _languagesCacheFile);

            Directory.CreateDirectory(_filePath);

            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                using var outputFile = new StreamWriter(new FileStream(path, FileMode.Create));
                await outputFile.WriteAsync(JsonConvert.SerializeObject(languagesList)).ConfigureAwait(false);
            }
            catch (IOException ex)
            {
                _logger.Warn(ex.Message);
            }
        }

        private static string GetFileName(string language)
        {
            return Invariant($"{language.ToUpper()}.json");
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
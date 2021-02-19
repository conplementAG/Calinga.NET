using System.Collections.Generic;
using System.Collections.ObjectModel;
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

        public FileCachingService(CalingaServiceSettings settings)
        {
            _filePath = Path.Combine(new[] { settings.CacheDirectory, settings.Organization, settings.Team, settings.Project });
        }

        public async Task<IReadOnlyDictionary<string, string>> GetTranslations(string language, bool includeDrafts)
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
                        return JsonConvert.DeserializeObject<Dictionary<string, string>>(fileContent);
                    }
                }

                catch (IOException ex)
                {
                    throw new TranslationsNotAvailableException(Invariant($"The file could not be read: {ex.Message}, path: {path}"), ex);
                }
            }

            return new ReadOnlyDictionary<string, string>(new Dictionary<string, string>());
        }

        public Task ClearCache()
        {
            var directoryInfo = new DirectoryInfo(_filePath);
            WalkDirectoryTree(directoryInfo);
            return Task.CompletedTask;
        }

        public async Task StoreTranslationsAsync(string language, IReadOnlyDictionary<string, string> translations)
        {
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
            catch (IOException)
            { }
        }

        private static string GetFileName(string language) => Invariant($"{language.ToUpper()}.json");

        private static void WalkDirectoryTree(DirectoryInfo directory)
        {
            if (!directory.Exists) return;
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
                WalkDirectoryTree(directoryInfo);

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
                using FileStream stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None);
                stream.Dispose();
            }
            catch (IOException)
            {
                // file is locked
                return true;
            }
            return false;
        }

        private static void RewriteLockedFile(FileInfo file)
        {
            try
            {
                file.IsReadOnly = false;
                using FileStream fs = new FileStream(file.FullName, FileMode.Create, FileAccess.Write);
                fs.Dispose();
            }
            catch (IOException)
            { }
        }
    }
}
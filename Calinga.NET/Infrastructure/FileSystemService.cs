using static System.FormattableString;

using System.IO;
using System.Text;
using System.Threading.Tasks;
using Calinga.NET.Infrastructure.Exceptions;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Calinga.NET.Infrastructure
{
    public class FileSystemService : IFileSystemService
    {
        private readonly string _filePath;

        public FileSystemService(CalingaServiceSettings settings)
        {
            _filePath = Path.Combine(new [] {settings.CacheDirectory, settings.Organization, settings.Team, settings.Project});
        }

        public async Task<IReadOnlyDictionary<string, string>> ReadCacheFileAsync(string language)
        {
            var path = Path.Combine(_filePath, GetFileName(language));
            if (!File.Exists(path)) throw new TranslationsNotAvailableException(Invariant($"File not found, path: {path}"));

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

        public async Task SaveTranslationsAsync(string language, IReadOnlyDictionary<string, string> translations)
        {
            var path = Path.Combine(_filePath, GetFileName(language));

            Directory.CreateDirectory(_filePath);
            if (File.Exists(path)) { File.Delete(path); }

            using (var outputFile = new StreamWriter(new FileStream(path, FileMode.Create)))
            {
                await outputFile.WriteAsync(JsonConvert.SerializeObject(translations)).ConfigureAwait(false);
            }
        }

        private static string GetFileName(string language) => Invariant($"{language.ToUpper()}.json");
    }
}
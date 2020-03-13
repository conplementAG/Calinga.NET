using static System.FormattableString;

using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Calinga.Infrastructure
{
    public class FileService : IFileService
    {
        private readonly string _filePath;

        public FileService(CalingaServiceSettings settings)
        {
            _filePath = Path.Combine(new [] {settings.CacheDirectory, settings.Project, settings.Version});
        }

        public async Task<string> GetJsonAsync(string language)
        {
            var path = Path.Combine(_filePath, GetFileName(language));
            if (!File.Exists(path)) throw new FileNotFoundException(Invariant($"File not found, path: {path}"));

            var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
            string text;
            try
            {
                using (var streamReader = new StreamReader(fileStream, Encoding.UTF8))
                {
                    text = await streamReader.ReadToEndAsync().ConfigureAwait(false);
                }
            }
            catch (IOException ex)
            {
                throw new IOException(Invariant($"The file could not be read:{ex.Message}, path: {path}"));
            }
            return text;
        }

        public async Task SaveTranslationsAsync(string language, string json)
        {
            var path = Path.Combine(_filePath, GetFileName(language));

            Directory.CreateDirectory(_filePath);
            if (File.Exists(path)) { File.Delete(path); }

            using (var outputFile = new StreamWriter(path))
            {
                await outputFile.WriteAsync(json).ConfigureAwait(false);
            }
        }

        private static string GetFileName(string language) => Invariant($"{language.ToUpper()}.json");
    }
}
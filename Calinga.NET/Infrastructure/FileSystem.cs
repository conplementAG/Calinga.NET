using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Calinga.NET.Infrastructure
{
    public class FileSystem : IFileSystem
    {
        public void CreateDirectory(string path)
        {
            Directory.CreateDirectory(path);
        }
        
        public async Task WriteAllTextAsync(string path, string contents)
        {
            using (var outputFile = new StreamWriter(new FileStream(path, FileMode.Create)))
            {
                await outputFile.WriteAsync(contents).ConfigureAwait(false);
            }
        }

        public async Task<string> ReadAllTextAsync(string path)
        {
            using (var tempFileStream = new FileStream(path, FileMode.Open, FileAccess.Read))
            using (var tempStreamReader = new StreamReader(tempFileStream, Encoding.UTF8))
            {
                return await tempStreamReader.ReadToEndAsync().ConfigureAwait(false);
            }
        }

        public bool FileExists(string path)
        {
            return File.Exists(path);
        }

        public void ReplaceFile(string sourceFileName, string destinationFileName, string? destinationBackupFileName = null)
        {
            if (!File.Exists(destinationFileName))
            {
                File.Copy(sourceFileName, destinationFileName, true);
                File.Delete(sourceFileName);
            }
            else
            {
                var backupFile = destinationBackupFileName 
                                 ?? destinationFileName + ".bak";
                File.Replace(sourceFileName, destinationFileName, backupFile, true);
            }
        }

        public void DeleteFile(string path)
        {
            File.Delete(path);
        }
        
        public void DeleteDirectory(string path)
        {
            Directory.Delete(path, true);
        }
    }
}
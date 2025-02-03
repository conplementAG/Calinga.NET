using System.Threading.Tasks;

namespace Calinga.NET.Infrastructure
{
    public interface IFileSystem
    {
        void CreateDirectory(string path);
        void DeleteDirectory(string path);
        Task WriteAllTextAsync(string path, string contents);
        Task<string> ReadAllTextAsync(string path);
        bool FileExists(string path);
        void ReplaceFile(string sourceFileName, string destinationFileName, string destinationBackupFileName);
        void DeleteFile(string path);
        void MoveFile(string sourceFileName, string destinationFileName);
        
    }
}
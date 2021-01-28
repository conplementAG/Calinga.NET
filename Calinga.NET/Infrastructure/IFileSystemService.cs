using System.Collections.Generic;
using System.Threading.Tasks;

namespace Calinga.NET.Infrastructure
{
    public interface IFileSystemService
    {
        Task SaveTranslationsAsync(string language, IReadOnlyDictionary<string, string> translations);

        Task<IReadOnlyDictionary<string, string>> ReadCacheFileAsync(string language);

        void DeleteFiles();
    }
}
using System.Threading.Tasks;

namespace Calinga.NET.Infrastructure
{
    public interface IFileService
    {
        Task SaveTranslationsAsync(string language, string json);

        Task<string> GetJsonAsync(string language);
    }
}

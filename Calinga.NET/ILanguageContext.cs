using System.Threading.Tasks;

namespace Calinga.NET
{
    public interface ILanguageContext
    {
        Task<string> TranslateAsync(string key);
    }
}
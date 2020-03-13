using System.Threading.Tasks;

namespace Calinga.NET
{
    public class LanguageContext : ILanguageContext
    {
        private readonly ICalingaService _service;
        private readonly string _language;

        public LanguageContext(string language, ICalingaService service)
        {
            Guard.IsNotNullOrWhiteSpace(language);

            _language = language;
            _service = service;
        }

        public Task<string> TranslateAsync(string key)
        {
            Guard.IsNotNullOrWhiteSpace(key);
            return _service.TranslateAsync(key, _language);
        }
    }
}
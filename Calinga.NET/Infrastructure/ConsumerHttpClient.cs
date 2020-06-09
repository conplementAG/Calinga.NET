using static System.FormattableString;

using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Calinga.NET.Infrastructure.Exceptions;
using Newtonsoft.Json;

namespace Calinga.NET.Infrastructure
{
    public class ConsumerHttpClient : IConsumerHttpClient
    {
        private const string ConsumerBaseUrl = "https://api.calinga.io/v3";

        private readonly CalingaServiceSettings _settings;

        public ConsumerHttpClient(CalingaServiceSettings settings)
        {
            _settings = settings;
        }

        public async Task<IReadOnlyDictionary<string, string>> GetTranslationsAsync(string language)
        {
            try
            {
                using (var client = CreateHttpClient())
                {
                    var url = Invariant($"{ConsumerBaseUrl}/{_settings.Organization}/{_settings.Team}/{_settings.Project}/languages/{language}");

                    var responseBody = await GetResponseBody(client, url).ConfigureAwait(false);
                    return CreateTranslationsDictionary(responseBody);
                }
            }
            catch (HttpRequestException ex)
            {
               throw new TranslationsNotAvailableException("Failed to fetch translations", ex);
            }
        }

        public async Task<IEnumerable<string>> GetLanguagesAsync()
        {
            try
            {
                using (var client = CreateHttpClient())
                {
                    var url = Invariant($"{ConsumerBaseUrl}/{_settings.Organization}/{_settings.Team}/{_settings.Project}/languages");
                    var responseBody = await GetResponseBody(client, url).ConfigureAwait(false);

                    return MapGetLanguagesResult(responseBody);
                }
            }
            catch (HttpRequestException ex)
            {
                throw new LanguagesNotAvailableException("Failed to fetch languages", ex);
            }
        }

        #region private static Methods

        private static async Task<string> GetResponseBody(HttpClient client, string url)
        {
            var response = await client.GetAsync(url).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        }

        private static Dictionary<string, string> CreateTranslationsDictionary(string json)
        {
            return JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
        }

        private static IEnumerable<string> MapGetLanguagesResult(string json)
        {
            return JsonConvert.DeserializeObject<Dictionary<string, bool>>(json).Select(l=>l.Key);
        }

        private HttpClient CreateHttpClient()
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("api-token", _settings.ApiToken);

            return httpClient;
        }

        #endregion private static Methods
    }
}
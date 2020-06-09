namespace Calinga.NET.Infrastructure
{
    public class CalingaServiceSettings
    {
        public string Organization { get; set; }

        public string Team { get; set; }

        public string Project { get; set; }

        public string ApiToken { get; set; }

        public bool IsDevMode { get; set; }

        public string CacheDirectory { get; set; }
    }
}

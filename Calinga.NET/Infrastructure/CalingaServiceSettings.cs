namespace Calinga.NET.Infrastructure
{
    public class CalingaServiceSettings
    {
        public string Tenant { get; set; }

        public string Project { get; set; }

        public string ApiToken { get; set; }

        public string Version { get; set; }

        public bool IsDevMode { get; set; }

        public string CacheDirectory { get; set; }
    }
}

namespace Calinga.NET.Infrastructure
{
    public class CalingaServiceSettings
    {
        public string Organization { get; set; }

        public string Team { get; set; }

        public string Project { get; set; }

        public string ApiToken { get; set; }

        public bool IncludeDrafts { get; set; }

        public bool IsDevMode { get; set; }
        
        /// <summary>
        /// Gets or sets the cache file system directory. Setting only required for default caching
        /// </summary>
        public string CacheDirectory { get; set; }

        public string ConsumerApiBaseUrl { get; set; } = "https://api.calinga.io/v3";

        public uint MemoryCacheExpirationIntervalInSeconds { get; set; }
    }
}

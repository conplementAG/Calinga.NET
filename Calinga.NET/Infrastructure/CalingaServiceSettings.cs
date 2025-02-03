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

        public bool DoNotWriteCacheFiles { get; set; }
        
        /// <summary>
        /// Gets or sets a value indicating whether the system should only fetch files from the cache and not download them from the internet.
        /// </summary>
        public bool UseCacheOnly { get; set; }
        
        /// <summary>
        /// Gets or sets a value indicating whether the system should fallback to the reference language if an error occurs or the requested language could not be found.
        /// </summary>
        public bool FallbackToReferenceLanguage { get; set; }
    }
}
namespace Calinga.NET.Infrastructure
{
    public class CalingaServiceSettings
    {
        public string Organization { get; set; } = string.Empty;

        public string Team { get; set; } = string.Empty;

        public string Project { get; set; } = string.Empty;

        public string ApiToken { get; set; } = string.Empty;

        public bool IncludeDrafts { get; set; }

        /// <summary>
        /// When true, translation lookups still hit the server (or cache) to determine which keys exist,
        /// but the returned values are the keys themselves rather than the translations. Use during UI
        /// development to verify which translation key renders where without depending on translated content.
        /// For the keyed <c>GetTranslationsAsync</c> overload, the server response is also validated:
        /// if any requested key is missing on the server, a <see cref="Exceptions.KeysNotFoundException"/>
        /// is thrown listing the missing keys, so typos and unknown keys surface at integration time
        /// rather than as silent omissions at runtime.
        /// </summary>
        public bool IsDevMode { get; set; }

        /// <summary>
        /// Gets or sets the cache file system directory. Setting only required for default caching
        /// </summary>
        public string CacheDirectory { get; set; } = string.Empty;

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
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Calinga.NET.Caching
{
    public class CacheResponse
    {
        public CacheResponse(IReadOnlyDictionary<string, string> result, bool foundTranslationsInCache, string? etag = null, bool isStale = false)
        {
            Result = result;
            FoundTranslationsInCache = foundTranslationsInCache;
            ETag = etag;
            IsStale = isStale;
        }

        public IReadOnlyDictionary<string, string> Result { get; }

        public bool FoundTranslationsInCache { get; }

        public string? ETag { get; }

        /// <summary>
        /// True when the entry was found but its in-memory expiration has elapsed.
        /// Data and ETag remain readable so the caller can revalidate via If-None-Match.
        /// </summary>
        public bool IsStale { get; }

        public static CacheResponse Empty => new CacheResponse(new ReadOnlyDictionary<string, string>(new Dictionary<string, string>()), false);
    }
}
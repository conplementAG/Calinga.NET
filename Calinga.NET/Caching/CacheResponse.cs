using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Calinga.NET.Caching
{
    public class CacheResponse
    {
        public CacheResponse(IReadOnlyDictionary<string, string> result, bool foundInCache)
        {
            Result = result;
            FoundInCache = foundInCache;
        }

        public IReadOnlyDictionary<string, string> Result { get; }
        public bool FoundInCache { get; }

        public static CacheResponse Empty => new CacheResponse(new ReadOnlyDictionary<string, string>(new Dictionary<string, string>()), false);
    }
}
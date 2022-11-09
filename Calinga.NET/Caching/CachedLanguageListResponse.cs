using System.Collections.Generic;

namespace Calinga.NET.Caching
{
    public class CachedLanguageListResponse
    {
        public IReadOnlyList<string> Result { get; }
        public bool FoundInCache { get; }

        public static CachedLanguageListResponse Empty => new CachedLanguageListResponse(new List<string>(), false);

        public CachedLanguageListResponse(IReadOnlyList<string> result, bool foundInCache)
        {
            Result = result;
            FoundInCache = foundInCache;
        }
    }
}
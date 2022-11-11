using System.Collections.Generic;

namespace Calinga.NET.Caching
{
    public class CachedLanguageListResponse
    {
        public IReadOnlyList<Language> Result { get; }
        public bool FoundInCache { get; }

        public static CachedLanguageListResponse Empty => new CachedLanguageListResponse(new List<Language>(), false);

        public CachedLanguageListResponse(IReadOnlyList<Language> result, bool foundInCache)
        {
            Result = result;
            FoundInCache = foundInCache;
        }
    }
}
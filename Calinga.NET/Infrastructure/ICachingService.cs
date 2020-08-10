﻿using System.Collections.Generic;

using System.Threading.Tasks;

namespace Calinga.NET.Infrastructure
{
    public interface ICachingService
    {
        Task<IReadOnlyDictionary<string, string>> GetTranslations(string language, bool includeDrafts);

        Task<IEnumerable<string>> GetLanguages();

        void ClearCache();
    }
}

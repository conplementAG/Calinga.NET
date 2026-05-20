using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Calinga.NET.Infrastructure
{
    public sealed class TranslationsHttpResponse
    {
        public TranslationsHttpResponse(IReadOnlyDictionary<string, string> translations, string? etag, bool notModified)
        {
            Translations = translations;
            ETag = etag;
            NotModified = notModified;
        }

        public IReadOnlyDictionary<string, string> Translations { get; }

        public string? ETag { get; }

        public bool NotModified { get; }

        public static TranslationsHttpResponse NotModifiedResponse(string? etag) =>
            new TranslationsHttpResponse(EmptyTranslations, etag, true);

        private static readonly IReadOnlyDictionary<string, string> EmptyTranslations =
            new ReadOnlyDictionary<string, string>(new Dictionary<string, string>());
    }
}

# Calinga.NET

[![NuGet](https://img.shields.io/nuget/v/Calinga.Net)](https://www.nuget.org/packages/Calinga.NET/)

Package to connect and use the calinga service in .NET applications

## General usage

1. Install the `Calinga.NET` nuget package
2. Create and populate an instance of `CalingaServiceSettings`
3. Instantiate `CalingaService` with your settings from 2.

## ASP.NET Core integration

1. Install the `Calinga.NET` nuget package
2. Extend your `appsettings.json` with:

```json
      "CalingaServiceSettings": {
            "Organization": <YOUR_ORGANIZATION>,
            "Team": <YOUR_TEAM>,
            "Project": <YOUR_PROJECT>,
            "ApiToken": <YOUR_TOKEN>,
            "IsDevMode": false,
            "IncludeDrafts": false,
            "CacheDirectory":  "CacheFiles", # Only needed for default caching implementation,
            "MemoryCacheExpirationIntervalInSeconds": <YOUR_CACHE_EXPIRATION_INTERVAL_IN_SECONDS>, # Only needed for default caching implementation,
            "DoNotWriteCacheFiles": false, # Only needed for default caching implementation
            "UseCacheOnly": false, # Only needed for default caching implementation
            "FallbackToReferenceLanguage": false
          }
```


- `Organization`: The name of your organization.
- `Team`: The name of your team.
- `Project`: The name of your project.
- `ApiToken`: The API token used for authentication.
- `IsDevMode`: A boolean indicating if the service is in development mode. When `true`, it returns keys instead of actual translations.
- `IncludeDrafts`: A boolean indicating if draft translations should be included.
- `CacheDirectory`: The directory where cache files are stored. Only needed for the default caching implementation.
- `MemoryCacheExpirationIntervalInSeconds`: The expiration interval for the in-memory cache in seconds. Only needed for the default caching implementation.
- `DoNotWriteCacheFiles`: A boolean indicating if cache files should not be written to the filesystem. Only needed for the default caching implementation.
- `UseCacheOnly`: A boolean indicating if the system should only fetch translations from the cache and not from the internet. Only needed for the default caching implementation.
- `FallbackToReferenceLanguage`: A boolean indicating if the system should fallback to the reference language if an error occurs or the requested language could not be found.

3. Add the following to your `Startup.ConfigureServices` method:

```csharp
    services.AddSingleton<ICalingaService>(ctx =>
        {
            var settings = new CalingaServiceSettings();
            Configuration.GetSection(nameof(CalingaServiceSettings)).Bind(settings);
            return new CalingaService(settings);
        });
```

## Custom Caching

Calinga uses out of the box in memory caching with a fallback to optional filesystem cache. You can override ICachingService with the implementation of your choice.

To enable the **optional filesystem caching**, the variable `DoNotWriteCacheFiles` in the configuration has to be set to `true`. When `DoNotWriteCacheFiles` is set to `false`, a copy of the entire language strings will be saved on disk at the `CacheDirectory` from the configuration, this copy will remain in use until `ClearCache()` is called.

When a translation for a string is called, Calinga.Net will check if the Key exists in its In-Memory cache, and that the In-memory cache has not yet expired, then return the translation value. If the In-memory cache was expired, it will continue to the following source, the cache stored in the filesystem and return the value. If the key was not found in the filesystem cache, Calinga.Net will send a request to Calinga API to get a new translation.

If the filesystem cache is used, you will have to manually update it by calling `ClearCache()` whenever it suits your use case, in order to discard the old json files and reload a fresh copy from the following Calinga API Call.

If the `DoNotWriteCacheFiles` was set to `true`, then once the cache expires, Calinga.Net will fetch the translations again from the Calinga API without looking in the local cache.

## Custom HttpClient

If you need to set additional network options (proxy configuration, customized encryption, etc.) pass a pre-configured `HttpClient` to `CalingaService`.

Now the CalingaService is ready to be used in your application.
More examples can be found [here](https://github.com/conplementAG/calinga-dotnet-demo).

## Language Tags

To fetch translations for languages with language tag you must provide the language and tag in the following format:

`<language code>~<language tag>`

e.g. `de-AT~Intranet`.

Calls to `GetLanguagesAsync()` will also return languages in this format.

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
            "CacheDirectory":  "CacheFiles" # Only needed for default caching implementation,
            "MemoryCacheExpirationIntervalInSeconds": <YOUR_CACHE_EXPIRATION_INTERVAL_IN_SECONDS> # Only needed for default caching implementation,
            "DoNotWriteCacheFiles": false # Only needed for default caching implementation
          }
```

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

Calinga uses out of the box in memory caching with a fallback to filesystem cache. You can override ICachingService with the implementation of your choice.

Languages used as well as a list of all available languages, and reference language are cached in memory and in the `CacheDirectory` provided in Settings.

## Custom HttpClient

If you need to set additional network options (proxy configuration, customized encryption, etc.) pass a pre-configured `HttpClient` to `CalingaService`.

Now the CalingaService is ready to be used in your application.
More examples can be found [here](https://github.com/conplementAG/calinga-dotnet-demo).

## Language Tags

To fetch translations for languages with language tag you must provide the language and tag in the following format:

`<language code>~<language tag>`

e.g. `de-AT~Intranet`.

Calls to `GetLanguagesAsync()` will also return languages in this format.

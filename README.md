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
            "CacheDirectory":  "CacheFiles"
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
 
Now the CalingaService is ready to be used in your application.
More examples can be found [here](https://github.com/conplementAG/calinga-dotnet-demo).

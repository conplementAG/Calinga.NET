using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Calinga.NET.Infrastructure;
using Calinga.NET.Infrastructure.Exceptions;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;

namespace Calinga.NET.Tests.Context
{
    public class TestContext
    {
        private readonly Dictionary<string, TranslationsRepository> _repositories = new Dictionary<string, TranslationsRepository>();

        public CalingaServiceSettings Settings { get; }
        public Exception LastException { get; private set; }
        public object LastResult { get; private set; }
        private ICalingaService _service;

        public ICalingaService Service => _service ??= BuildCalingaService();

        public TranslationsRepository this[string repository] => _repositories[repository];

        public TestContext()
        {
            Settings = new CalingaServiceSettings
            {
                CacheDirectory = AppDomain.CurrentDomain.BaseDirectory ?? string.Empty,
                Organization = Guid.NewGuid().ToString(),
                Team = Guid.NewGuid().ToString(),
                Project = Guid.NewGuid().ToString()
            };

            _repositories.Add("Calinga", new TranslationsRepository());
            _repositories.Add("Cache", new TranslationsRepository());
        }

        public async Task Try<T>(Func<Task<T>> action)
        {
            try
            {
                LastResult = await action().ConfigureAwait(false);
                LastException = null;
            }
            catch (Exception e)
            {
                LastException = e;
            }
        }

        private ICalingaService BuildCalingaService()
        {
            var httpClient = BuildHttpClientMock();

            var fileService = BuildFileServiceMock();

            var cachingService = new CachingService(fileService.Object);
            var consumerHttpClient = new ConsumerHttpClient(Settings, httpClient);

            return new CalingaService(cachingService, consumerHttpClient, Settings);
        }

        private Mock<IFileSystemService> BuildFileServiceMock()
        {
            var fileService = new Mock<IFileSystemService>();
            fileService.Setup(x => x.ReadCacheFileAsync(It.IsAny<string>())).ReturnsAsync(
                (string languageName) =>
                {
                    if (
                        !this["Cache"].Organizations.ContainsKey(Settings.Organization) ||
                        !this["Cache"].Organizations[Settings.Organization]
                            .ContainsKey(Settings.Team) ||
                        !this["Cache"].Organizations[Settings.Organization][
                            this.Settings.Team].ContainsKey(Settings.Project))
                    {
                        throw new TranslationsNotAvailableException(
                            $"File not found, path: {Settings.Organization}, {Settings.Team}, {Settings.Project}, {languageName}");
                    }

                    return this["Cache"].Organizations[Settings.Organization][
                        Settings.Team][
                        Settings.Project][languageName];
                });

            fileService.Setup(f => f.DeleteFiles()).Callback(() =>
            {
                this["Cache"].Organizations.Clear();
            });
            return fileService;
        }

        private HttpClient BuildHttpClientMock()
        {
            var messageHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            messageHandler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(
                (HttpRequestMessage request, CancellationToken _) =>
                {
                    if (request.Method == HttpMethod.Get)
                    {
                        var segments = request.RequestUri.Segments.Select(s => s.Trim('/'))
                            .Select(HttpUtility.UrlDecode).ToArray();
                        var organizationName = segments[2];
                        var teamName = segments[3];
                        var projectName = segments[4];
                        var languageName = segments[6];
                        try
                        {
                            var translations =
                                this["Calinga"].Organizations
                                    [organizationName][teamName][projectName][languageName];
                            return new HttpResponseMessage
                            {
                                StatusCode = HttpStatusCode.OK,
                                Content = new StringContent(JsonConvert.SerializeObject(translations))
                            };
                        }
                        catch (Exception)
                        {
                            return new HttpResponseMessage
                            {
                                StatusCode = HttpStatusCode.NotFound, Content = new StringContent("not found")
                            };
                        }
                    }

                    return new HttpResponseMessage
                        {StatusCode = HttpStatusCode.NotImplemented, Content = new StringContent("{}")};
                });
            var httpClient = new HttpClient(messageHandler.Object);
            return httpClient;
        }
    }
}
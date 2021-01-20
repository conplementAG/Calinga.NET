using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Policy;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Calinga.NET.Infrastructure;
using Calinga.NET.Infrastructure.Exceptions;
using Calinga.NET.Tests.Context;
using FluentAssertions;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using NSubstitute.ExceptionExtensions;
using TechTalk.SpecFlow;

namespace Calinga.NET.Tests
{
    [Binding]
    public class CalingaNetSteps
    {
        private readonly TestContext _testContext;

        public CalingaNetSteps(TestContext testContext)
        {
            _testContext = testContext;
        }

        [Given(@"Calinga\.Net is configured for organization ""(.*)""")]
        public void GivenCalinga_NetIsConfiguredForOrganization(string organizationName)
        {
            _testContext.Settings.Organization = organizationName;
        }

        [Given(@"Calinga\.Net is configured for team ""(.*)""")]
        public void GivenCalinga_NetIsConfiguredForTeam(string teamName)
        {
            _testContext.Settings.Team = teamName;
        }

        [Given(@"Calinga\.Net is configured for project ""(.*)""")]
        public void GivenCalinga_NetIsConfiguredForProject(string projectName)
        {
            _testContext.Settings.Project = projectName;
        }

        [When(@"the user requests ""(.*)"" translation for key ""(.*)""")]
        public async Task WhenTheUserRequestsTranslationForKey(string language, string key)
        {
            await _testContext.Try(() => _testContext.Service.TranslateAsync(key, language)).ConfigureAwait(false);
        }

        [When(@"the user clears the cache")]
        public void WhenTheUserClearsTheCache()
        {
            _testContext.Service.ClearCache();
        }

        [Then(@"Calinga\.Net throws an exception telling the user that project ""(.*)"" is unknown")]
        public void ThenCalinga_NetThrowsAnExceptionTellingTheUserThatProjectIsUnknown(string projectName)
        {
            _testContext.LastException.Should().NotBeNull();
            _testContext.LastException.Message.Should().Contain(projectName);
        }

        [Then(@"Calinga\.Net throws an exception telling the user that team ""(.*)"" is unknown")]
        public void ThenCalinga_NetThrowsAnExceptionTellingTheUserThatTeamIsUnknown(string teamName)
        {
            _testContext.LastException.Should().NotBeNull();
            _testContext.LastException.Message.Should().Contain(teamName);
        }

        [Then(@"Calinga\.Net throws an exception telling the user that organization ""(.*)"" is unknown")]
        public void ThenCalinga_NetThrowsAnExceptionTellingTheUserThatOrganizationIsUnknown(string organizationName)
        {
            _testContext.LastException.Should().NotBeNull();
            _testContext.LastException.Message.Should().Contain(organizationName);
        }

        [Then(@"Calinga\.Net throws an exception telling the user that language ""(.*)"" is unknown in project ""(.*)""")]
        public void ThenCalinga_NetThrowsAnExceptionTellingTheUserThatLanguageIsUnknownInProject(string languageName, string projectName)
        {
            _testContext.LastException.Should().NotBeNull();
            _testContext.LastException.Message.Should().Contain(languageName);
            _testContext.LastException.Message.Should().Contain(projectName);
        }

        [Then(@"Calinga\.Net returns translation ""(.*)""")]
        public void ThenCalinga_NetReturnsTranslationForKey(string translation)
        {
            _testContext.LastResult.Should().Be(translation);
        }
    }
}

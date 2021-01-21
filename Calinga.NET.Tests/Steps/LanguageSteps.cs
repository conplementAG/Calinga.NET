using System.Collections.Generic;
using System.Linq;
using Calinga.NET.Tests.Context;
using FluentAssertions;
using TechTalk.SpecFlow;

namespace Calinga.NET.Tests.Steps
{
    [Binding]
    public class LanguageSteps
    {
        private readonly TestContext _testContext;

        public LanguageSteps(TestContext testContext)
        {
            _testContext = testContext;
        }

        [Given(@"in ([^ ]*) ""(.*)"" has no language ""(.*)""")]
        public void GivenThereLanguageHasNotBeenAddedToProject(string repository, string projectName, string languageName)
        {
            _testContext[repository].Organizations[_testContext[repository].LatestOrganizationName][_testContext[repository].LatestTeamName][projectName].Keys
                .Select(k => k).Count(l => l == languageName).Should().Be(0);
        }

        [Given(@"in ([^ ]*) ""(.*)"" has language ""(.*)""")]
        public void GivenProjectHasLanguage(string repository, string projectName, string languageName)
        {
            _testContext[repository].LatestTeam[projectName].Add(languageName, new Dictionary<string, string>());
            if (_testContext[repository].LatestTeam[projectName].Count > 0)
            {
                var languageToCopyKeysFrom = _testContext[repository].LatestTeam[projectName].Keys.First();
                foreach (var key in _testContext[repository].LatestTeam[projectName][languageToCopyKeysFrom].Keys)
                {
                    _testContext[repository].LatestTeam[projectName][languageName].Add(key, string.Empty);
                }
            }
        }
    }
}

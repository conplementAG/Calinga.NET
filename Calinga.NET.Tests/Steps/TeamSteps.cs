using System.Linq;
using Calinga.NET.Tests.Context;
using FluentAssertions;
using TechTalk.SpecFlow;

namespace Calinga.NET.Tests.Steps
{
    [Binding]
    public class TeamSteps
    {
        private readonly TestContext _testContext;

        public TeamSteps(TestContext testContext)
        {
            _testContext = testContext;
        }

        [Given(@"in ([^ ]*) ""(.*)"" has team ""(.*)""")]
        public void GivenHasTeamInCalinga(string repository, string organizationName, string teamName)
        {
            _testContext[repository].AddTeam(teamName, organizationName);
        }

        [Given(@"in ([^ ]*) organization ""(.*)"" has no team ""(.*)""")]
        public void GivenThereIsNoTeamInCalinga(string repository, string organization, string teamName)
        {
            _testContext[repository].Organizations[organization].Values.SelectMany( team => team.Keys).Count(name => name == teamName).Should().Be(0);
        }
    }
}

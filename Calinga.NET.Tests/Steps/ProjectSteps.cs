using System.Linq;
using FluentAssertions;
using TechTalk.SpecFlow;
using TestContext = Calinga.NET.Tests.Context.TestContext;

namespace Calinga.NET.Tests.Steps
{
    [Binding]
    public class ProjectSteps
    {
        private readonly TestContext _testContext;

        public ProjectSteps(TestContext testContext)
        {
            _testContext = testContext;
        }

        [Given(@"in ([^ ]*) ""(.*)"" has project ""(.*)""")]
        public void GivenHasProjectInCalinga(string repository, string teamName, string projectName)
        {
            _testContext[repository].AddProject(projectName, teamName);
        }

        [Given(@"in ([^ ]*) team ""(.*)"" has no project ""(.*)""")]
        public void GivenThereIsNoProjectInCalinga(string repository, string teamName, string projectName)
        {
            _testContext[repository].LatestOrganization[teamName].Values.SelectMany( t => t.Keys).Count(k => k == projectName).Should().Be(0);
        }
    }
}

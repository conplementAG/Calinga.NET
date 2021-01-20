using System.Linq;
using Calinga.NET.Tests.Context;
using FluentAssertions;
using TechTalk.SpecFlow;

namespace Calinga.NET.Tests.Steps
{
    [Binding]
    public class OrganizationSteps
    {
        private readonly TestContext _testContext;

        public OrganizationSteps(TestContext testContext)
        {
            _testContext = testContext;
        }

        [Given(@"in ([^ ]*) there is an organization ""(.*)""")]
        public void GivenThereIsAnOrganizationInCalinga(string repository, string organizationName)
        {
            _testContext[repository].AddOrganization(organizationName);
        }

        [Given(@"in ([^ ]*) there is no organization ""(.*)""")]
        public void GivenThereIsNoOrganizationInCalinga(string repository, string organizationName)
        {
            _testContext[repository].Organizations.Keys.Select(k => k).Count(name => name == organizationName).Should().Be(0);
        }
    }
}

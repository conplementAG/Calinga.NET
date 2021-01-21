using TechTalk.SpecFlow;
using TestContext = Calinga.NET.Tests.Context.TestContext;

namespace Calinga.NET.Tests.Steps
{
    [Binding]
    public class KeySteps
    {
        private readonly TestContext _testContext;

        public KeySteps(TestContext testContext)
        {
            _testContext = testContext;
        }

        [Given(@"in ([^ ]*) ""(.*)"" has key ""(.*)""")]
        public void GivenProjectHasKey(string repository, string projectName, string keyName)
        {
            foreach (var languageTranslation in _testContext[repository].LatestTeam[projectName].Values)
            {
                languageTranslation.Add(keyName, string.Empty);
            }
        }
    }
}

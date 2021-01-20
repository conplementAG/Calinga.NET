using TechTalk.SpecFlow;
using TestContext = Calinga.NET.Tests.Context.TestContext;

namespace Calinga.NET.Tests
{
    [Binding]
    public class TranslationSteps
    {
        private readonly TestContext _testContext;

        public TranslationSteps(TestContext testContext)
        {
            _testContext = testContext;
        }

        [Given(@"in ([^ ]*) the ""(.*)"" translation of key ""(.*)"" is ""(.*)""")]
        public void GivenInCacheTheTranslationOfKeyIs(string repository, string languageName, string keyName, string translation)
        {
            _testContext[repository].LatestProject[languageName][keyName] = translation;
        }
    }
}

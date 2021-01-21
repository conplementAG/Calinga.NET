using System.Collections.Generic;

namespace Calinga.NET.Tests.Context
{
    public class TranslationsRepository
    {
        public Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, string>>>>> Organizations { get; }
        public string LatestOrganizationName { get; private set; }
        public string LatestTeamName { get; private set; }
        public string LatestProjectName { get; private set; }

        public Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, string>>>>
            LatestOrganization => Organizations[LatestOrganizationName];

        public Dictionary<string, Dictionary<string, Dictionary<string, string>>>
            LatestTeam => Organizations[LatestOrganizationName][LatestTeamName];

        public Dictionary<string, Dictionary<string, string>>
            LatestProject => Organizations[LatestOrganizationName][LatestTeamName][LatestProjectName];

        public TranslationsRepository()
        {
            Organizations = new Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, string>>>>>();
        }

        public void AddOrganization(string organizationName)
        {
            Organizations.Add(organizationName, new Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, string>>>>());
            LatestOrganizationName = organizationName;
        }

        public void AddTeam(string teamName, string organizationName)
        {
            Organizations[organizationName].Add(teamName, new Dictionary<string, Dictionary<string, Dictionary<string, string>>>());
            LatestTeamName = teamName;
        }

        public void AddProject(string projectName, string teamName)
        {
            LatestOrganization[teamName].Add(projectName, new Dictionary<string, Dictionary<string, string>>());
            LatestProjectName = projectName;
        }

        public void AddLanguage(string languageName, string projectName)
        {
            LatestTeam[projectName].Add(languageName, new Dictionary<string, string>());
        }

        public void AddTranslation(string languageName, string keyName, string translation)
        {
            LatestProject[languageName].Add(keyName, translation);
        }
    }
}
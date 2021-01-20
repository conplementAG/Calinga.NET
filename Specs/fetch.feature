Feature: Fetch Translations

    Background:
        Given Calinga.Net is configured for organization "Bob's Company"
        And Calinga.Net is configured for team "App Team"
        And Calinga.Net is configured for project "Registration App"

    Scenario: Error if project unknown
        Given in Calinga there is an organization "Bob's Company"
        And in Calinga "Bob's Company" has team "App Team"
        But in Calinga team "App Team" has no project "Registration App"
        When the user requests "en" translation for key "title"
        Then Calinga.Net throws an exception telling the user that project "Registration App" is unknown

    Scenario: Error if team unknown
        Given in Calinga there is an organization "Bob's Company"
        But in Calinga organization "Bob's Company" has no team "App Team"
        When the user requests "en" translation for key "title"
        Then Calinga.Net throws an exception telling the user that team "App Team" is unknown

    Scenario: Error if organization unknown
        Given in Calinga there is no organization "Bob's Company"
        When the user requests "en" translation for key "title"
        Then Calinga.Net throws an exception telling the user that organization "Bob's Company" is unknown

    Scenario: Error if language not added to project
        Given in Calinga there is an organization "Bob's Company"
        And in Calinga "Bob's Company" has team "App Team"
        And in Calinga "App Team" has project "Registration App"
        But in Calinga "Registration App" has no language "de"
        When the user requests "de" translation for key "title"
        Then Calinga.Net throws an exception telling the user that language "de" is unknown in project "Registration App"

    Scenario: Return key name if language translations not cached and connection to calinga failed
        Given conne
#
#    Scenario: Return translation from cache if translations have not changed on server
#
#    Scenario: Error if language tag not added to team
#
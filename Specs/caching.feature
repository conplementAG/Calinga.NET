Feature: Translation Caching

    Background:
        Given Calinga.Net is configured for organization "Bob's Company"
        And Calinga.Net is configured for team "App Team"
        And Calinga.Net is configured for project "Registration App"

    Scenario: Return translation from cache if cache is available
        Given in Cache there is an organization "Bob's Company"
        And in Cache "Bob's Company" has team "App Team"
        And in Cache "App Team" has project "Registration App"
        And in Cache "Registration App" has language "de"
        And in Cache "Registration App" has key "title"
        And in Cache the "de" translation of key "title" is "Titel"
        When the user requests "de" translation for key "title"
        Then Calinga.Net returns translation "Titel"

    Scenario: Return translation from cache if available although value has changed in Calinga
        Given in Cache there is an organization "Bob's Company"
        And in Cache "Bob's Company" has team "App Team"
        And in Cache "App Team" has project "Registration App"
        And in Cache "Registration App" has language "de"
        And in Cache "Registration App" has key "title"
        And in Cache the "de" translation of key "title" is "Titel"
        And in Calinga there is an organization "Bob's Company"
        And in Calinga "Bob's Company" has team "App Team"
        And in Calinga "App Team" has project "Registration App"
        And in Calinga "Registration App" has language "de"
        And in Calinga "Registration App" has key "title"
        But in Calinga the "de" translation of key "title" is "Überschrift"
        When the user requests "de" translation for key "title"
        Then Calinga.Net returns translation "Titel"

    Scenario: Return key name if key is unknown to cache
        Given in Cache there is an organization "Bob's Company"
        And in Cache "Bob's Company" has team "App Team"
        And in Cache "App Team" has project "Registration App"
        And in Cache "Registration App" has language "de"
        When the user requests "de" translation for key "title"
        Then Calinga.Net returns translation "title"
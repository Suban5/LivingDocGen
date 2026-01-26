Feature: Dating App Matching
  As a dating app user
  I want to find matches
  So that I can meet people

  Scenario: Create dating profile
    Given I am new user
    When I create profile
    And I add photos and bio
    Then profile should be complete
    And visible to others

  Scenario: Browse potential matches
    Given I am looking for matches
    When I browse profiles
    Then I see people
    And based on preferences

  Scenario: Like a profile
    Given I found interesting person
    When I like their profile
    Then they should be notified
    And we might match

  Scenario: Match with someone
    Given we both liked each other
    When match is created
    Then we can start chatting
    And are notified

  Scenario: Send message
    Given I matched with someone
    When I send message
    Then they receive it
    And can respond

  Scenario: Report inappropriate profile
    Given I see fake profile
    When I report it
    Then moderators are notified
    And profile is reviewed

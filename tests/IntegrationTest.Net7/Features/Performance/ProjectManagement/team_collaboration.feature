Feature: Team Collaboration Tools
  As a team member
  I want to collaborate
  So that we work efficiently together

  Scenario: Create project workspace
    Given I am starting a new project
    When I create a workspace
    Then team members can be invited
    And shared resources available

  Scenario: Share documents
    Given I have project documents
    When I upload to shared drive
    Then team should have access
    And version history tracked

  Scenario: Schedule team meeting
    Given I need to discuss progress
    When I schedule a meeting
    Then calendar invites should be sent
    And meeting room reserved

  Scenario: Conduct video conference
    Given meeting time has arrived
    When I start video call
    Then participants should join
    And screen sharing available

  Scenario: Create meeting notes
    Given meeting is in progress
    When I take notes
    Then notes should be shared live
    And action items tracked

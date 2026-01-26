Feature: Video Conferencing Platform
  As a remote worker
  I want to video conference
  So that I can meet virtually

  Scenario: Start instant meeting
    Given I need to meet now
    When I start instant meeting
    Then meeting room should be created
    And I can invite others

  Scenario: Schedule meeting
    Given I want to plan ahead
    When I schedule meeting
    Then calendar invites sent
    And meeting link generated

  Scenario: Join meeting
    Given I received meeting invite
    When I click join link
    Then I should enter meeting With video and audio

  Scenario: Share screen
    Given I am in meeting
    When I share my screen
    Then others should see My screen content

  Scenario: Record meeting
    Given meeting should be documented
    When I start recording
    Then video should be captured
    And saved after meeting

  Scenario: Use virtual background
    Given I want privacy
    When I enable virtual background
    Then my background should change To selected image

  Scenario: Chat during meeting
    Given I want to share link
    When I send chat message
    Then all participants see it In chat panel

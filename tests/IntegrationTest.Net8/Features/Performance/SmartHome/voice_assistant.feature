Feature: Voice Assistant Integration
  As a smart home user
  I want to use voice commands
  So that I can control devices hands-free

  Scenario: Control devices with voice
    Given voice assistant is set up
    When I say "Turn on bedroom lights"
    Then bedroom lights should turn on
    And confirmation should be spoken

  Scenario: Ask for status updates
    Given I want to check devices
    When I ask "Are all doors locked?"
    Then assistant should check
    And tell me door status

  Scenario: Create voice routines
    Given I use certain commands often
    When I create custom routine
    Then single phrase should trigger Multiple device actions

  Scenario: Play music on speakers
    Given I have smart speakers
    When I say "Play jazz music"
    Then music should start playing On specified speaker

  Scenario: Set reminders and timers
    Given I need to remember something
    When I ask to set reminder
    Then reminder should be created
    And alert me at right time

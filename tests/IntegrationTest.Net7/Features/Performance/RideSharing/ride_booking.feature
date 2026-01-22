Feature: Ride Booking Service
  As a passenger
  I want to book rides
  So that I can travel conveniently

  Scenario: Request immediate ride
    Given I need a ride now
    When I enter destination
    And I request a ride
    Then nearby drivers should be notified
    And ride should be assigned

  Scenario: Schedule ride in advance
    Given I have appointment tomorrow
    When I schedule ride for specific time
    Then booking should be confirmed
    And driver assigned before pickup

  Scenario: Choose vehicle type
    Given I need extra space
    When I select SUV option
    Then only SUV drivers should be matched
    And price should reflect vehicle type

  Scenario: Track driver location
    Given driver accepted ride
    When I view map
    Then I should see driver approaching
    And estimated arrival time

  Scenario: Complete ride and pay
    Given ride is complete
    When I arrive at destination
    Then payment should process automatically
    And receipt should be sent

  Scenario: Rate driver
    Given ride ended
    When I rate the experience
    Then rating should be recorded
    And I can leave feedback

Feature: Driver Management System
  As a ride-share driver
  I want to manage my driving
  So that I can earn income

  Scenario: Go online to accept rides
    Given I want to start driving
    When I go online
    Then I should receive ride requests From nearby passengers

  Scenario: Accept ride request
    Given I received ride request
    When I accept the ride
    Then navigation should start To passenger pickup location

  Scenario: Navigate to pickup
    Given I accepted ride
    When I follow navigation
    Then I should reach passenger At specified location

  Scenario: Start ride
    Given passenger is in vehicle
    When I start the ride
    Then meter should begin
    And route to destination shown

  Scenario: Complete ride
    Given we reached destination
    When I end the ride
    Then payment should be processed
    And earnings added to account

  Scenario: View earnings summary
    Given I drove today
    When I check earnings
    Then I should see total earned
    And breakdown by ride

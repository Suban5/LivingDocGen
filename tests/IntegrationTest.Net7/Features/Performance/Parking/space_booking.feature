Feature: Parking Space Booking
  As a driver
  I want to book parking
  So that I have guaranteed spot

  Scenario: Search for parking
    Given I need parking downtown
    When I search for available spots
    Then I should see options
    And with prices and locations

  Scenario: Book parking spot
    Given I found suitable spot
    When I book for 3 hours
    Then reservation should be confirmed
    And QR code provided

  Scenario: Extend parking time
    Given my meeting is running late
    When I extend reservation
    Then additional time should be added
    And payment processed

  Scenario: Navigate to parking
    Given I booked a spot
    When I start navigation
    Then I should get directions To exact parking location

  Scenario: Check in to parking
    Given I arrived at parking
    When I scan QR code
    Then check-in should be recorded
    And timer should start

  Scenario: Check out from parking
    Given I am leaving
    When I check out
    Then payment should finalize
    And receipt should be sent

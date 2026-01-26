Feature: Hotel Reservation System
  As a traveler
  I want to book hotels
  So that I have accommodation

  Scenario: Search hotels by location
    Given I am traveling to "Paris"
    When I search for hotels
    And I filter by price range $100-$200
    Then I should see matching hotels
    And their ratings and reviews

  Scenario: Book hotel room
    Given I found a hotel I like
    When I select check-in date "March 20, 2026"
    And I select check-out date "March 25, 2026"
    Then I should see available room types
    And total cost for 5 nights

  Scenario: Book room with breakfast included
    Given I am booking a hotel room
    When I select "Room with Breakfast" option
    Then breakfast cost should be added
    And meal times should be displayed

  Scenario: Request early check-in
    Given I am completing hotel booking
    When I request early check-in
    Then the request should be sent to hotel
    And I should see confirmation status

  Scenario: Cancel hotel reservation
    Given I have a hotel booking
    And the cancellation policy allows free cancellation
    When I cancel the booking
    Then I should receive full refund
    And cancellation confirmation email

  Scenario: Add special requests
    Given I am booking a hotel
    When I add request "High floor, non-smoking"
    Then the request should be sent to hotel
    And noted in reservation details

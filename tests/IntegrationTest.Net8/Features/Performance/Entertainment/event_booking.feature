Feature: Event and Ticket Booking
  As an entertainment enthusiast
  I want to book event tickets
  So that I can attend shows and concerts

  Scenario: Search for events
    Given I want to find events in my city
    When I search for "concerts in New York"
    Then I should see upcoming concerts
    And venue information

  Scenario: View event details
    Given I found an interesting concert
    When I click on the event
    Then I should see full details including date, time, venue, and pricing

  Scenario: Select seats for concert
    Given I am booking concert tickets
    When I view the seating chart
    Then I can select my preferred seats
    And see real-time availability

  Scenario: Book multiple tickets
    Given I want to go with 3 friends
    When I select 4 tickets
    And I complete the purchase
    Then all 4 tickets should be booked
    And sent to my email

  Scenario: Apply promo code
    Given I have a discount code
    When I enter the promo code at checkout
    Then the discount should be applied
    And total price should be reduced

  Scenario: Transfer ticket to friend
    Given I cannot attend the event
    When I transfer my ticket to a friend
    Then the ticket should be sent to them
    And removed from my account

  Scenario: Check-in at venue
    Given I arrived at the event
    When I scan my digital ticket
    Then I should be checked in
    And allowed entry to venue

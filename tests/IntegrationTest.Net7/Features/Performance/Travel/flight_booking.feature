Feature: Flight Booking System
  As a traveler
  I want to book flights
  So that I can travel to my destination

  Scenario: Search for flights
    Given I want to travel from "New York" to "London"
    When I search for flights on "March 15, 2026"
    Then I should see available flights
    And prices for different classes

  Scenario: Book economy class flight
    Given I found a suitable flight
    When I select economy class
    And I complete passenger details
    Then the booking should be confirmed
    And I should receive e-ticket

  Scenario: Book round trip flight
    Given I am searching for flights
    When I select round trip option
    And I choose departure and return dates
    Then I should see round trip options
    And combined pricing

  Scenario: Add extra baggage
    Given I am booking a flight
    When I add 1 extra baggage item
    Then baggage fee should be added
    And total price should be updated

  Scenario: Select seat preference
    Given I have booked a flight
    When I choose my seat from seat map
    Then the seat should be reserved
    And shown on my boarding pass

  Scenario: Book multi-city flight
    Given I want to visit 3 cities
    When I enter all destinations
    Then I should see multi-city options
    And optimized itinerary

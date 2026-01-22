Feature: Car Rental Service
  As a traveler
  I want to rent cars
  So that I can have transportation

  Scenario: Search for rental cars
    Given I am in "Los Angeles"
    When I search for available cars
    Then I should see different car types
    And daily rental rates

  Scenario: Book compact car
    Given I selected a compact car
    When I choose pickup date "April 1, 2026"
    And I choose return date "April 7, 2026"
    Then the booking should be confirmed
    And total cost should be calculated

  Scenario: Add additional driver
    Given I am booking a rental car
    When I add my spouse as additional driver
    Then additional driver fee should be added
    And both names should be on contract

  Scenario: Purchase insurance coverage
    Given I am completing car rental booking
    When I select full insurance coverage
    Then insurance cost should be added
    And coverage details should be provided

  Scenario: Choose pickup location
    Given I am booking a car
    When I select "Airport Terminal 1" as pickup
    Then location should be confirmed
    And directions should be provided

  Scenario: Extend rental period
    Given I have an active car rental
    When I request to extend by 2 days
    Then extension request should be processed
    And new total cost should be calculated

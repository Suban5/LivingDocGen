Feature: Travel Package Deals
  As a traveler
  I want to book package deals
  So that I can save money

  Scenario: Browse vacation packages
    Given I want to go to "Hawaii"
    When I browse vacation packages
    Then I should see bundled deals
    And including flights, hotels, and activities

  Scenario: Book all-inclusive package
    Given I found an all-inclusive resort package
    When I book the package
    Then flight, hotel, and meals should be included
    And I should see total savings

  Scenario: Customize package components
    Given I am viewing a package
    When I upgrade hotel to 5-star
    And I keep economy flights
    Then package price should be recalculated
    And savings percentage should update

  Scenario: Add tour activities to package
    Given I have booked a package
    When I add "City Tour" activity
    And I add "Snorkeling" activity
    Then activities should be added to itinerary
    And total cost should update

  Scenario: Group booking discount
    Given I am booking for 6 people
    When I select a group package
    Then group discount should be applied
    And I should see per person cost

  Scenario: Last minute deals
    Given I am looking for immediate travel
    When I filter by "Last Minute Deals"
    Then I should see discounted packages
    And for departures within next 7 days

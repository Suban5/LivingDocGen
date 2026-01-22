Feature: Property Search and Discovery
  As a home buyer
  I want to search for properties
  So that I can find my dream home

  Scenario: Search by location
    Given I want to buy in "Seattle"
    When I search for properties
    Then I should see available listings In Seattle area

  Scenario: Filter by price range
    Given I am searching for properties
    When I set price range $300k to $500k
    Then only properties in range should show
    And count should be displayed

  Scenario: Filter by property type
    Given I only want apartments
    When I filter by "Apartment"
    Then only apartments should be shown
    And houses should be excluded

  Scenario: Sort search results
    Given I have search results
    When I sort by "Price: Low to High"
    Then results should be reordered
    And cheapest properties shown first

  Scenario: Save search criteria
    Given I configured specific filters
    When I save the search
    Then I should be able to reload it
    And get alerts for new matches

  Scenario: View property on map
    Given I am viewing search results
    When I switch to map view
    Then all properties should be shown on map
    And I can click pins to see details

  Scenario: Compare multiple properties
    Given I shortlisted 3 properties
    When I select "Compare"
    Then I should see side-by-side comparison Of features and prices

Feature: Restaurant Browsing and Search
  As a hungry customer
  I want to browse restaurants
  So that I can order food

  Scenario: Browse nearby restaurants
    Given I am at home
    When I open the app
    Then I should see restaurants near me
    And their estimated delivery times

  Scenario: Filter by cuisine type
    Given I want Italian food
    When I apply "Italian" filter
    Then only Italian restaurants should show
    And I can see their menus

  Scenario: Search for specific dish
    Given I want pizza
    When I search for "pizza"
    Then restaurants offering pizza should appear
    And I can see pizza options

  Scenario: View restaurant details
    Given I found interesting restaurant
    When I click on it
    Then I should see full menu
    And ratings and reviews

  Scenario: Check restaurant ratings
    Given I am choosing a restaurant
    When I view ratings
    Then I should see average rating
    And number of reviews

  Scenario: Filter by delivery time
    Given I am very hungry
    When I filter by "Under 30 minutes"
    Then only fast delivery restaurants show
    And delivery time is displayed

Feature: Weather Monitoring and Forecasting
  As a user
  I want to check weather
  So that I can plan my day

  Scenario: View current weather
    Given I open weather app
    When I allow location access
    Then I should see current temperature
    And weather conditions

  Scenario: Check hourly forecast
    Given I want detailed forecast
    When I view hourly breakdown
    Then I should see temperature For next 24 hours

  Scenario: View 7-day forecast
    Given I am planning week
    When I check weekly forecast
    Then I should see predictions For next 7 days

  Scenario: Receive severe weather alerts
    Given storm is approaching
    When alert is issued
    Then I should be notified
    And with details and safety info

  Scenario: Add multiple locations
    Given I travel frequently
    When I add favorite cities
    Then I can switch between them
    And see their weather

  Scenario: View weather radar
    Given I want to see rain patterns
    When I open radar view
    Then I should see animated map Of precipitation

Feature: Energy Management System
  As a homeowner
  I want to manage energy usage
  So that I reduce costs

  Scenario: Monitor energy consumption
    Given I have smart meter
    When I check energy usage
    Then I should see consumption By device and time

  Scenario: Set energy saving mode
    Given energy costs are high
    When I enable eco mode
    Then devices should optimize For energy efficiency

  Scenario: Schedule device operation
    Given I want to save energy
    When I schedule appliances
    Then they should run During off-peak hours

  Scenario: View energy reports
    Given I used energy this month
    When I generate report
    Then I should see consumption trends
    And cost breakdown

  Scenario: Receive high usage alerts
    Given I set usage threshold
    When consumption exceeds limit
    Then I should be notified
    And can take action

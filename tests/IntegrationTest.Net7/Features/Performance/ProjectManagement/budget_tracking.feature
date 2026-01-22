Feature: Budget and Cost Tracking
  As a project manager
  I want to track costs
  So that projects stay within budget

  Scenario: Create project budget
    Given starting a new project
    When I set budget of $100,000
    Then budget should be allocated Across different categories

  Scenario: Log project expense
    Given project is running
    When I log an expense of $5,000
    Then expense should be recorded
    And budget should be updated

  Scenario: Generate cost report
    Given project has expenses
    When I generate financial report
    Then I should see all costs
    And budget vs actual

  Scenario: Set budget alerts
    Given project budget is $50,000
    When I set alert at 80% threshold
    Then I should be notified
    When spending reaches $40,000

  Scenario: Forecast project costs
    Given project is 50% complete
    When I forecast remaining costs
    Then estimate to completion shown
    And potential overrun identified

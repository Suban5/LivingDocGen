Feature: Survey and Feedback Collection
  As a researcher
  I want to create surveys
  So that I can collect feedback

  Scenario: Create survey
    Given I need customer feedback
    When I create survey
    And I add questions
    Then survey should be ready For distribution

  Scenario: Share survey link
    Given survey is created
    When I generate share link
    Then respondents can access Via the link

  Scenario: Collect responses
    Given survey is active
    When people submit responses
    Then answers should be recorded
    And stored securely

  Scenario: View response analytics
    Given responses are collected
    When I view analytics
    Then I should see charts
    And response statistics

  Scenario: Export survey data
    Given I need to analyze data
    When I export responses
    Then data should download In CSV or Excel format

  Scenario: Close survey
    Given I have enough responses
    When I close the survey
    Then no more submissions accepted
    And final report generated

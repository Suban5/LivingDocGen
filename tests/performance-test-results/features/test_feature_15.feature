Feature: Test Feature 15
  As a tester
  I want to test performance
  So that I can validate scroll smoothness

  @smoke @performance
  Scenario: Test Scenario 1 for Feature 15
    Given I have opened the application
    When I perform action 15
    Then I should see result 15

  @regression
  Scenario: Test Scenario 2 for Feature 15
    Given the system is ready
    When I execute test 15
    Then the system responds correctly

  Scenario Outline: Test Scenario Outline for Feature 15
    Given I have <item>
    When I use <action>
    Then I get <result>

    Examples:
      | item    | action   | result  |
      | A       | process  | success |
      | B       | validate | pass    |
      | C       | execute  | done    |

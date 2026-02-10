@authentication @smoke
Feature: User Login
  As a user
  I want to log in to the system
  So that I can access my account

  Background:
    Given the application is running
    And the database is initialized

  @positive
  Scenario: Successful login with valid credentials
    ##Test comment inside the scenario
    Given I am on the login page
    When I enter username "john.doe@example.com"
    And I enter password "SecurePass123!"
    And I click the login button
    Then I should be redirected to the dashboard
    And I should see a welcome message "Welcome, John Doe"

  @negative
  Scenario: Failed login with invalid password
    Given I am on the login page
    When I enter username "john.doe@example.com"
    And I enter password "WrongPassword"
    And I click the login button
    Then I should see an error message "Invalid credentials"
    And I should remain on the login page

  @data-driven
  Scenario Outline: Login with multiple users
    Given I am on the login page
    When I enter username "<username>"
    And I enter password "<password>"
    And I click the login button
    Then I should see "<result>"

    Examples:
      | username              | password        | result                |
      | admin@test.com        | Admin123!       | Welcome, Admin        |
      | user@test.com         | User123!        | Welcome, User         |
      | invalid@test.com      | wrong           | Invalid credentials   |

Feature: Bank Account Management
  As a bank customer
  I want to manage my accounts
  So that I can track my finances

  Scenario: Open new savings account
    Given I am a verified customer
    When I apply for a savings account
    And I deposit initial amount of $500
    Then the account should be created
    And I should receive an account number

  Scenario: View account balance
    Given I am logged into my account
    When I navigate to account dashboard
    Then I should see my current balance
    And I should see recent transactions

  Scenario: Update account preferences
    Given I am logged into my account
    When I update my email address
    And I change notification preferences
    Then the changes should be saved
    And I should receive a confirmation

  Scenario: Close savings account
    Given I have a savings account with $100 balance
    When I request to close the account
    Then the remaining balance should be transferred
    And the account should be marked as closed

  Scenario: View account statement
    Given I have an active checking account
    When I request a statement for last 3 months
    Then I should receive a PDF statement
    And it should include all transactions

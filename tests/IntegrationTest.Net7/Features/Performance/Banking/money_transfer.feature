Feature: Money Transfer Operations
  As a bank customer
  I want to transfer money
  So that I can make payments

  Scenario: Transfer between own accounts
    Given I have $1000 in my checking account
    When I transfer $200 to my savings account
    Then the checking account balance should be $800
    And the savings account balance should increase by $200

  Scenario: Transfer to another customer
    Given I have $500 in my account
    When I transfer $100 to account number "987654321"
    And I enter the correct PIN
    Then the transfer should be completed
    And both parties should receive notifications

  Scenario: International wire transfer
    Given I initiate a transfer of $1000 to UK
    When I provide recipient IBAN details
    And I confirm the exchange rate
    Then the transfer should be queued
    And I should see the expected delivery time

  Scenario: Failed transfer due to insufficient funds
    Given I have $50 in my account
    When I attempt to transfer $100
    Then the transfer should be declined
    And I should see an error message

  Scenario: Schedule recurring transfer
    Given I want to transfer $500 monthly
    When I set up a recurring transfer
    And I specify the 1st of each month
    Then the schedule should be saved
    And transfers should execute automatically

  Scenario: Cancel pending transfer
    Given I have a scheduled transfer for tomorrow
    When I cancel the transfer
    Then the transfer should be removed from queue
    And no money should be moved

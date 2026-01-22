Feature: Card Services Management
  As a cardholder
  I want to manage my cards
  So that I can use them securely

  Scenario: Request new debit card
    Given I have an active checking account
    When I request a new debit card
    And I select card design preference
    Then the card request should be processed
    And I should receive the card within 7 days

  Scenario: Block lost card
    Given I have an active credit card
    When I report the card as lost
    Then the card should be blocked immediately
    And no transactions should be allowed

  Scenario: Set card transaction limits
    Given I have a debit card
    When I set daily withdrawal limit to $500
    And I set daily purchase limit to $2000
    Then the limits should be applied
    And transactions exceeding limits should be declined

  Scenario: View card transactions
    Given I have a credit card
    When I view my card statement
    Then I should see all transactions
    And they should be categorized by type

  Scenario: Activate new card
    Given I received a new card in mail
    When I activate it using mobile app
    And I set a new PIN
    Then the card should be ready to use
    And I should be able to make transactions

  Scenario: Report fraudulent transaction
    Given I notice an unauthorized transaction
    When I report it as fraud
    Then the transaction should be investigated
    And my card should be blocked temporarily

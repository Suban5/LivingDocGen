Feature: Payment Gateway Integration
  As a customer
  I want to pay for my orders securely
  So that I can complete my purchase

  Scenario: Pay with credit card
    Given I have items worth $150 in my cart
    When I enter valid credit card details
    And I submit the payment
    Then the payment should be processed successfully
    And I should see a payment confirmation

  Scenario: Pay with PayPal
    Given I am at the payment step
    When I select "PayPal" as payment method
    And I authenticate with PayPal
    Then the payment should be completed
    And I should be redirected back to the store

  Scenario: Failed payment due to insufficient funds
    Given I am paying for a $500 order
    When I enter a card with insufficient funds
    Then the payment should be declined
    And I should see an error message

  Scenario: Save payment method for future use
    Given I am entering payment details
    When I check "Save this card for future purchases"
    And I complete the payment
    Then the card should be saved to my account
    And card details should be masked for security

  Scenario: Split payment between gift card and credit card
    Given my order total is $100
    When I apply a gift card worth $30
    And I pay the remaining $70 with credit card
    Then both payments should be processed
    And the order should be confirmed

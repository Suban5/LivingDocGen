Feature: Order Processing System
  As a customer
  I want to process my orders efficiently
  So that I receive my products on time

  Scenario: Place a simple order
    Given I have added 2 items to my cart
    When I proceed to checkout
    And I complete the payment with credit card
    Then the order should be confirmed
    And I should receive an order confirmation email

  Scenario: Order with multiple shipping addresses
    Given I have 5 items in my cart
    When I specify different shipping addresses for 2 groups
    And I complete the payment
    Then 2 separate orders should be created
    And each should have correct shipping information

  Scenario: Cancel order before shipping
    Given I have placed an order 30 minutes ago
    And the order status is "Processing"
    When I request to cancel the order
    Then the order should be cancelled
    And I should receive a full refund

  Scenario: Track order status
    Given I have an order with tracking number "TRK123456"
    When I check the order status
    Then I should see the current delivery stage
    And I should see estimated delivery date

  Scenario: Order with gift wrapping
    Given I am checking out with 1 item
    When I select "Gift Wrapping" option
    And I add a gift message
    Then the gift wrapping fee should be added
    And the gift message should be saved

  Scenario: Express delivery option
    Given I have items in my cart
    When I select "Express Delivery" at checkout
    Then the delivery fee should increase by $15
    And the delivery time should be 1-2 business days

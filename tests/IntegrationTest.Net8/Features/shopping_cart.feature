@shopping @regression
Feature: Shopping Cart Management
  As a customer
  I want to manage items in my shopping cart
  So that I can purchase products

  Scenario: Add product to empty cart
    Given I am logged in as a customer
    And my cart is empty
    When I navigate to product "Laptop"
    And I click "Add to Cart"
    Then the cart should contain 1 item
    And the cart total should be "$999.99"

  Scenario: Remove product from cart
    Given I am logged in as a customer
    And I have the following items in my cart:
      | Product    | Quantity | Price   |
      | Laptop     | 1        | $999.99 |
      | Mouse      | 2        | $25.00  |
    When I remove "Mouse" from the cart
    Then the cart should contain 1 item
    And the cart total should be "$998.99"

  @ignore
  Scenario: Apply discount code
    Given I am logged in as a customer
    And I have items worth "$500.00" in my cart
    When I apply discount code "SAVE20"
    Then the cart total should be "$400.00"
    And I should see a message "Discount applied successfully"

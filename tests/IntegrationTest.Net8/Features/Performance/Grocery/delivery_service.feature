Feature: Grocery Delivery Service
  As a customer
  I want groceries delivered
  So that I save time

  Scenario: Browse grocery items
    Given I need groceries
    When I browse categories
    Then I see available products
    And with prices

  Scenario: Add items to cart
    Given I am shopping
    When I add items to cart
    Then cart should update
    And with total price

  Scenario: Apply coupon code
    Given I have coupon
    When I apply code
    Then discount should be applied
    And total reduced

  Scenario: Select delivery slot
    Given I am ready to checkout
    When I choose delivery time
    Then slot should be reserved for my order

  Scenario: Track delivery
    Given order is being prepared
    When I check status
    Then I see current stage
    And estimated arrival

  Scenario: Rate delivery
    Given order was delivered
    When I rate experience
    Then feedback recorded
    And driver sees rating

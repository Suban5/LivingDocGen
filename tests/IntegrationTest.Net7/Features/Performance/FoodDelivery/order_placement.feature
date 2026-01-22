Feature: Food Order Placement
  As a customer
  I want to place food orders
  So that I can get food delivered

  Scenario: Add items to cart
    Given I am browsing a restaurant menu
    When I add "Margherita Pizza" to cart
    And I add "Garlic Bread" to cart
    Then items should appear in my cart
    And total should be calculated

  Scenario: Customize menu item
    Given I am ordering a burger
    When I select "No onions"
    And I add "Extra cheese"
    Then customization should be noted
    And price adjusted if applicable

  Scenario: Apply promo code
    Given I have a discount code
    When I enter the promo code
    Then discount should be applied
    And new total should be shown

  Scenario: Schedule delivery time
    Given I want food delivered later
    When I schedule for 7 PM
    Then delivery time should be set
    And restaurant should be notified

  Scenario: Add delivery instructions
    Given I am checking out
    When I add note "Leave at door"
    Then instruction should be saved
    And shown to delivery driver

  Scenario: Place order
    Given my cart has items
    When I confirm the order
    And I complete payment
    Then order should be sent to restaurant
    And I should receive confirmation

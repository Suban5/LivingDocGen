Feature: Gift Services and Registry
  As a customer
  I want gift services
  So that I can send gifts to others

  Scenario: Send product as gift
    Given I am purchasing a product
    When I mark it as a gift
    And I add gift message
    Then price should not be shown on receipt
    And gift message should be included

  Scenario: Create gift registry
    Given I am planning an event
    When I create a gift registry
    And I add 20 items
    Then registry should be shareable
    And others can purchase from it

  Scenario: Purchase from registry
    Given I found a friend's registry
    When I select an item to purchase
    Then item should be marked as purchased
    And shipped to registry owner

  Scenario: Send digital gift card
    Given I want to send a gift card
    When I purchase $50 gift card
    And I enter recipient email
    Then gift card should be emailed
    And I should receive confirmation

  Scenario: Redeem gift card
    Given I received a gift card code
    When I apply it at checkout
    Then the balance should be used
    And any remainder saved to account

  Scenario: Check gift card balance
    Given I have a gift card
    When I check the balance
    Then I should see remaining amount
    And expiration date if applicable

Feature: Real-time Order Tracking
  As a customer
  I want to track my order
  So that I know when food will arrive

  Scenario: View order status
    Given I placed an order
    When I open order tracking
    Then I should see current status
    And estimated delivery time

  Scenario: Track preparation stage
    Given restaurant is preparing my order
    When I check the status
    Then it should show "Preparing" and elapsed time

  Scenario: Track delivery driver
    Given driver picked up my order
    When I view the map
    Then I should see driver's location
    And route to my address

  Scenario: Receive status updates
    Given my order is in progress
    When status changes
    Then I should receive push notification
    And updated ETA

  Scenario: Contact delivery driver
    Given driver is on the way
    When I need to give additional instructions
    Then I can call or message driver through the app

  Scenario: Rate completed order
    Given my order was delivered
    When I rate the experience
    Then rating should be submitted
    And I can leave feedback

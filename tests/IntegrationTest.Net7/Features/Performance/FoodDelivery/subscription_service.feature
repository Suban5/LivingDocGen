Feature: Food Delivery Subscription
  As a frequent customer
  I want subscription benefits
  So that I save on delivery fees

  Scenario: Subscribe to premium
    Given I order food frequently
    When I subscribe to premium plan
    Then I should get free delivery
    And exclusive discounts

  Scenario: Track subscription savings
    Given I am a premium member
    When I check my savings
    Then I should see total amount saved on delivery fees this month

  Scenario: Access exclusive deals
    Given I have active subscription
    When I browse restaurants
    Then I should see subscriber-only deals and special discounts

  Scenario: Pause subscription
    Given I will be traveling
    When I pause my subscription
    Then billing should stop
    And I can reactivate anytime

  Scenario: Cancel subscription
    Given I want to cancel
    When I go through cancellation
    Then subscription should end at period end
    And benefits continue until

  Scenario: Upgrade subscription tier
    Given I have basic subscription
    When I upgrade to premium
    Then enhanced benefits should activate
    And billing should be adjusted

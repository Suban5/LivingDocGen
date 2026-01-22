Feature: Loyalty Rewards Program
  As a loyal customer
  I want to earn rewards
  So that I can save on purchases

  Scenario: Join loyalty program
    Given I am a registered customer
    When I enroll in rewards program
    Then I should receive welcome bonus
    And start earning points

  Scenario: Earn points on purchase
    Given I am a rewards member
    When I make a $100 purchase
    Then I should earn 100 points
    And see updated point balance

  Scenario: Redeem points for discount
    Given I have 1000 points
    When I redeem for $10 off
    Then points should be deducted
    And discount applied to cart

  Scenario: Check points balance
    Given I am a rewards member
    When I check my account
    Then I should see current points
    And points history

  Scenario: Birthday bonus points
    Given today is my birthday
    When I log into my account
    Then I should receive bonus points
    And a birthday discount code

  Scenario: Tier upgrade notification
    Given I have earned 5000 points this year
    When I reach gold tier
    Then I should be upgraded
    And receive tier benefits

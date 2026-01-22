Feature: Travel Loyalty Program
  As a frequent traveler
  I want to earn rewards
  So that I can get benefits

  Scenario: Enroll in loyalty program
    Given I am a new user
    When I sign up for loyalty program
    Then my account should be created
    And I should receive welcome bonus points

  Scenario: Earn points on flight booking
    Given I am a loyalty member
    When I book a flight worth $500
    Then I should earn 500 points
    And points should be added to my account

  Scenario: Redeem points for free flight
    Given I have 25000 points
    When I redeem points for a flight
    Then the flight cost should be covered
    And points should be deducted

  Scenario: Achieve elite status
    Given I have earned 50000 points this year
    When the system evaluates my status
    Then I should be upgraded to Gold tier
    And receive elite benefits

  Scenario: Transfer points to partner
    Given I have 10000 points
    When I transfer points to hotel partner
    Then points should be converted
    And available in hotel loyalty account

  Scenario: Points expiration notification
    Given I have points expiring soon
    When the expiration date approaches
    Then I should receive notification
    And suggestions to use points

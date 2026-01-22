Feature: Entertainment Subscription Management
  As a user
  I want to manage my subscriptions
  So that I can control my entertainment expenses

  Scenario: Subscribe to premium plan
    Given I am using free tier
    When I upgrade to premium plan
    And I complete payment
    Then premium features should be unlocked
    And I should see ad-free experience

  Scenario: Change subscription plan
    Given I have a monthly subscription
    When I switch to annual plan
    Then my billing should be updated
    And I should save money on yearly rate

  Scenario: Cancel subscription
    Given I want to cancel my subscription
    When I go through cancellation process
    Then subscription should end at period end
    And I should retain access until then

  Scenario: Reactivate cancelled subscription
    Given I previously cancelled subscription
    When I choose to reactivate
    Then my account should be restored
    And billing should resume

  Scenario: Update payment method
    Given my credit card is expiring
    When I update payment information
    Then new card should be set as default
    And future charges should use new card

  Scenario: View billing history
    Given I have been subscribed for 6 months
    When I access billing section
    Then I should see all past invoices
    And be able to download receipts

  Scenario: Family plan sharing
    Given I have a family subscription
    When I invite 4 family members
    Then they should get access
    And all share one subscription cost

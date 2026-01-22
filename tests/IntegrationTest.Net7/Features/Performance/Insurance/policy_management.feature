Feature: Insurance Policy Management
  As an insurance customer
  I want to manage my policies
  So that I have proper coverage

  Scenario: Purchase auto insurance policy
    Given I need car insurance
    When I provide vehicle details
    And I select coverage options
    Then policy should be created
    And I should receive policy documents

  Scenario: Renew existing policy
    Given my policy is expiring soon
    When I renew the policy
    Then coverage should continue
    And premium should be updated

  Scenario: File insurance claim
    Given I had a car accident
    When I file a claim
    And I upload photos and police report
    Then claim should be registered
    And adjuster should be assigned

  Scenario: Update policy beneficiaries
    Given I have a life insurance policy
    When I change beneficiary information
    Then updates should be recorded
    And confirmation should be sent

  Scenario: Request policy cancellation
    Given I no longer need the policy
    When I request cancellation
    Then policy should be terminated
    And prorated refund calculated

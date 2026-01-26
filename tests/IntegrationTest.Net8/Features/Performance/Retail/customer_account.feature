Feature: Customer Account Management
  As a retail customer
  I want to manage my account
  So that I can shop efficiently

  Scenario: Create customer account
    Given I am a new customer
    When I fill in registration form
    And I verify my email
    Then my account should be created
    And I should be logged in

  Scenario: Update profile information
    Given I am logged into my account
    When I change my shipping address
    And I update my phone number
    Then the changes should be saved
    And used for future orders

  Scenario: Save multiple addresses
    Given I shop for home and office
    When I add two addresses
    Then both should be saved
    And I can choose at checkout

  Scenario: Set communication preferences
    Given I am in account settings
    When I opt-out of promotional emails
    But keep order notifications
    Then preferences should be saved
    And respected in communications

  Scenario: View order history
    Given I have placed 10 orders
    When I access order history
    Then I should see all orders
    And their current status

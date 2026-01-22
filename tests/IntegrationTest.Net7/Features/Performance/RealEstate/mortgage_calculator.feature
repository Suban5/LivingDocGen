Feature: Mortgage Calculator and Pre-approval
  As a home buyer
  I want to calculate mortgage
  So that I know my budget

  Scenario: Calculate monthly payment
    Given property price is $400,000
    When I enter 20% down payment
    And I select 30-year term at 4% interest
    Then monthly payment should be calculated
    And displayed with breakdown

  Scenario: Compare loan options
    Given I am exploring financing
    When I compare 15-year vs 30-year loan
    Then I should see payment differences
    And total interest paid

  Scenario: Calculate affordability
    Given my annual income is $80,000
    When I check how much I can afford
    Then maximum home price should be shown Based on standard debt ratios

  Scenario: Apply for pre-approval
    Given I want pre-approval letter
    When I submit financial information
    Then application should be processed
    And I receive pre-approval amount

  Scenario: View pre-approval status
    Given I applied for pre-approval
    When I check status
    Then I should see current stage
    And any required documents

  Scenario: Add property taxes and insurance
    Given I am calculating mortgage
    When I include taxes and insurance
    Then total monthly payment should update
    And show complete housing cost

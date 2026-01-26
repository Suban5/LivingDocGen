Feature: Loan Application and Processing
  As a customer
  I want to apply for loans
  So that I can finance my needs

  Scenario: Apply for personal loan
    Given I am an existing customer
    When I apply for a personal loan of $10000
    And I provide income proof
    Then my application should be submitted
    And I should receive a reference number

  Scenario: Loan eligibility check
    Given I want to apply for a home loan
    When I enter my income and expenses
    Then the system should calculate my eligibility
    And show the maximum loan amount

  Scenario: Approve loan application
    Given a loan application is pending review
    When the loan officer reviews the documents
    And approves the application
    Then the customer should be notified
    And the loan amount should be disbursed

  Scenario: Reject loan application
    Given a loan application fails credit check
    When the system evaluates the application
    Then the application should be rejected
    And the customer should receive rejection reasons

  Scenario: View loan repayment schedule
    Given I have an active loan
    When I access my loan details
    Then I should see the repayment schedule
    And each installment amount and due date

  Scenario: Make early loan repayment
    Given I have a loan with $5000 outstanding
    When I make an early payment of $1000
    Then the principal should be reduced
    And future interest should be recalculated

Feature: Payroll Processing System
  As a payroll administrator
  I want to process payroll
  So that employees are paid correctly

  Scenario: Process monthly payroll
    Given it's end of month
    When I run payroll process
    Then salaries should be calculated
    And payments initiated

  Scenario: Apply tax deductions
    Given processing payroll
    When I calculate taxes
    Then proper deductions should be applied
    And based on tax brackets

  Scenario: Generate pay slips
    Given payroll is processed
    When I generate pay slips
    Then employees should receive them Via email

  Scenario: Handle bonus payment
    Given employee earned bonus
    When I add bonus to payroll
    Then bonus should be included in next payment cycle

  Scenario: Process reimbursements
    Given employee submitted expenses
    When I approve reimbursement
    Then amount should be added to their next paycheck

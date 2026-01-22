Feature: Student Portal Dashboard
  As a student
  I want to access student portal
  So that I can manage my academic life

  Scenario: View academic transcript
    Given I am logged into student portal
    When I access my transcript
    Then I should see all completed courses
    And my grades and GPA

  Scenario: Check financial aid status
    Given I have applied for financial aid
    When I check aid status
    Then I should see application status
    And awarded amount if approved

  Scenario: Pay tuition fees
    Given my tuition bill is generated
    When I proceed to payment
    And I complete the transaction
    Then payment should be recorded
    And I should receive receipt

  Scenario: Update contact information
    Given I need to update my phone number
    When I edit my profile
    And I save changes
    Then new information should be saved
    And reflected in all systems

  Scenario: Request official documents
    Given I need a transcript copy
    When I submit document request
    Then request should be processed
    And I can track delivery status

  Scenario: View academic calendar
    Given I am planning my semester
    When I access academic calendar
    Then I should see important dates including holidays and exam periods

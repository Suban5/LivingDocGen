Feature: Assignment Submission and Grading
  As a student
  I want to submit assignments
  So that I can complete my coursework

  Scenario: View assignment details
    Given I am enrolled in a course
    When I access the assignments section
    Then I should see all assignments
    And their due dates

  Scenario: Submit assignment before deadline
    Given an assignment is due tomorrow
    When I upload my document
    And I click "Submit"
    Then the assignment should be submitted
    And I should receive confirmation

  Scenario: Late submission
    Given an assignment deadline has passed
    When I try to submit
    Then I should see late penalty warning
    And submission should be marked as late

  Scenario: Resubmit assignment
    Given I submitted an assignment
    And resubmissions are allowed
    When I upload a revised version
    Then old submission should be replaced
    And teacher sees latest version

  Scenario: View assignment grade
    Given teacher has graded my assignment
    When I check my grades
    Then I should see the score
    And teacher's feedback comments

  Scenario: Request grade review
    Given I received a grade I disagree with
    When I submit a review request
    Then request should be sent to teacher
    And I should track review status

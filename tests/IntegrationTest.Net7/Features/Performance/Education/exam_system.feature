Feature: Online Examination System
  As a student
  I want to take online exams
  So that I can be evaluated

  Scenario: Take multiple choice exam
    Given I have an exam scheduled
    When I start the exam
    Then I should see all questions
    And be able to select answers

  Scenario: Submit exam before time limit
    Given I am taking a timed exam
    When I complete all questions
    And I submit early
    Then my answers should be saved
    And exam should be marked as complete

  Scenario: Auto-submit when time expires
    Given exam has 60 minute time limit
    When the timer reaches zero
    Then exam should auto-submit
    And I should see confirmation

  Scenario: Navigate between questions
    Given I am taking an exam
    When I click "Next" or "Previous"
    Then I should move between questions
    And my answers should be saved

  Scenario: Flag questions for review
    Given I am unsure about a question
    When I flag it for review
    Then it should be marked
    And I can return to it later

  Scenario: View exam results
    Given my exam has been graded
    When I check results
    Then I should see my score
    And correct answers for review

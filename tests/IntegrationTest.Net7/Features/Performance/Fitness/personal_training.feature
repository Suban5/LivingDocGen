Feature: Personal Training Programs
  As a user
  I want personalized training plans
  So that I can achieve my fitness goals

  Scenario: Create custom workout plan
    Given I want to build muscle
    When I answer fitness questionnaire
    Then personalized plan should be generated
    And workouts scheduled for week

  Scenario: Follow guided workout
    Given I have a workout scheduled
    When I start the guided session
    Then instructions should be shown
    And timer should track rest periods

  Scenario: Modify workout plan
    Given I find plan too difficult
    When I adjust intensity level
    Then plan should be regenerated
    And with appropriate difficulty

  Scenario: Track personal records
    Given I lifted heavier weight today
    When I log the workout
    Then new personal record should be noted
    And celebrated with badge

  Scenario: Connect with trainer
    Given I have questions about form
    When I message my trainer
    Then trainer should receive message
    And can respond with guidance

  Scenario: Schedule trainer session
    Given I want 1-on-1 training
    When I book session with trainer
    Then appointment should be confirmed
    And video call link provided

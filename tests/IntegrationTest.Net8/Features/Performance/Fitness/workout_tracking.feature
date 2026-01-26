Feature: Workout Activity Tracking
  As a fitness enthusiast
  I want to track my workouts
  So that I can monitor my progress

  Scenario: Log cardio workout
    Given I completed a run
    When I log the workout
    And I enter distance and duration
    Then workout should be saved
    And calories burned should be calculated

  Scenario: Log strength training
    Given I finished weight training
    When I log exercises and sets
    And I enter weights used
    Then workout should be recorded
    And progress should be tracked

  Scenario: Start live workout tracking
    Given I am starting a workout
    When I start the timer
    Then time should be tracked live
    And I can log exercises in real-time

  Scenario: View workout history
    Given I have been working out regularly
    When I check my history
    Then I should see all past workouts
    And total stats

  Scenario: Set workout goals
    Given I want to run 50 miles this month
    When I set the goal
    Then progress should be tracked
    And I should see percentage complete

  Scenario: Complete workout challenge
    Given I am doing a 30-day challenge
    When I complete today's workout
    Then day should be marked complete
    And streak should be maintained

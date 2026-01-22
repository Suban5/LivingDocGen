Feature: Course Enrollment System
  As a student
  I want to enroll in courses
  So that I can pursue my education

  Scenario: Browse available courses
    Given I am a registered student
    When I view course catalog
    Then I should see all available courses
    And their descriptions and credits

  Scenario: Enroll in a course
    Given I want to take "Introduction to Python"
    When I click "Enroll" button
    And I confirm enrollment
    Then I should be registered for the course
    And it should appear in my schedule

  Scenario: Check course prerequisites
    Given I want to enroll in "Advanced Algorithms"
    When I attempt to enroll
    Then system should check prerequisites
    And inform me if I'm eligible

  Scenario: Drop a course
    Given I am enrolled in "Chemistry 101"
    And it's within drop period
    When I drop the course
    Then I should be unenrolled
    And the spot should be available for others

  Scenario: Waitlist for full course
    Given "Popular Psychology" is full
    When I add myself to waitlist
    Then I should be added to queue
    And notified if a spot opens

  Scenario: View class schedule
    Given I am enrolled in 5 courses
    When I view my schedule
    Then I should see all class timings
    And room locations

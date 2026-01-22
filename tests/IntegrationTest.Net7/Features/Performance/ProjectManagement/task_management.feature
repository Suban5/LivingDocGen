Feature: Project Task Management
  As a project manager
  I want to manage tasks
  So that projects stay on track

  Scenario: Create new task
    Given I am managing a project
    When I create a task "Design homepage"
    And I assign it to John
    Then task should be created
    And John should be notified

  Scenario: Update task status
    Given a task is in progress
    When team member marks it complete
    Then status should update
    And next dependent task should start

  Scenario: Set task priority
    Given I have multiple tasks
    When I mark a task as high priority
    Then it should appear at top of list
    And team should be alerted

  Scenario: Add task dependencies
    Given task B requires task A completion
    When I set dependency relationship
    Then task B should be blocked Until task A is complete

  Scenario: Track task time
    Given I am working on a task
    When I log 3 hours of work
    Then time should be recorded
    And project budget updated

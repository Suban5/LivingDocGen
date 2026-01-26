Feature: Sprint Planning and Execution
  As a scrum master
  I want to plan sprints
  So that development is organized

  Scenario: Create sprint
    Given we use agile methodology
    When I create a 2-week sprint
    Then sprint should be initialized
    And ready for planning

  Scenario: Add stories to sprint
    Given sprint planning meeting
    When team commits to stories
    Then stories should be added to sprint
    And capacity calculated

  Scenario: Conduct daily standup
    Given sprint is active
    When team has daily standup
    Then updates should be recorded
    And blockers identified

  Scenario: Complete sprint
    Given sprint end date reached
    When I close the sprint
    Then velocity should be calculated
    And retrospective scheduled

  Scenario: Track sprint burndown
    Given sprint is in progress
    When I view burndown chart
    Then I should see remaining work
    And trend toward completion

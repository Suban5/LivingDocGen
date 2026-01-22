Feature: Resource Allocation and Planning
  As a resource manager
  I want to allocate resources
  So that projects are properly staffed

  Scenario: Assign team member to project
    Given a new project needs staffing
    When I assign Sarah to the project
    Then her availability should be updated
    And project team roster updated

  Scenario: Check resource availability
    Given I need to staff a project
    When I check team availability
    Then I should see who is available
    And their skill sets

  Scenario: Handle resource conflicts
    Given John is assigned to two projects
    When scheduling conflict occurs
    Then I should be alerted
    And can reassign work

  Scenario: Track resource utilization
    Given team has been working
    When I generate utilization report
    Then I should see capacity usage
    And identify over/under allocation

  Scenario: Request additional resources
    Given project needs more people
    When I submit resource request
    Then request should be reviewed
    And approved or denied

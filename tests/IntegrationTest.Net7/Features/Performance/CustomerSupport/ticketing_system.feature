Feature: Customer Support Ticketing System
  As a support agent
  I want to manage support tickets
  So that customers get help

  Scenario: Create support ticket
    Given customer has issue
    When they submit ticket
    Then ticket should be created
    And assigned to agent

  Scenario: View ticket queue
    Given I am support agent
    When I view my queue
    Then I see all assigned tickets
    And sorted by priority

  Scenario: Respond to ticket
    Given I reviewed ticket
    When I send response
    Then customer should be notified
    And ticket updated

  Scenario: Escalate ticket
    Given issue is complex
    When I escalate to senior agent
    Then ticket should be reassigned
    And customer informed

  Scenario: Close resolved ticket
    Given issue is resolved
    When I close ticket
    Then customer gets notification
    And can reopen if needed

  Scenario: Track response time
    Given tickets are being handled
    When I view metrics
    Then I see average response time
    And resolution time

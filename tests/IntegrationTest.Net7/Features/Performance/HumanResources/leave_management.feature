Feature: Leave Management System
  As an employee
  I want to request time off
  So that I can take breaks

  Scenario: Submit vacation request
    Given I want to take vacation
    When I request 5 days off
    And I select dates
    Then request should be submitted
    And manager should be notified

  Scenario: Manager approves leave
    Given employee requested leave
    When I approve the request
    Then leave should be granted
    And calendar should be updated

  Scenario: Check leave balance
    Given I am an employee
    When I check my leave balance
    Then I should see available days for each leave type

  Scenario: Cancel leave request
    Given I have approved leave
    When I cancel the request
    Then leave should be restored
    And manager should be informed

  Scenario: Handle leave overlap
    Given team member has leave
    When another submits overlapping request
    Then conflict should be flagged
    And require manager decision

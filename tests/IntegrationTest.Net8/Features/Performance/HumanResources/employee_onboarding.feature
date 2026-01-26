Feature: Employee Onboarding Process
  As an HR manager
  I want to onboard new employees
  So that they integrate smoothly

  Scenario: Create new employee record
    Given we hired a new employee
    When I enter their information
    Then employee profile should be created
    And ID should be generated

  Scenario: Send welcome package
    Given employee starts next week
    When I send welcome email
    Then they should receive All necessary documents and information

  Scenario: Assign equipment
    Given new employee needs computer
    When I request equipment
    Then IT should be notified
    And equipment prepared

  Scenario: Schedule orientation
    Given employee's first day
    When I schedule orientation
    Then calendar invite should be sent
    And training materials prepared

  Scenario: Create access accounts
    Given employee needs system access
    When I request account creation
    Then accounts should be set up
    And credentials provided

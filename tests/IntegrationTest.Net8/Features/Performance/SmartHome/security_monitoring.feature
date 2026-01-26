Feature: Home Security Monitoring
  As a homeowner
  I want to monitor home security
  So that my home is protected

  Scenario: Arm security system
    Given I am leaving home
    When I arm the security system
    Then all sensors should be activated
    And I should receive confirmation

  Scenario: Receive intrusion alert
    Given security system is armed
    When motion is detected
    Then I should receive alert
    And can view camera footage

  Scenario: Disarm system with code
    Given I return home
    When I enter disarm code
    Then system should disarm
    And sensors deactivate

  Scenario: Check door/window sensors
    Given sensors are installed
    When I view sensor status
    Then I should see which doors/windows Are open or closed

  Scenario: Review security history
    Given I want to check activity
    When I view security log
    Then I should see all events With timestamps

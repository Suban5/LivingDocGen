Feature: Health Metrics and Analytics
  As a user
  I want to track health metrics
  So that I can understand my overall health

  Scenario: Log body weight
    Given I weighed myself
    When I log my weight
    Then weight should be recorded
    And trend chart should update

  Scenario: Track body measurements
    Given I took measurements
    When I log waist, chest, arm sizes
    Then measurements should be saved
    And progress photos can be added

  Scenario: Monitor heart rate
    Given I have wearable device connected
    When I sync the data
    Then heart rate readings should import
    And resting heart rate calculated

  Scenario: View weekly progress report
    Given it's end of the week
    When I generate progress report
    Then I should see all metrics
    And comparison with previous week

  Scenario: Set body composition goal
    Given I want to reach 15% body fat
    When I set the target
    Then progress should be tracked
    And estimated timeline shown

  Scenario: Connect fitness devices
    Given I have a smartwatch
    When I connect the device
    Then data should sync automatically
    And appear in my dashboard

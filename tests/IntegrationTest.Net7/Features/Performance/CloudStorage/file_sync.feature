Feature: Cloud Storage and File Sync
  As a user
  I want to store files in cloud
  So that I can access anywhere

  Scenario: Upload files
    Given I have files to backup
    When I upload to cloud
    Then files should be stored
    And accessible from any device

  Scenario: Sync across devices
    Given I saved file on computer
    When I check my phone
    Then same file should appear
    And thanks to auto-sync

  Scenario: Share files with others
    Given I want to collaborate
    When I share file via link
    Then recipient can access
    And without needing account

  Scenario: Create shared folder
    Given team needs shared space
    When I create shared folder
    Then team members have access
    And can add files

  Scenario: Restore deleted file
    Given I accidentally deleted file
    When I check trash
    Then I can restore file
    And within 30 days

  Scenario: Check storage usage
    Given I want to know capacity
    When I view storage stats
    Then I see used and available space
    And can upgrade if needed

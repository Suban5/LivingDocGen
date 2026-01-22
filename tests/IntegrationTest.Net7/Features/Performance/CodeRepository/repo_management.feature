Feature: Code Repository Management
  As a developer
  I want to manage code repositories
  So that I can collaborate on projects

  Scenario: Create new repository
    Given I am starting new project
    When I create repository
    Then repo should be initialized
    And ready for code

  Scenario: Clone repository
    Given I want to work locally
    When I clone the repo
    Then all files should download to my local machine

  Scenario: Commit changes
    Given I made code changes
    When I commit with message
    Then changes should be saved in version history

  Scenario: Push to remote
    Given I have local commits
    When I push to remote repo
    Then changes should sync to remote server

  Scenario: Create pull request
    Given I completed feature
    When I create pull request
    Then reviewers should be notified
    And can review code

  Scenario: Merge pull request
    Given PR is approved
    When I merge the PR
    Then changes should integrate into main branch

  Scenario: View commit history
    Given repo has commits
    When I view history
    Then I see all changes
    And with timestamps and authors

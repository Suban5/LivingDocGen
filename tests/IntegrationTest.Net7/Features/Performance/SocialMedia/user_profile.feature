Feature: User Profile Management
  As a social media user
  I want to manage my profile
  So that I can express my identity

  Scenario: Create user profile
    Given I have registered an account
    When I add profile picture
    And I fill in bio information
    Then my profile should be created
    And it should be visible to others

  Scenario: Update profile information
    Given I have an existing profile
    When I change my display name
    And I update my location
    Then the changes should be saved
    And reflected in all my posts

  Scenario: Set profile privacy
    Given I am editing my profile settings
    When I set profile visibility to "Friends Only"
    Then only my friends should see my profile
    And strangers should see limited information

  Scenario: Upload cover photo
    Given I am on my profile page
    When I upload a new cover photo
    Then the photo should be displayed
    And it should be optimized for different devices

  Scenario: Add social links
    Given I am editing my profile
    When I add links to my other social accounts
    Then the links should be displayed on profile
    And they should be clickable

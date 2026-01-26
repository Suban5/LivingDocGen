Feature: Social Media Post Creation
  As a user
  I want to create posts
  So that I can share content with my network

  Scenario: Create text post
    Given I am on my timeline
    When I write a status update
    And I click "Post"
    Then the post should appear on my timeline
    And my friends should see it in their feed

  Scenario: Create post with image
    Given I am creating a new post
    When I upload 3 photos
    And I add a caption
    Then the post should be created with images
    And images should be displayed in a gallery

  Scenario: Create post with video
    Given I want to share a video
    When I upload a video file
    And I wait for processing
    Then the video post should be published
    And it should be playable in the feed

  Scenario: Tag friends in post
    Given I am creating a post
    When I mention "@JohnDoe" in the text
    Then John should be tagged in the post
    And he should receive a notification

  Scenario: Add location to post
    Given I am creating a post
    When I add location "Central Park"
    Then the location should be attached to post
    And others can click to see the location

  Scenario: Schedule post for later
    Given I have written a post
    When I schedule it for tomorrow at 9 AM
    Then the post should be saved as draft
    And published automatically at scheduled time

  Scenario: Create poll post
    Given I want to create a poll
    When I add question "What's your favorite color?"
    And I add options "Red", "Blue", "Green"
    Then the poll should be created
    And friends can vote on it

Feature: Notification Management System
  As a user
  I want to receive notifications
  So that I stay updated on activities

  Scenario: Receive like notification
    Given someone likes my post
    When the like is registered
    Then I should receive a notification
    And it should show who liked the post

  Scenario: Receive comment notification
    Given someone comments on my photo
    When the comment is posted
    Then I should get a notification
    And I can click to view the comment

  Scenario: Turn off notifications for post
    Given I have a popular post
    When I disable notifications for that post
    Then I should not receive further notifications About that specific post

  Scenario: Group notifications
    Given I receive 10 likes in 5 minutes
    When notifications are delivered
    Then they should be grouped As "10 people liked your post"

  Scenario: Clear all notifications
    Given I have 50 unread notifications
    When I click "Clear All"
    Then all notifications should be marked as read
    And the notification count should be zero

  Scenario: Customize notification settings
    Given I am in notification settings
    When I turn off "Tag" notifications
    And I keep "Message" notifications on
    Then I should only receive message notifications

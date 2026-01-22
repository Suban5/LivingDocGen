Feature: Friend and Connection Management
  As a user
  I want to manage my connections
  So that I can build my network

  Scenario: Send friend request
    Given I am viewing a user profile
    When I click "Add Friend" button
    Then a friend request should be sent
    And the user should receive a notification

  Scenario: Accept friend request
    Given I have received a friend request
    When I click "Accept"
    Then we should become friends
    And we should see each other's posts

  Scenario: Decline friend request
    Given I have a pending friend request
    When I click "Decline"
    Then the request should be removed
    And no notification should be sent to sender

  Scenario: Unfriend a connection
    Given "Alice" is my friend
    When I unfriend Alice
    Then Alice should be removed from my friends
    And I should be removed from her friends

  Scenario: Block user
    Given I want to block "SpamUser"
    When I block the user
    Then SpamUser cannot send me requests
    And SpamUser cannot see my profile

  Scenario: View mutual friends
    Given I am viewing Bob's profile
    When I click "Mutual Friends"
    Then I should see list of common friends
    And their profile pictures

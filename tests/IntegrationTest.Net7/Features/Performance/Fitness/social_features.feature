Feature: Social Fitness Features
  As a user
  I want to connect with other users
  So that we can motivate each other

  Scenario: Add fitness friends
    Given I know someone's username
    When I send friend request
    Then they should receive notification
    And we can share activities

  Scenario: Share workout achievement
    Given I completed a milestone
    When I share to feed
    Then friends should see my achievement
    And can like and comment

  Scenario: Join fitness challenge
    Given there's a running challenge
    When I join the challenge
    Then my activities should count toward it
    And I can see leaderboard

  Scenario: Create group workout
    Given I want to workout with friends
    When I create group session
    Then friends should be invited
    And we can video chat during workout

  Scenario: Give kudos to friends
    Given friend completed a workout
    When I give them kudos
    Then they should receive encouragement
    And feel motivated

  Scenario: Compare stats with friends
    Given I want to see how I'm doing
    When I view friend comparison
    Then I should see relative performance in various metrics

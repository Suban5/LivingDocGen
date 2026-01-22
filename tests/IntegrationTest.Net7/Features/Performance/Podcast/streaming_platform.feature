Feature: Podcast Streaming Platform
  As a podcast listener
  I want to listen to podcasts
  So that I can learn and be entertained

  Scenario: Browse podcast categories
    Given I am exploring podcasts
    When I browse categories
    Then I should see podcasts Organized by topic

  Scenario: Subscribe to podcast
    Given I found interesting podcast
    When I subscribe
    Then new episodes should appear In my feed automatically

  Scenario: Download episode
    Given I will be offline
    When I download episodes
    Then they should save to device For offline listening

  Scenario: Play podcast episode
    Given I selected episode
    When I press play
    Then audio should stream 
    And show progress

  Scenario: Adjust playback speed
    Given I want to listen faster
    When I change speed to 1.5x
    Then playback should speed up Without distorting audio

  Scenario: Create playlist
    Given I have favorite episodes
    When I create playlist
    Then I can organize episodes
    And play them in sequence

  Scenario: Leave episode review
    Given I loved an episode
    When I write review
    Then rating and comment posted
    And visible to others

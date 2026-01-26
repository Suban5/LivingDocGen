Feature: Video Streaming Service
  As a subscriber
  I want to watch videos
  So that I can enjoy entertainment

  Scenario: Browse content library
    Given I am on the home page
    When I browse available content
    Then I should see movies and TV shows
    And personalized recommendations

  Scenario: Search for specific title
    Given I want to watch "Breaking Bad"
    When I use the search function
    Then I should find the show
    And see all available seasons

  Scenario: Start watching a movie
    Given I selected a movie
    When I click "Play"
    Then the movie should start streaming
    And playback controls should be visible

  Scenario: Pause and resume playback
    Given I am watching a movie
    When I pause the video
    And I resume after 10 minutes
    Then playback should continue from same position
    And my watch progress should be saved

  Scenario: Change video quality
    Given I am streaming content
    When I change quality to "1080p"
    Then video should adjust to HD
    And maintain smooth playback

  Scenario: Add to my list
    Given I found an interesting show
    When I click "Add to My List"
    Then it should be saved
    And appear in my watchlist

  Scenario: Download for offline viewing
    Given I want to watch content offline
    When I download a movie
    Then it should save to my device
    And be available without internet

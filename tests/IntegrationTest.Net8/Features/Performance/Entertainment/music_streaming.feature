Feature: Music Streaming Platform
  As a music lover
  I want to stream music
  So that I can listen to my favorite songs

  Scenario: Search for songs
    Given I am on the music app
    When I search for "Imagine Dragons"
    Then I should see their songs and albums
    And be able to play them

  Scenario: Create playlist
    Given I want to organize my music
    When I create a new playlist "Workout Mix"
    And I add 10 songs
    Then the playlist should be saved
    And accessible from my library

  Scenario: Play album
    Given I found an album I like
    When I click "Play Album"
    Then all songs should play in order
    And album art should be displayed

  Scenario: Shuffle play
    Given I am playing a playlist
    When I enable shuffle mode
    Then songs should play in random order
    And each song plays only once

  Scenario: Download songs for offline listening
    Given I have a premium subscription
    When I download my favorite playlist
    Then songs should be available offline
    And play without data connection

  Scenario: Create radio station
    Given I like a particular artist
    When I create a radio station
    Then similar music should play automatically
    And I can discover new artists

  Scenario: Share song with friends
    Given I am listening to a great song
    When I click "Share"
    Then I can send link to friends via social media or messaging

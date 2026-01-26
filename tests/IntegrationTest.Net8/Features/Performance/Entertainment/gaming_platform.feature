Feature: Online Gaming Platform
  As a gamer
  I want to play online games
  So that I can have fun

  Scenario: Browse game library
    Given I am logged into gaming platform
    When I browse the game store
    Then I should see available games
    And their ratings and reviews

  Scenario: Purchase and download game
    Given I want to buy a new game
    When I complete the purchase
    Then the game should start downloading
    And I should receive receipt

  Scenario: Launch game
    Given I have a game installed
    When I click "Play"
    Then the game should launch
    And load my saved progress

  Scenario: Join multiplayer match
    Given I am in an online game
    When I join a multiplayer match
    Then I should be connected to server
    And matched with other players

  Scenario: Earn achievement
    Given I completed a difficult level
    When the achievement unlocks
    Then it should be added to my profile
    And friends should see the notification

  Scenario: Voice chat with teammates
    Given I am in a team game
    When I enable voice chat
    Then I should hear my teammates
    And they should hear me

  Scenario: View game statistics
    Given I have been playing for a while
    When I check my stats
    Then I should see playtime, wins, losses
    And ranking on leaderboard

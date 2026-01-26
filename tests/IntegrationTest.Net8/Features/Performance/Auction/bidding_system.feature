Feature: Auction Listing and Bidding
  As an auction participant
  I want to bid on items
  So that I can purchase goods

  Scenario: List item for auction
    Given I want to sell an item
    When I create auction listing
    And I set starting bid and duration
    Then auction should go live
    And appear in search results

  Scenario: Place bid on item
    Given I found item I want
    When I place a bid
    Then bid should be registered
    And I should be current high bidder

  Scenario: Receive outbid notification
    Given I was high bidder
    When someone bids higher
    Then I should be notified
    And can place new bid

  Scenario: Automatic bidding
    Given I don't want to monitor constantly
    When I set maximum bid amount
    Then system should auto-bid for me Up to my maximum

  Scenario: Win auction
    Given auction ended
    And I was high bidder
    Then I should be declared winner
    And payment should be processed

  Scenario: Watch items of interest
    Given I am interested in item
    When I add to watchlist
    Then I should receive updates on bidding activity

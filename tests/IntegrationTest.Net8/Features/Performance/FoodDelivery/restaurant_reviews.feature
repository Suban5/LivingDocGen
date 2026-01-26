Feature: Restaurant and Delivery Reviews
  As a customer
  I want to leave reviews
  So that I can help others decide

  Scenario: Rate restaurant
    Given I received my order
    When I rate restaurant 5 stars
    And I rate food quality
    Then rating should be recorded
    And contribute to restaurant score

  Scenario: Rate delivery experience
    Given order was delivered
    When I rate delivery driver
    And I comment on timeliness
    Then driver rating should be updated
    And feedback recorded

  Scenario: Upload food photos
    Given I am writing a review
    When I upload photo of the food
    Then photo should be attached
    And visible to other customers

  Scenario: Flag inappropriate review
    Given I see a fake review
    When I report it
    Then review should be flagged
    And investigated by moderators

  Scenario: View my review history
    Given I have written multiple reviews
    When I access my reviews
    Then I should see all past reviews
    And restaurants I reviewed

  Scenario: Edit previous review
    Given I wrote a review last week
    When I update my rating
    Then new rating should replace old one
    And timestamp should update

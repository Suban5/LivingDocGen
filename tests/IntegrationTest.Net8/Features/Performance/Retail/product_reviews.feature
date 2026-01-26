Feature: Product Review System
  As a customer
  I want to read and write reviews
  So that I can make informed decisions

  Scenario: Write product review
    Given I purchased a product
    When I write a review with 5 stars
    And I add comments
    Then the review should be published
    And appear on product page

  Scenario: Upload photos with review
    Given I am writing a review
    When I upload 3 product photos
    Then photos should be attached
    And visible to other customers

  Scenario: Mark review as helpful
    Given I found a helpful review
    When I click "Helpful" button
    Then the vote should be counted
    And review ranking should update

  Scenario: Filter reviews by rating
    Given I am reading product reviews
    When I filter by "5 stars"
    Then only 5-star reviews should show
    And count should be displayed

  Scenario: Report inappropriate review
    Given I see a fake review
    When I report it
    Then the report should be submitted
    And review team should investigate

  Scenario: Verify purchase badge
    Given I wrote a review
    And I actually bought the product
    Then my review should show "Verified Purchase" Badge for credibility

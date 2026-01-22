Feature: News Article Publishing
  As a journalist
  I want to publish articles
  So that readers stay informed

  Scenario: Write new article
    Given I am a staff writer
    When I write an article
    And I add images
    Then article should be saved as draft
    And ready for editing

  Scenario: Submit for editorial review
    Given article is complete
    When I submit for review
    Then editor should be notified
    And can provide feedback

  Scenario: Publish article
    Given article is approved
    When I publish the article
    Then it should appear on website
    And be available to readers

  Scenario: Schedule article publication
    Given article is ready
    When I schedule for tomorrow
    Then article should publish automatically at specified time

  Scenario: Update published article
    Given article has errors
    When I make corrections
    Then updates should be published
    And revision noted

Feature: News Reader Experience
  As a news reader
  I want to consume news content
  So that I stay informed

  Scenario: Browse news by category
    Given I am on news website
    When I select "Technology" category
    Then I should see tech articles
    And sorted by recency

  Scenario: Read full article
    Given I found interesting headline
    When I click to read
    Then full article should display with images and formatting

  Scenario: Save article for later
    Given I don't have time now
    When I bookmark article
    Then it should be saved to my reading list

  Scenario: Share article on social media
    Given I read great article
    When I click share button
    Then I can post to social platforms with article link

  Scenario: Subscribe to newsletter
    Given I like the content
    When I subscribe to newsletter
    Then I should receive Daily or weekly digest

  Scenario: Comment on article
    Given I have opinions on article
    When I write a comment
    Then comment should be posted
    And others can reply

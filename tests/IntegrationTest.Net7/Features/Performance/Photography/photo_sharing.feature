Feature: Photo Sharing and Social Network
  As a photographer
  I want to share photos
  So that others can see my work

  Scenario: Upload photo
    Given I took a great photo
    When I upload to platform
    And I add caption and tags
    Then photo should be published
    And visible to followers

  Scenario: Apply filters
    Given I am editing photo
    When I apply vintage filter
    Then photo should be enhanced
    And preview should update

  Scenario: Like photos
    Given I see amazing photo
    When I double-tap to like
    Then like should be registered
    And creator should be notified

  Scenario: Comment on photo
    Given I want to give feedback
    When I write a comment
    Then comment should appear Under the photo

  Scenario: Follow other users
    Given I found interesting photographer
    When I follow them
    Then their photos should appear In my feed

  Scenario: Create photo album
    Given I have related photos
    When I create album
    Then photos should be grouped
    And shareable as collection

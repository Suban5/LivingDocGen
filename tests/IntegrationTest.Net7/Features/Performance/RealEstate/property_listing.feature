Feature: Property Listing Management
  As a real estate agent
  I want to list properties
  So that buyers can find them

  Scenario: Create new property listing
    Given I am a registered agent
    When I create a listing for a house
    And I add details, photos, and price
    Then the listing should be published
    And appear in search results

  Scenario: Edit existing listing
    Given I have an active listing
    When I update the price
    And I add more photos
    Then the changes should be saved
    And reflected immediately

  Scenario: Mark property as sold
    Given a property has been sold
    When I mark it as sold
    Then the listing should be archived
    And removed from active listings

  Scenario: Feature premium listing
    Given I want more visibility
    When I upgrade to featured listing
    Then property should appear at top
    And get more views

  Scenario: Schedule open house
    Given I have a property listing
    When I schedule open house for Saturday
    Then event should be added to listing
    And interested buyers notified

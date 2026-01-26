Feature: API Documentation Platform
  As an API developer
  I want to document APIs
  So that developers can integrate

  Scenario: Create API documentation
    Given I built an API
    When I document endpoints
    Then docs should be generated
    And published

  Scenario: Interactive API explorer
    Given docs are published
    When developer views endpoint
    Then they can test it
    And directly in browser

  Scenario: View code examples
    Given endpoint is documented
    When developer selects language
    Then code example shown
    And in that language

  Scenario: Search documentation
    Given docs contain many endpoints
    When developer searches
    Then relevant endpoints found
    And with descriptions

  Scenario: Version documentation
    Given API has versions
    When developer selects version
    Then appropriate docs shown
    And for that version

  Scenario: Test API endpoint
    Given I want to try endpoint
    When I enter parameters
    Then request is sent
    And response displayed

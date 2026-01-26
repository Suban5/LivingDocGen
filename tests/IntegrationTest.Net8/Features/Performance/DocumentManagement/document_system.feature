Feature: Document Management System
  As a business user
  I want to manage documents
  So that information is organized

  Scenario: Upload document
    Given I have a document to store
    When I upload the file
    And I add metadata
    Then document should be saved and searchable

  Scenario: Organize in folders
    Given I have multiple documents
    When I create folder structure
    Then documents can be organized by category or project

  Scenario: Search for documents
    Given I need to find document
    When I search by keyword
    Then matching documents shown
    And with relevance ranking

  Scenario: Share document
    Given I want to collaborate
    When I share document with colleague
    Then they should receive access
    And can view or edit

  Scenario: Version control
    Given document is being edited
    When changes are saved
    Then new version should be created
    And history maintained

  Scenario: Set permissions
    Given document is confidential
    When I set access permissions
    Then only authorized users can view or edit

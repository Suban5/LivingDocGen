Feature: E-Book Library Management
  As a reader
  I want to access ebooks
  So that I can read digitally

  Scenario: Browse ebook catalog
    Given I am in library
    When I browse books
    Then I see available titles by category

  Scenario: Search for book
    Given I want specific book
    When I search by title or author
    Then matching books appear with availability status

  Scenario: Borrow ebook
    Given book is available
    When I borrow it
    Then book downloads
    And loan period starts

  Scenario: Read ebook
    Given I borrowed book
    When I open it
    Then book displays
    And I can read

  Scenario: Add bookmark
    Given I am reading
    When I bookmark page
    Then bookmark saved
    And I can return later

  Scenario: Return ebook early
    Given I finished book
    When I return it
    Then it becomes available for others to borrow

  Scenario: Request book
    Given book is checked out
    When I place hold
    Then I am added to queue
    And notified when available

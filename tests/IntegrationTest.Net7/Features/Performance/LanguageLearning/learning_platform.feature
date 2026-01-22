Feature: Language Learning Platform
  As a language learner
  I want to learn new language
  So that I can communicate

  Scenario: Start language course
    Given I want to learn Spanish
    When I enroll in course
    Then lessons should be available
    And progress tracking starts

  Scenario: Complete daily lesson
    Given I have lesson today
    When I complete the exercises
    Then progress should be recorded
    And streak maintained

  Scenario: Practice vocabulary
    Given I learned new words
    When I practice flashcards
    Then I should review words using spaced repetition

  Scenario: Take proficiency test
    Given I completed beginner level
    When I take assessment
    Then my level should be evaluated
    And certificate issued if passed

  Scenario: Practice with native speakers
    Given I want conversation practice
    When I join language exchange
    Then I should be matched
    And with native speaker

  Scenario: Track learning progress
    Given I have been learning
    When I view progress dashboard
    Then I should see lessons completed
    And skills mastered

Feature: Performance Review System
  As a manager
  I want to conduct performance reviews
  So that employees receive feedback

  Scenario: Initiate review cycle
    Given it's review season
    When I start annual reviews
    Then review forms should be generated for all team members

  Scenario: Complete self-assessment
    Given I have a pending review
    When I complete self-assessment
    Then answers should be saved
    And submitted to manager

  Scenario: Write performance review
    Given employee completed self-assessment
    When I write the review
    And I rate performance areas
    Then review should be saved
    And ready for discussion

  Scenario: Conduct review meeting
    Given review is completed
    When I schedule review meeting
    Then employee should be notified
    And meeting time confirmed

  Scenario: Set performance goals
    Given review is complete
    When I set goals for next period
    Then goals should be documented
    And tracked for next review

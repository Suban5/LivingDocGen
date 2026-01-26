Feature: Laboratory Test Management
  As a healthcare provider
  I want to manage lab tests
  So that patients receive proper diagnosis

  Scenario: Order blood test
    Given I am treating a patient
    When I order a "Complete Blood Count" test
    Then the lab order should be created
    And patient should be informed

  Scenario: Receive lab results
    Given lab has completed the tests
    When results are uploaded to system
    Then I should be notified
    And results should appear in patient record

  Scenario: Order multiple tests
    Given patient needs comprehensive checkup
    When I order 5 different tests
    Then all tests should be bundled
    And patient pays once for all

  Scenario: Critical result alert
    Given a lab result shows critical values
    When the result is uploaded
    Then doctor should get urgent alert
    And patient should be contacted immediately

  Scenario: Track test sample
    Given blood sample is collected
    When sample is sent to lab
    Then I can track sample status
    And see when results will be ready

  Scenario: Request test report copy
    Given I have test results in system
    When patient requests a copy
    Then report should be generated
    And sent to patient's email

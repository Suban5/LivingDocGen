Feature: Insurance Claims Processing
  As a claims adjuster
  I want to process claims
  So that customers receive compensation

  Scenario: Review new claim
    Given a claim was submitted
    When I review the documentation
    Then I should assess damage extent
    And determine claim validity

  Scenario: Approve claim payment
    Given claim is validated
    When I approve the claim
    Then payment should be processed
    And customer should be notified

  Scenario: Request additional information
    Given claim lacks documentation
    When I request more information
    Then customer should receive request
    And claim should be paused

  Scenario: Deny fraudulent claim
    Given claim appears fraudulent
    When investigation confirms fraud
    Then claim should be denied
    And appropriate action taken

  Scenario: Schedule damage inspection
    Given claim requires assessment
    When I schedule inspection
    Then inspector should be assigned
    And customer should be contacted

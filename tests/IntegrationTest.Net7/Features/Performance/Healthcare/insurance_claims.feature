Feature: Insurance Claims Processing
  As a billing administrator
  I want to process insurance claims
  So that payments are received

  Scenario: Submit insurance claim
    Given a patient received treatment
    When I submit claim to insurance company
    Then the claim should be sent electronically
    And I should receive confirmation number

  Scenario: Verify insurance eligibility
    Given a patient provides insurance card
    When I verify the insurance
    Then I should see coverage details and co-pay amount

  Scenario: Process approved claim
    Given insurance company approved a claim
    When I receive the approval
    Then payment should be recorded
    And patient balance should be updated

  Scenario: Handle rejected claim
    Given insurance company rejected a claim
    When I receive rejection notice
    Then I should review rejection reason
    And resubmit with corrections

  Scenario: Check claim status
    Given I submitted a claim last week
    When I check claim status
    Then I should see current processing stage
    And expected payment date

  Scenario: Generate claim report
    Given I need monthly claim statistics
    When I generate the report
    Then I should see total claims submitted
    And approval vs rejection rates

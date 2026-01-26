Feature: Electronic Prescription Management
  As a doctor
  I want to manage prescriptions digitally
  So that patients receive accurate medication

  Scenario: Create new prescription
    Given I am treating a patient
    When I prescribe "Amoxicillin 500mg"
    And I specify "Take 3 times daily for 7 days"
    Then the prescription should be created
    And sent to patient's pharmacy

  Scenario: Add multiple medications
    Given I am creating a prescription
    When I add 3 different medications
    Then all medications should be listed
    And drug interactions should be checked

  Scenario: Prescription with allergies check
    Given patient is allergic to Penicillin
    When I try to prescribe Amoxicillin
    Then the system should show a warning
    And suggest alternative medications

  Scenario: Refill existing prescription
    Given patient requests prescription refill
    When I review the request
    And I approve the refill
    Then the pharmacy should be notified
    And patient can collect medication

  Scenario: Send prescription to preferred pharmacy
    Given patient has preferred pharmacy "CVS Main St"
    When I issue a prescription
    Then it should be sent to that pharmacy
    And patient should receive SMS notification

  Scenario: View prescription history
    Given I am reviewing patient file
    When I access prescription history
    Then I should see all past prescriptions
    And their fulfillment status

Feature: Patient Registration and Management
  As a healthcare provider
  I want to register patients
  So that we can maintain medical records

  Scenario: Register new patient
    Given a new patient arrives at hospital
    When I enter patient details
    And I collect insurance information
    Then a patient record should be created
    And a unique patient ID should be generated

  Scenario: Update patient information
    Given patient "John Smith" is registered
    When I update his contact number
    And I update his address
    Then the changes should be saved
    And reflected in all systems

  Scenario: Link family members
    Given patient "Mary Johnson" is registered
    When I add her child as dependent
    Then family link should be established
    And family medical history should be accessible

  Scenario: View patient medical history
    Given I am viewing patient record
    When I access medical history tab
    Then I should see all past visits
    And all previous diagnoses

  Scenario: Mark patient as VIP
    Given a patient is a premium member
    When I mark them as VIP
    Then they should get priority service
    And special flags should appear in system

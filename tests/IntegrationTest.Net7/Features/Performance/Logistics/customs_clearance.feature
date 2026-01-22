Feature: International Shipping and Customs
  As a logistics coordinator
  I want to handle international shipments
  So that goods clear customs smoothly

  Scenario: Generate customs documentation
    Given I am shipping internationally
    When I create customs declaration
    And I enter item values and descriptions
    Then required forms should be generated
    And attached to shipment

  Scenario: Calculate duties and taxes
    Given shipment value is $1000
    When I check destination country rates
    Then duties should be calculated
    And customer should be informed

  Scenario: Track customs clearance
    Given package reached customs
    When I check clearance status
    Then I should see processing stage
    And estimated clearance time

  Scenario: Handle customs hold
    Given package is held at customs
    When I receive hold notice
    Then I should see reason for hold
    And required actions

  Scenario: Provide additional documentation
    Given customs requested more info
    When I upload required documents
    Then documents should be submitted
    And clearance should proceed

  Scenario: Process customs release
    Given customs approved shipment
    When release is confirmed
    Then package should proceed to delivery
    And customer should be notified

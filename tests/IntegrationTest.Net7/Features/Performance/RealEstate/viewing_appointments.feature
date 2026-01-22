Feature: Property Viewing Appointments
  As a potential buyer
  I want to schedule viewings
  So that I can see properties in person

  Scenario: Request property viewing
    Given I am interested in a property
    When I request a viewing
    And I select preferred time slot
    Then request should be sent to agent
    And I should receive confirmation

  Scenario: Virtual tour
    Given property offers virtual tour
    When I start the virtual tour
    Then I should see 360-degree views Of all rooms

  Scenario: Reschedule viewing
    Given I have a scheduled viewing
    When I request to reschedule
    Then agent should be notified
    And new time should be confirmed

  Scenario: Cancel viewing appointment
    Given I no longer interested
    When I cancel the viewing
    Then appointment should be removed
    And agent should be informed

  Scenario: Add notes during viewing
    Given I am at a property viewing
    When I take notes in app
    Then notes should be saved
    And linked to the property

  Scenario: Rate property after viewing
    Given I completed a viewing
    When I rate the property
    Then rating should be saved privately
    And help me compare later

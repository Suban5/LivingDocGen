Feature: Smart Home Device Control
  As a homeowner
  I want to control smart devices
  So that I can automate my home

  Scenario: Control smart lights
    Given I have smart bulbs installed
    When I turn on living room lights
    Then lights should turn on
    And brightness can be adjusted

  Scenario: Adjust thermostat
    Given I have smart thermostat
    When I set temperature to 72°F
    Then heating/cooling should adjust To reach target temperature

  Scenario: Lock smart door lock
    Given I have smart lock
    When I leave home
    Then I can lock door remotely
    And receive confirmation

  Scenario: View security camera
    Given I have security cameras
    When I open camera feed
    Then I should see live video From all cameras

  Scenario: Create automation routine
    Given I want automated actions
    When I create "Good Morning" routine
    Then lights, thermostat, and coffee maker Should activate at set time

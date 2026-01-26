Feature: Fleet and Vehicle Management
  As a fleet manager
  I want to manage delivery vehicles
  So that operations run smoothly

  Scenario: Assign vehicle to driver
    Given a driver is starting shift
    When I assign vehicle
    Then driver should receive vehicle details
    And vehicle status should be "In Use"

  Scenario: Track vehicle location
    Given vehicle is on delivery route
    When I check vehicle location
    Then I should see real-time GPS position
    And current route

  Scenario: Schedule vehicle maintenance
    Given vehicle reached 10000 miles
    When I schedule maintenance
    Then vehicle should be taken offline
    And alternative vehicle assigned

  Scenario: Log fuel consumption
    Given driver refueled vehicle
    When I log fuel purchase
    Then fuel cost should be recorded
    And MPG calculated

  Scenario: Report vehicle issue
    Given vehicle has mechanical problem
    When driver reports issue
    Then maintenance should be notified
    And vehicle status updated

  Scenario: Optimize delivery routes
    Given I have 20 deliveries today
    When I optimize routes
    Then most efficient path should be calculated
    And assigned to drivers

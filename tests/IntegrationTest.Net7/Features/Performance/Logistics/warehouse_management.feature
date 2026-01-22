Feature: Warehouse Operations Management
  As a warehouse manager
  I want to manage warehouse operations
  So that inventory flows efficiently

  Scenario: Receive incoming goods
    Given a shipment arrived at warehouse
    When I scan and verify items
    Then items should be added to inventory
    And location should be assigned

  Scenario: Pick items for order
    Given an order needs fulfillment
    When I generate pick list
    Then optimal route should be shown
    And items located efficiently

  Scenario: Pack order for shipping
    Given items are picked
    When I pack the order
    And I print shipping label
    Then order should be ready to ship
    And tracking should be activated

  Scenario: Cycle count inventory
    Given it's time for cycle count
    When I count items in section A
    Then counts should be recorded
    And discrepancies flagged

  Scenario: Move inventory between locations
    Given items need relocation
    When I transfer to different zone
    Then system should update locations
    And movement should be logged

  Scenario: Process returns
    Given customer returned items
    When I inspect returned goods
    Then items should be restocked or disposed
    And inventory adjusted

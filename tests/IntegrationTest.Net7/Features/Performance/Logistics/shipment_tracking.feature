Feature: Shipment Tracking System
  As a logistics coordinator
  I want to track shipments
  So that I can monitor deliveries

  Scenario: Create new shipment
    Given I need to ship a package
    When I create shipment record
    And I enter pickup and delivery addresses
    Then shipment should be registered
    And tracking number generated

  Scenario: Track package location
    Given a package is in transit
    When I enter tracking number
    Then I should see current location
    And shipment history

  Scenario: Update shipment status
    Given package reached a checkpoint
    When status is updated to "In Transit"
    Then customer should be notified
    And tracking page should update

  Scenario: Estimate delivery date
    Given package is shipped
    When I check delivery estimate
    Then expected date should be shown
    And based on shipping method

  Scenario: Handle delivery exception
    Given package cannot be delivered
    When I mark exception reason
    Then customer should be informed
    And next steps provided

  Scenario: Confirm delivery
    Given package reached destination
    When recipient signs for package
    Then delivery should be confirmed
    And proof of delivery saved

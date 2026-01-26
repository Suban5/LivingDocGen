Feature: Reverse Logistics and Returns
  As a returns coordinator
  I want to manage product returns
  So that reverse logistics is efficient

  Scenario: Generate return label
    Given customer wants to return item
    When I create return authorization
    Then return label should be generated
    And emailed to customer

  Scenario: Track return shipment
    Given customer shipped return
    When I track the return
    Then I should see return progress
    And estimated arrival at warehouse

  Scenario: Receive returned item
    Given return arrived at warehouse
    When I scan return barcode
    Then return should be matched to order
    And inspection initiated

  Scenario: Inspect returned item
    Given I am inspecting return
    When I assess item condition
    Then I should categorize the return as restock, refurbish, or dispose

  Scenario: Process return refund
    Given return is approved
    When I process the refund
    Then money should be returned
    And customer notified

  Scenario: Restock returned item
    Given returned item is in good condition
    When I restock it
    Then item should return to inventory
    And be available for sale

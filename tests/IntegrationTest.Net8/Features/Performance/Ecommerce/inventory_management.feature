Feature: Inventory Management System
  As a warehouse manager
  I want to manage product inventory
  So that we maintain optimal stock levels

  Scenario: Automatic low stock alert
    Given a product has minimum threshold of 10 units
    When the stock level drops to 9 units
    Then a low stock alert should be triggered
    And the purchasing team should be notified

  Scenario: Receive inventory shipment
    Given a shipment of 200 units is arriving
    When the warehouse receives the shipment
    And the manager confirms the receipt
    Then the inventory should be updated
    And the stock level should increase by 200

  Scenario: Process inventory return
    Given a customer returns a defective product
    When the warehouse receives the returned item
    Then the inventory should be adjusted
    And the item should be marked for quality inspection

  Scenario: Real-time inventory sync across channels
    Given a product is sold on website
    When the order is confirmed
    Then inventory should be updated in real-time
    And the same count should reflect in mobile app

  Scenario: Inventory audit and reconciliation
    Given the system shows 150 units in stock
    When the warehouse performs physical count
    And finds 148 units
    Then a discrepancy report should be generated
    And the system should be adjusted to actual count

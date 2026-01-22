Feature: Product Catalog Management
  As a store administrator
  I want to manage product catalog
  So that customers can browse and purchase products

  Background:
    Given the admin is logged into the system
    And the product database is initialized

  Scenario: Add new product to catalog
    When the admin adds a new product with name "Wireless Mouse"
    And sets the price to "$29.99"
    And sets the stock quantity to 100
    Then the product should appear in the catalog
    And the product status should be "Active"

  Scenario: Update product information
    Given a product "Gaming Keyboard" exists in the catalog
    When the admin updates the price to "$89.99"
    And updates the description to "RGB Mechanical Gaming Keyboard"
    Then the product information should be updated
    And customers should see the new price

  Scenario: Remove product from catalog
    Given a product "Old Model Mouse" exists in the catalog
    When the admin removes the product from catalog
    Then the product should be marked as "Discontinued"
    And the product should not appear in customer searches

  Scenario: Bulk import products
    When the admin uploads a CSV file with 50 products
    Then all 50 products should be imported successfully
    And each product should have a unique SKU

  Scenario: Search products by category
    Given the catalog contains 100 products across 10 categories
    When a customer searches for "Electronics"
    Then only products in "Electronics" category should be displayed
    And results should be sorted by popularity

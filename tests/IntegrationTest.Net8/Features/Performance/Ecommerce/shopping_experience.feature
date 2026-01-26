Feature: Enhanced Shopping Experience
  As a customer
  I want a seamless shopping experience
  So that I can easily find and purchase products

  Scenario: Product quick view
    Given I am browsing the product catalog
    When I hover over a product "Laptop Stand"
    Then a quick view popup should appear
    And I should see product image, price, and ratings

  Scenario: Add to wishlist
    Given I am viewing a product "Professional Camera"
    When I click the "Add to Wishlist" button
    Then the product should be saved to my wishlist
    And I should see a confirmation message

  Scenario: Compare products
    Given I have selected 3 products for comparison
    When I click "Compare Products"
    Then I should see a comparison table
    And the table should show features side by side

  Scenario: Apply filters to search results
    Given I am on the search results page
    When I apply filters for price range "$50-$100"
    And I select brand "TechBrand"
    Then only matching products should be displayed
    And the filter count should show applied filters

  Scenario: Product recommendations
    Given I have viewed 5 products in "Gaming" category
    When I scroll to the bottom of the page
    Then I should see personalized product recommendations
    And recommendations should be relevant to my browsing history

Feature: Returns and Refunds Processing
  As a customer
  I want to return products
  So that I can get refunds for unsuitable items

  Scenario: Initiate return request
    Given I received an unsatisfactory product
    When I request a return
    And I provide reason
    Then return request should be created
    And I should receive return label

  Scenario: Print return shipping label
    Given my return is approved
    When I print the return label
    Then I can attach it to package
    And ship back the product

  Scenario: Track return shipment
    Given I shipped the return
    When I check return status
    Then I should see tracking information
    And estimated processing time

  Scenario: Receive refund
    Given my return was received and inspected
    When the return is approved
    Then refund should be processed
    And money returned to original payment method

  Scenario: Exchange instead of refund
    Given I want different size
    When I request exchange
    Then new item should be shipped
    And I return old item

  Scenario: Partial refund for damaged item
    Given product is partially damaged
    When I report the issue
    Then partial refund should be offered
    And I keep the product

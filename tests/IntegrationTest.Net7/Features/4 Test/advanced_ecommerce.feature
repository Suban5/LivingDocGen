# language: en
# This is a comment at the top of the file
# Comments should be preserved or at least documented

@api @integration @high-priority
Feature: Advanced E-commerce Order Processing
  As a customer
  I want to place orders with various payment methods and shipping options
  So that I can receive my products conveniently

  # Background runs before each scenario
  Background: Setup test environment
    Given the e-commerce platform is running
    And the following products are available in inventory:
      | Product ID | Name                | Price  | Stock | Category    |
      | PROD-001   | Laptop Pro 15"      | 999.99 | 50    | Electronics |
      | PROD-002   | Wireless Mouse      | 29.99  | 200   | Accessories |
      | PROD-003   | USB-C Cable         | 14.99  | 500   | Accessories |
      | PROD-004   | 4K Monitor          | 499.99 | 30    | Electronics |
      | PROD-005   | Mechanical Keyboard | 89.99  | 75    | Accessories |
    And the payment gateway is online
    And the shipping service is available
    # Database is seeded with test data

  @smoke @checkout
  Scenario: Complete order with credit card and express shipping
    # This scenario tests the happy path for checkout
    Given I am logged in as "premium.customer@example.com"
    And my shopping cart contains:
      | Product ID | Quantity |
      | PROD-001   | 1        |
      | PROD-002   | 2        |
    When I proceed to checkout
    And I select shipping address:
      """
      John Doe
      123 Main Street, Apt 4B
      San Francisco, CA 94102
      United States
      """
    And I choose "Express Shipping" delivery method
    And I enter payment details:
      | Field          | Value                |
      | Card Number    | 4532-1234-5678-9010 |
      | Expiry Date    | 12/2027             |
      | CVV            | 123                 |
      | Billing ZIP    | 94102               |
    And I apply promotional code "SAVE10"
    And I confirm the order
    Then the order should be placed successfully
    And I should receive order confirmation email
    And the order total should be:
      """json
      {
        "subtotal": 1059.97,
        "discount": 106.00,
        "shipping": 25.00,
        "tax": 85.80,
        "total": 1064.77
      }
      """
    And the inventory should be updated:
      | Product ID | New Stock |
      | PROD-001   | 49        |
      | PROD-002   | 198       |

  @data-driven @payment-methods
  Scenario Outline: Process orders with different payment methods and customer types
    # Testing multiple payment scenarios with different user types
    Given I am logged in as a "<customer_type>" customer with email "<email>"
    And I have "<loyalty_points>" loyalty points
    When I add product "<product_id>" with quantity <quantity> to cart
    And I proceed to checkout with "<payment_method>" payment
    And I select "<shipping_method>" shipping
    Then the order should be "<order_status>"
    And I should see "<notification_type>" notification
    And my loyalty points should be "<final_points>"
    And the estimated delivery should be within "<delivery_days>" days

    # First set of examples for premium customers
    @premium-customers
    Examples: Premium Customers with Various Payment Methods
      | customer_type | email                    | loyalty_points | product_id | quantity | payment_method | shipping_method | order_status | notification_type | final_points | delivery_days |
      | premium       | premium1@example.com     | 5000           | PROD-001   | 1        | Credit Card    | Express         | confirmed    | email_sms         | 6000         | 1-2           |
      | premium       | premium2@example.com     | 3500           | PROD-004   | 2        | PayPal         | Express         | confirmed    | email_sms         | 4500         | 1-2           |
      | premium       | premium3@example.com     | 10000          | PROD-005   | 3        | Apple Pay      | Standard        | confirmed    | email_sms         | 10270        | 3-5           |
      | premium       | vip@example.com          | 25000          | PROD-001   | 5        | Credit Card    | Same Day        | confirmed    | email_sms_push    | 30000        | same-day      |

    # Second set of examples for regular customers
    @regular-customers
    Examples: Regular Customers with Standard Payment
      | customer_type | email                    | loyalty_points | product_id | quantity | payment_method | shipping_method | order_status | notification_type | final_points | delivery_days |
      | regular       | user1@example.com        | 100            | PROD-002   | 1        | Credit Card    | Standard        | confirmed    | email             | 130          | 3-5           |
      | regular       | user2@example.com        | 0              | PROD-003   | 5        | Debit Card     | Standard        | confirmed    | email             | 75           | 3-5           |
      | regular       | user3@example.com        | 500            | PROD-005   | 1        | PayPal         | Economy         | confirmed    | email             | 590          | 5-7           |

    # Third set for edge cases and failures
    @edge-cases
    Examples: Edge Cases and Payment Failures
      | customer_type | email                    | loyalty_points | product_id | quantity | payment_method | shipping_method | order_status | notification_type | final_points | delivery_days |
      | regular       | failcard@example.com     | 200            | PROD-001   | 1        | Credit Card    | Express         | failed       | error             | 200          | N/A           |
      | guest         | guest@example.com        | 0              | PROD-003   | 100      | Credit Card    | Standard        | pending      | email             | 0            | 3-5           |
      | suspended     | suspended@example.com    | 1000           | PROD-002   | 1        | Credit Card    | Express         | rejected     | error             | 1000         | N/A           |

  @bulk-order @wholesale
  Scenario: Process large wholesale order with custom pricing
    # Bulk orders get special pricing and handling
    Given I am logged in as a "wholesale" customer
    And I have negotiated pricing agreement "WHOLESALE-2025-A"
    When I create a bulk order with the following items:
      | SKU      | Product Name        | Quantity | Unit Price | Total Price |
      | PROD-001 | Laptop Pro 15"      | 50       | 899.99     | 44999.50    |
      | PROD-002 | Wireless Mouse      | 200      | 24.99      | 4998.00     |
      | PROD-003 | USB-C Cable         | 500      | 12.99      | 6495.00     |
      | PROD-004 | 4K Monitor          | 30       | 449.99     | 13499.70    |
      | PROD-005 | Mechanical Keyboard | 100      | 79.99      | 7999.00     |
      | PROD-006 | Laptop Stand        | 75       | 39.99      | 2999.25     |
      | PROD-007 | Webcam HD           | 150      | 69.99      | 10498.50    |
      | PROD-008 | Headset Pro         | 200      | 89.99      | 17998.00    |
      | PROD-009 | External SSD 1TB    | 100      | 119.99     | 11999.00    |
      | PROD-010 | Dock Station        | 50       | 199.99     | 9999.50     |
    And I request split shipment to multiple warehouses:
      """
      Warehouse 1 (West Coast):
        - 60% of items to San Francisco, CA
      Warehouse 2 (East Coast):
        - 40% of items to New York, NY
      
      Delivery Instructions:
        - Signature required
        - Business hours only (9 AM - 5 PM)
        - Loading dock access needed
      """
    And I choose payment terms "Net 30"
    Then the order should be submitted for approval
    And I should receive quote document with order ID
    And the estimated total should be "$131485.45"
    And the bulk discount should be "15%"
    And the final amount should be "$111762.63"

  @returns @refund @ignore
  Scenario: Process return with partial refund and restocking
    Given I have an existing order "ORD-2025-12345" with status "delivered"
    And the order was delivered 5 days ago
    And the order contained:
      | Product ID | Product Name        | Quantity | Price  | Status    |
      | PROD-001   | Laptop Pro 15"      | 1        | 999.99 | delivered |
      | PROD-002   | Wireless Mouse      | 2        | 29.99  | delivered |
      | PROD-003   | USB-C Cable         | 1        | 14.99  | delivered |
    When I initiate a return for the following items:
      | Product ID | Quantity | Reason                    | Condition |
      | PROD-001   | 1        | Defective screen          | Damaged   |
      | PROD-002   | 1        | Changed mind              | Unopened  |
    And I upload return images:
      """
      - laptop_screen_defect.jpg
      - mouse_unopened_box.jpg
      - shipping_label.pdf
      """
    And I select refund method "Original Payment Method"
    Then the return request should be approved
    And I should receive return shipping label via email
    And the refund amount should be calculated as:
      | Item               | Amount  | Restocking Fee | Refund Amount |
      | Laptop Pro 15"     | 999.99  | 0.00           | 999.99        |
      | Wireless Mouse     | 29.99   | 3.00           | 26.99         |
      | Processing Fee     | -       | -              | -10.00        |
      | Total Refund       | -       | -              | 1016.98       |
    And the inventory should be incremented:
      | Product ID | Increment | New Status      |
      | PROD-001   | 0         | quarantine      |
      | PROD-002   | 1         | available       |

  @subscription @recurring
  Scenario: Setup recurring monthly subscription order
    # Monthly subscription with automatic billing
    Given I am logged in as "subscriber@example.com"
    When I choose a subscription plan with the following configuration:
      """yaml
      subscription_type: monthly
      products:
        - product_id: PROD-003
          quantity: 5
          frequency: every_month
        - product_id: PROD-002
          quantity: 2
          frequency: every_month
      billing:
        payment_method: Credit Card
        billing_day: 1st of month
      shipping:
        method: Standard
        address: Same as account
      discounts:
        - type: subscription_discount
          amount: 20%
        - type: loyalty_bonus
          amount: 5%
      notifications:
        - email: 3 days before charge
        - sms: on shipment
      """
    And I confirm subscription terms and conditions
    Then the subscription should be activated
    And I should see subscription dashboard with:
      | Field                  | Value                    |
      | Status                 | Active                   |
      | Next Billing Date      | 2025-01-01               |
      | Monthly Amount         | $127.48                  |
      | Products per Month     | 7 items                  |
      | Savings per Order      | $31.87 (25%)             |
      | Cancellation Policy    | Cancel anytime           |
    And I should receive welcome email with subscription details
    And a calendar reminder should be created for "2024-12-29"

  # This is a comment at the end
  # Total scenarios: 6
  # Total examples: 13

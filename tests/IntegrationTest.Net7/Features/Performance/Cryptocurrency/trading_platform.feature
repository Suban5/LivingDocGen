Feature: Cryptocurrency Trading Platform
  As a crypto trader
  I want to trade cryptocurrencies
  So that I can invest

  Scenario: View market prices
    Given I am on trading platform
    When I view market dashboard
    Then I should see current prices For all listed cryptocurrencies

  Scenario: Buy cryptocurrency
    Given I want to buy Bitcoin
    When I place buy order for 0.5 BTC
    Then order should be executed
    And BTC added to my wallet

  Scenario: Sell cryptocurrency
    Given I own Ethereum
    When I place sell order
    Then ETH should be sold
    And funds added to balance

  Scenario: Set price alerts
    Given I want to know about price changes
    When I set alert for Bitcoin at $50,000
    Then I should be notified
    When price reaches that level

  Scenario: View transaction history
    Given I have made trades
    When I access history
    Then I should see all transactions
    And with dates and amounts

  Scenario: Transfer to external wallet
    Given I want to move my crypto
    When I transfer to external address
    Then transaction should be processed
    And confirmed on blockchain

Feature: Investment Services Platform
  As an investor
  I want to manage my investments
  So that I can grow my wealth

  Scenario: Open investment account
    Given I am a verified customer
    When I apply for an investment account
    And I complete risk assessment questionnaire
    Then my investment account should be created
    And I should have access to trading platform

  Scenario: Buy stocks
    Given I have $5000 in my investment account
    When I place an order to buy 10 shares of AAPL
    Then the order should be executed
    And the shares should appear in my portfolio

  Scenario: Sell stocks
    Given I own 20 shares of GOOGL
    When I place a sell order for 10 shares
    Then the shares should be sold at market price
    And the proceeds should be added to my account

  Scenario: Set up automatic investment plan
    Given I want to invest $500 monthly
    When I set up a SIP in index fund
    Then the plan should be activated
    And investments should be made automatically

  Scenario: View portfolio performance
    Given I have investments in multiple assets
    When I check my portfolio
    Then I should see overall returns
    And performance of each investment

  Scenario: Rebalance portfolio
    Given my portfolio has drifted from target allocation
    When I request portfolio rebalancing
    Then the system should suggest adjustments
    And I can approve the rebalancing

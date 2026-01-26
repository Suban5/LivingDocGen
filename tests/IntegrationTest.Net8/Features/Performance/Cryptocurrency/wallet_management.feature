Feature: Crypto Wallet Management
  As a crypto user
  I want to manage my wallet
  So that my assets are secure

  Scenario: Create new wallet
    Given I am new user
    When I create wallet
    Then wallet address should be generated
    And private keys secured

  Scenario: View wallet balance
    Given I have a wallet
    When I check balance
    Then I should see holdings For each cryptocurrency

  Scenario: Receive cryptocurrency
    Given someone is sending me crypto
    When I provide my wallet address
    Then funds should appear After blockchain confirmation

  Scenario: Send cryptocurrency
    Given I want to send Bitcoin
    When I enter recipient address and amount
    Then transaction should be broadcast
    And confirmed on blockchain

  Scenario: Backup wallet
    Given I want to secure my wallet
    When I backup recovery phrase
    Then 12-word phrase should be shown
    And I can restore later

  Scenario: Enable two-factor authentication
    Given I want extra security
    When I enable 2FA
    Then code should be required for all transactions

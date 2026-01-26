Feature: Offer and Negotiation Process
  As a buyer or seller
  I want to make and negotiate offers
  So that we can agree on terms

  Scenario: Submit purchase offer
    Given I want to buy a property
    When I submit offer of $450,000
    And I include contingencies
    Then offer should be sent to seller
    And I should track status

  Scenario: Seller receives offer
    Given buyer submitted an offer
    When I view the offer
    Then I should see offer amount
    And all terms and conditions

  Scenario: Counter offer
    Given I received an offer
    When I counter with $460,000
    Then counter offer should be sent
    And buyer should be notified

  Scenario: Accept offer
    Given I received acceptable offer
    When I accept the offer
    Then both parties should be notified
    And process moves to closing

  Scenario: Reject offer
    Given offer is too low
    When I reject it
    Then buyer should be informed
    And can submit new offer

  Scenario: Multiple offers handling
    Given I received 5 offers
    When I review all offers
    Then I should see comparison
    And choose best offer

Feature: Pet Care and Adoption
  As a pet lover
  I want to adopt pets
  So that I can provide them home

  Scenario: Browse available pets
    Given I want to adopt
    When I browse pet listings
    Then I see pets available With photos and details

  Scenario: Filter by pet type
    Given I prefer dogs
    When I filter by "Dogs"
    Then only dogs are shown
    And I can see breeds

  Scenario: View pet profile
    Given I found interesting pet
    When I view full profile
    Then I see age, breed, personality
    And adoption requirements

  Scenario: Submit adoption application
    Given I want to adopt pet
    When I submit application
    Then shelter receives it
    And reviews my info

  Scenario: Schedule meet and greet
    Given application is approved
    When I schedule visit
    Then appointment is set To meet the pet

  Scenario: Complete adoption
    Given we are good match
    When I finalize adoption
    Then pet is mine
    And I receive care instructions

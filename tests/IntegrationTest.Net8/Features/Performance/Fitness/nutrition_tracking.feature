Feature: Nutrition and Meal Tracking
  As a health-conscious user
  I want to track my nutrition
  So that I can maintain healthy diet

  Scenario: Log meal
    Given I ate breakfast
    When I log "Oatmeal with berries"
    Then calories should be calculated
    And macros should be tracked

  Scenario: Scan food barcode
    Given I am eating packaged food
    When I scan the barcode
    Then nutrition info should be retrieved
    And automatically logged

  Scenario: Set daily calorie goal
    Given I want to lose weight
    When I set goal of 1800 calories per day
    Then daily target should be set
    And progress should be tracked

  Scenario: View nutrition breakdown
    Given I logged my meals today
    When I check nutrition summary
    Then I should see calories, protein, carbs, fats
    And percentage of daily goals

  Scenario: Track water intake
    Given I drank a glass of water
    When I log it
    Then water intake should increase
    And progress toward 8 glasses shown

  Scenario: Create custom food entry
    Given I ate homemade meal
    When I create custom food entry
    And I enter nutrition values
    Then it should be saved
    And available for future logging

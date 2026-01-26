Feature: Email Campaign Management
  As a marketing manager
  I want to send email campaigns
  So that I can reach customers

  Scenario: Create email campaign
    Given I want to send newsletter
    When I create new campaign
    And I design email template
    Then campaign should be saved as draft

  Scenario: Build recipient list
    Given I have customer database
    When I segment by criteria
    Then targeted list should be created for the campaign

  Scenario: Schedule campaign
    Given campaign is ready
    When I schedule for tomorrow
    Then emails should be queued
    And sent at specified time

  Scenario: Track email opens
    Given campaign was sent
    When recipients open emails
    Then open rate should be tracked
    And analytics updated

  Scenario: Monitor click-through rate
    Given email contains links
    When recipients click
    Then clicks should be tracked by link and recipient

  Scenario: A/B test campaigns
    Given I want to optimize
    When I create two versions
    Then system should split test
    And determine winner

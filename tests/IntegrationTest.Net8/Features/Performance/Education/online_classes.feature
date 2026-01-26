Feature: Online Class Management
  As a student
  I want to attend online classes
  So that I can learn remotely

  Scenario: Join live class
    Given I have a class scheduled at 10 AM
    When I click "Join Class" button
    Then I should enter the virtual classroom
    And see the teacher's video

  Scenario: Participate in class chat
    Given I am in an online class
    When I type a question in chat
    Then other participants should see it
    And teacher can respond

  Scenario: Raise hand in class
    Given I am attending a live class
    When I click "Raise Hand" button
    Then teacher should be notified
    And my name should appear in queue

  Scenario: View class recording
    Given I missed yesterday's class
    When I access course materials
    Then I should find the class recording
    And be able to watch it

  Scenario: Share screen during presentation
    Given I am presenting in class
    When I share my screen
    Then all participants should see it
    And I can control what's visible

  Scenario: Download class materials
    Given teacher uploaded lecture slides
    When I access the materials section
    Then I should be able to download files
    And they should save to my device

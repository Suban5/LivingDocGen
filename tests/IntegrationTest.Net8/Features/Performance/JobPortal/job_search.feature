Feature: Job Search and Application
  As a job seeker
  I want to find jobs
  So that I can advance my career

  Scenario: Search for jobs
    Given I am looking for work
    When I search for "Software Engineer"
    And I filter by location
    Then I should see matching jobs
    And their details

  Scenario: Save job posting
    Given I found interesting job
    When I save it
    Then it should appear in my saved jobs

  Scenario: Apply for job
    Given I want to apply
    When I submit application
    And I attach resume
    Then application should be sent
    And confirmation received

  Scenario: Track application status
    Given I applied to jobs
    When I check status
    Then I should see where each application stands

  Scenario: Set job alerts
    Given I want notifications
    When I create alert for criteria
    Then I should receive emails for matching new postings

  Scenario: Update profile
    Given my info changed
    When I update my profile
    Then changes should be saved
    And visible to employers

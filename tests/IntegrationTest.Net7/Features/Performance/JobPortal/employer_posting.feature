Feature: Employer Job Posting Management
  As an employer
  I want to post jobs
  So that I can find candidates

  Scenario: Create job posting
    Given I have open position
    When I create job post
    And I enter job details
    Then posting should be published
    And visible to job seekers

  Scenario: Review applications
    Given candidates applied
    When I view applications
    Then I should see resumes
    And can shortlist candidates

  Scenario: Message candidates
    Given I want to contact applicant
    When I send message
    Then candidate should receive it
    And can respond

  Scenario: Close job posting
    Given position is filled
    When I close the posting
    Then it should be removed
    And no more applications accepted

  Scenario: Repost expired listing
    Given posting expired
    When I repost the job
    Then it should be active again
    And with updated date

  Scenario: View analytics
    Given job is posted
    When I check analytics
    Then I should see views
    And application count

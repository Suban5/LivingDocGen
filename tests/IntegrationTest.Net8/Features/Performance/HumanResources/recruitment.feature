Feature: Recruitment and Hiring
  As a recruiter
  I want to manage hiring process
  So that we find best candidates

  Scenario: Post job opening
    Given we have an open position
    When I create job posting
    Then posting should be published on job boards and website

  Scenario: Review applications
    Given candidates applied
    When I review resumes
    Then I can shortlist candidates
    And schedule interviews

  Scenario: Schedule interview
    Given candidate is shortlisted
    When I schedule interview
    Then calendar invite should be sent
    And interviewers notified

  Scenario: Record interview feedback
    Given interview was completed
    When interviewer submits feedback
    Then feedback should be recorded
    And hiring decision supported

  Scenario: Extend job offer
    Given we selected a candidate
    When I send offer letter
    Then candidate should receive offer
    And can accept or decline

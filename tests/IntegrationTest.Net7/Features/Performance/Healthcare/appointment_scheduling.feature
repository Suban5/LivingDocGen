Feature: Medical Appointment Scheduling
  As a patient
  I want to schedule appointments
  So that I can see doctors

  Scenario: Book appointment with general physician
    Given I am a registered patient
    When I select "General Physician" specialty
    And I choose an available time slot
    Then the appointment should be confirmed
    And I should receive confirmation SMS

  Scenario: Book appointment with specific doctor
    Given I want to see Dr. Sarah Connor
    When I check her availability
    And I book a slot for next Tuesday
    Then the appointment should be scheduled
    And it should appear in my appointments

  Scenario: Reschedule existing appointment
    Given I have an appointment tomorrow at 10 AM
    When I reschedule it to next week
    Then the old slot should be released
    And new slot should be booked

  Scenario: Cancel appointment
    Given I have an upcoming appointment
    When I cancel the appointment
    Then the slot should become available
    And the doctor should be notified

  Scenario: Book follow-up appointment
    Given I just completed a consultation
    When the doctor recommends follow-up
    Then I can book follow-up directly
    And it's marked as related to current visit

  Scenario: Waitlist for fully booked doctor
    Given Dr. Johnson is fully booked
    When I add myself to waitlist
    Then I should be notified if slot opens
    And I can book within 2 hours of notification

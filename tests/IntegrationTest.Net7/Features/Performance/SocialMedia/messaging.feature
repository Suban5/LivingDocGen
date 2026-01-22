Feature: Direct Messaging System
  As a user
  I want to send messages
  So that I can communicate privately

  Scenario: Send text message
    Given I open chat with "Sarah"
    When I type "Hello, how are you?"
    And I press send
    Then the message should be delivered
    And Sarah should receive a notification

  Scenario: Send image in message
    Given I am chatting with "Mike"
    When I attach an image file
    And I send the message
    Then the image should be uploaded
    And Mike should see the image

  Scenario: Create group chat
    Given I want to chat with multiple people
    When I create a group with "Tom", "Jerry", "Spike"
    And I name it "Work Team"
    Then the group chat should be created
    And all members should be notified

  Scenario: React to message
    Given I received a message from "Emma"
    When I react with a "❤️" emoji
    Then Emma should see the reaction
    And it should appear under the message

  Scenario: Delete sent message
    Given I sent a message 2 minutes ago
    When I delete the message
    Then it should be removed from chat
    And show "Message deleted" placeholder

  Scenario: Mark conversation as unread
    Given I have read messages from "Lisa"
    When I mark the conversation as unread
    Then it should appear as unread
    And stay at top of my message list

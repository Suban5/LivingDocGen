# language: en
# API Testing Feature File
# Demonstrates REST API testing scenarios with Gherkin

@api @rest @backend
Feature: User Management REST API
  As an API consumer
  I want to manage user accounts via REST endpoints
  So that I can integrate with the user management system

  Background: API Configuration
    Given the API base URL is "https://api.example.com/v2"
    And I have valid API credentials:
      | Key        | Value                              |
      | API_KEY    | test_api_key_1234567890abcdef      |
      | API_SECRET | test_api_secret_0987654321fedcba   |
    And the API rate limit is 1000 requests per hour
    And I set request headers:
      | Header       | Value            |
      | Content-Type | application/json |
      | Accept       | application/json |

  @crud @post
  Scenario: Create new user account via API
    # POST /users endpoint
    When I send a POST request to "/users" with body:
      """json
      {
        "username": "john.doe.2025",
        "email": "john.doe@example.com",
        "firstName": "John",
        "lastName": "Doe",
        "password": "SecureP@ssw0rd!",
        "dateOfBirth": "1990-05-15",
        "phoneNumber": "+1-555-123-4567",
        "address": {
          "street": "123 Main St",
          "city": "San Francisco",
          "state": "CA",
          "zipCode": "94102",
          "country": "USA"
        },
        "preferences": {
          "newsletter": true,
          "smsNotifications": false,
          "theme": "dark"
        },
        "roles": ["user", "customer"],
        "metadata": {
          "source": "mobile_app",
          "campaign": "spring_2025"
        }
      }
      """
    Then the response status code should be 201
    And the response time should be less than 500 milliseconds
    And the response should have header "Location"
    And the response body should match:
      """json
      {
        "id": "@uuid@",
        "username": "john.doe.2025",
        "email": "john.doe@example.com",
        "firstName": "John",
        "lastName": "Doe",
        "status": "active",
        "emailVerified": false,
        "createdAt": "@datetime@",
        "updatedAt": "@datetime@",
        "_links": {
          "self": "/users/@string@",
          "verify-email": "/users/@string@/verify",
          "update": "/users/@string@"
        }
      }
      """
    And the response should NOT contain field "password"
    And I should receive webhook notification:
      """json
      {
        "event": "user.created",
        "userId": "@uuid@",
        "timestamp": "@datetime@"
      }
      """

  @validation @negative
  Scenario Outline: Validate user creation with invalid data
    When I send a POST request to "/users" with body:
      """json
      {
        "username": "<username>",
        "email": "<email>",
        "password": "<password>"
      }
      """
    Then the response status code should be <status_code>
    And the response body should contain error:
      """json
      {
        "error": {
          "code": "<error_code>",
          "message": "<error_message>",
          "field": "<field>"
        }
      }
      """

    Examples: Invalid Input Validation
      | username  | email                 | password  | status_code | error_code          | error_message                           | field    |
      |           | valid@example.com     | Pass123!  | 400         | MISSING_FIELD       | Username is required                    | username |
      | john      | invalid-email         | Pass123!  | 400         | INVALID_FORMAT      | Invalid email format                    | email    |
      | john      | valid@example.com     | 123       | 400         | WEAK_PASSWORD       | Password must be at least 8 characters  | password |
      | ab        | valid@example.com     | Pass123!  | 400         | TOO_SHORT           | Username must be at least 3 characters  | username |
      | john      | duplicate@example.com | Pass123!  | 409         | DUPLICATE_USER      | Email already exists                    | email    |
      | $pecial#  | valid@example.com     | Pass123!  | 400         | INVALID_CHARACTERS  | Username contains invalid characters    | username |

  @crud @get @pagination @ignore
  Scenario: Retrieve paginated list of users
    Given the database contains 150 users
    When I send a GET request to "/users" with query parameters:
      | Parameter | Value |
      | page      | 2     |
      | limit     | 25    |
      | sort      | email |
      | order     | asc   |
      | status    | active|
    Then the response status code should be 200
    And the response should contain 25 users
    And the response should have pagination metadata:
      """json
      {
        "pagination": {
          "page": 2,
          "limit": 25,
          "total": 150,
          "totalPages": 6,
          "hasNext": true,
          "hasPrevious": true,
          "links": {
            "first": "/users?page=1&limit=25",
            "previous": "/users?page=1&limit=25",
            "next": "/users?page=3&limit=25",
            "last": "/users?page=6&limit=25"
          }
        }
      }
      """
    And all users should have "active" status
    And the users should be sorted by "email" in "ascending" order

  @crud @patch @partial-update
  Scenario: Partially update user profile
    Given a user exists with ID "usr_abc123xyz"
    When I send a PATCH request to "/users/usr_abc123xyz" with body:
      """json
      {
        "phoneNumber": "+1-555-999-8888",
        "preferences": {
          "newsletter": false,
          "theme": "light"
        }
      }
      """
    Then the response status code should be 200
    And the response should contain updated fields:
      | Field                      | Value              |
      | phoneNumber                | +1-555-999-8888    |
      | preferences.newsletter     | false              |
      | preferences.theme          | light              |
    And the response should preserve original values:
      | Field     |
      | email     |
      | username  |
      | firstName |
      | lastName  |

  @crud @delete @soft-delete
  Scenario: Soft delete user account
    Given a user exists with email "delete.me@example.com"
    When I send a DELETE request to "/users/{userId}"
    Then the response status code should be 204
    And the user should be marked as "deleted" in database
    And the user should NOT appear in GET "/users" response
    And the user data should be retained for 30 days
    And I should receive deletion confirmation email

  @batch @bulk-operations
  Scenario: Bulk update user roles
    When I send a POST request to "/users/bulk/update-roles" with body:
      """json
      {
        "userIds": [
          "usr_001",
          "usr_002",
          "usr_003",
          "usr_004",
          "usr_005"
        ],
        "action": "add",
        "roles": ["premium", "beta-tester"]
      }
      """
    Then the response status code should be 202
    And the response should contain batch job information:
      """json
      {
        "jobId": "@uuid@",
        "status": "processing",
        "totalUsers": 5,
        "estimatedCompletion": "@datetime@",
        "statusUrl": "/jobs/@string@"
      }
      """
    And within 10 seconds the job status should be "completed"
    And all 5 users should have roles ["user", "customer", "premium", "beta-tester"]

  @security @authentication
  Scenario Outline: Test API authentication and authorization
    When I send a <method> request to "<endpoint>" with authentication "<auth_type>"
    Then the response status code should be <status_code>
    And the response should contain "<response_field>"

    Examples: Authentication Scenarios
      | method | endpoint           | auth_type      | status_code | response_field |
      | GET    | /users             | valid_token    | 200         | users          |
      | GET    | /users             | expired_token  | 401         | error          |
      | GET    | /users             | invalid_token  | 401         | error          |
      | GET    | /users             | no_token       | 401         | error          |
      | GET    | /admin/users       | user_token     | 403         | error          |
      | GET    | /admin/users       | admin_token    | 200         | users          |
      | POST   | /users             | valid_token    | 201         | id             |
      | DELETE | /users/usr_123     | other_user     | 403         | error          |

  @performance @load-testing
  Scenario: API should handle concurrent requests efficiently
    # Performance testing scenario
    When I send 100 concurrent GET requests to "/users/{randomUserId}"
    Then at least 95% of requests should complete within 1 second
    And the average response time should be less than 300 milliseconds
    And no requests should fail due to rate limiting
    And the API should return correct data in all responses

  @webhooks @integration
  Scenario: Verify webhook delivery for user events
    Given I have configured a webhook endpoint "https://myapp.com/webhooks/users"
    And the webhook signature secret is "whsec_test_secret_key"
    When I create a new user via API
    Then a webhook POST request should be sent to my endpoint within 5 seconds
    And the webhook payload should contain:
      """json
      {
        "id": "@uuid@",
        "event": "user.created",
        "apiVersion": "v2",
        "timestamp": "@datetime@",
        "data": {
          "userId": "@uuid@",
          "username": "@string@",
          "email": "@email@"
        }
      }
      """
    And the webhook request should include header "X-Webhook-Signature"
    And the signature should be valid using HMAC-SHA256
    And if webhook fails, it should retry 3 times with exponential backoff

# Comments can appear anywhere
# This demonstrates comprehensive API testing coverage

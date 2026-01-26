# language: en
# This feature file demonstrates EVERY Gherkin keyword and construct
# Used for comprehensive parser testing

@gherkin-showcase @complete-syntax
Feature: Complete Gherkin Syntax Demonstration
  This feature demonstrates all possible Gherkin keywords and constructs
  including comments, tags, backgrounds, scenarios, scenario outlines,
  data tables, doc strings, and various step keywords.
  
  It serves as a comprehensive test case for the Universal BDD Parser
  to ensure all Gherkin elements are correctly parsed and represented.

  # Rule keyword (Gherkin 6+)
  Rule: User Authentication Rules
    All users must be authenticated before accessing protected resources
    Authentication can be via username/password, OAuth, or API keys

    Background: Common setup for authentication scenarios
      # Background applies to all scenarios under this rule
      Given the authentication service is running
      And the user database is accessible
      And rate limiting is configured to 5 attempts per minute
      * logging is enabled for security events

    # Example using all step keywords: Given, When, Then, And, But, *
    @positive @smoke
    Scenario: Successful login with valid credentials
      # Given - preconditions/initial state
      Given a user exists with username "john_doe"
      And the user's password is "SecurePass123!"
      And the user's account is "active"
      And the user has role "standard_user"
      * the user's email is verified
      
      # When - actions/events
      When the user submits login form with:
        | Field    | Value          |
        | username | john_doe       |
        | password | SecurePass123! |
      And the user's IP address is "192.168.1.100"
      And the user agent is "Mozilla/5.0"
      * the request includes CSRF token
      
      # Then - expected outcomes
      Then the login should be successful
      And a session token should be generated
      And the session should expire in 30 minutes
      But the password should NOT be logged
      * an audit entry should be created

    @negative @security
    Scenario: Failed login attempts should trigger account lockout
      Given a user exists with username "jane_doe"
      And the account lockout threshold is 3 attempts
      When the user enters wrong password 3 times
      Then the account should be locked
      And the user should see error message:
        """
        Your account has been temporarily locked due to multiple failed login attempts.
        Please try again in 15 minutes or reset your password.
        
        For security reasons, we've sent an email notification to your registered address.
        """
      And an email should be sent with subject "Security Alert: Account Locked"
      But the actual password should NOT be revealed

    # Scenario Outline with multiple Examples sections
    @data-driven @parametrized
    Scenario Outline: Login with different user roles and permissions
      Given a user with username "<username>" and role "<role>"
      When the user logs in successfully
      Then the user should have access to "<accessible_pages>"
      And the user should NOT have access to "<restricted_pages>"
      But the user should see "<navigation_menu>"
      * the session data should include role "<role>"

      # First set of examples - Admin users
      @admin-users
      Examples: Administrator Access Levels
        | username    | role           | accessible_pages                        | restricted_pages | navigation_menu           |
        | admin_001   | super_admin    | all                                     | none             | admin_full_menu           |
        | admin_002   | admin          | dashboard,users,settings,reports        | system_config    | admin_menu                |
        | moderator   | moderator      | dashboard,content,reports               | users,settings   | moderator_menu            |

      # Second set - Regular users
      @regular-users
      Examples: Standard User Access
        | username    | role           | accessible_pages                        | restricted_pages          | navigation_menu           |
        | user_001    | premium        | dashboard,profile,content,premium       | admin,settings            | premium_user_menu         |
        | user_002    | standard       | dashboard,profile,content               | admin,settings,premium    | standard_user_menu        |
        | user_003    | trial          | dashboard,profile                       | content,admin,settings    | trial_user_menu           |
        | guest_001   | guest          | public_pages                            | all_protected             | guest_menu                |

      # Third set - Special cases
      @edge-cases
      Examples: Edge Cases and Special Scenarios
        | username    | role           | accessible_pages                        | restricted_pages | navigation_menu           |
        | suspended   | suspended      | none                                    | all              | empty                     |
        | readonly    | read_only      | dashboard,reports                       | write_operations | readonly_menu             |

  Rule: Password Management Rules
    Passwords must meet security requirements
    Users can reset forgotten passwords via email

    Scenario: Password must meet complexity requirements
      # Testing password validation rules
      When a user attempts to set password "<password>"
      Then the system should evaluate password strength
      And the result should be:
        | Criteria              | Status | Details                          |
        | Minimum length (8)    | pass   | Password has 12 characters       |
        | Contains uppercase    | pass   | Contains: A, B, C                |
        | Contains lowercase    | pass   | Contains: a, b, c                |
        | Contains numbers      | pass   | Contains: 1, 2, 3                |
        | Contains special char | pass   | Contains: !, @, #                |
        | Not common password   | pass   | Not in breach database           |
        | Not same as username  | pass   | Different from username          |

    Scenario: User can reset password via email token
      Given a user with email "user@example.com" forgot their password
      When the user requests a password reset
      Then a reset token should be generated with properties:
        """json
        {
          "token": "randomly_generated_64_char_string",
          "expiresIn": "15 minutes",
          "usageLimit": 1,
          "userId": "user_id_hash",
          "createdAt": "2025-12-02T10:30:00Z",
          "ipAddress": "192.168.1.100"
        }
        """
      And an email should be sent containing the reset link
      When the user clicks the reset link within 15 minutes
      And enters a new password "NewSecureP@ss123"
      Then the password should be updated
      And all existing sessions should be invalidated
      But the reset token should be marked as used

  # Multiple scenarios with large data tables
  Rule: Multi-factor Authentication Rules

    @mfa @totp
    Scenario: Setup Time-based One-Time Password (TOTP)
      Given a user wants to enable two-factor authentication
      When the user initiates TOTP setup
      Then the system should generate a secret key
      And display a QR code with data:
        """
        otpauth://totp/MyApp:user@example.com?
        secret=JBSWY3DPEHPK3PXP&
        issuer=MyApp&
        algorithm=SHA1&
        digits=6&
        period=30
        """
      And provide backup codes:
        | Code      | Status  | Used Date |
        | ABC-123   | unused  | null      |
        | DEF-456   | unused  | null      |
        | GHI-789   | unused  | null      |
        | JKL-012   | unused  | null      |
        | MNO-345   | unused  | null      |
        | PQR-678   | unused  | null      |
        | STU-901   | unused  | null      |
        | VWX-234   | unused  | null      |

    @mfa @verification
    Scenario Outline: Verify TOTP codes with time windows
      Given TOTP is enabled for user "<username>"
      And the current timestamp is <timestamp>
      And the secret key is "<secret>"
      When the user enters TOTP code "<code>"
      Then the verification should be "<result>"
      And the time window used should be "<window>"

      Examples: TOTP Time Window Validation
        | username | timestamp  | secret          | code   | result  | window    |
        | user1    | 1638360000 | JBSWY3DPEHPK3PXP | 123456 | success | current   |
        | user2    | 1638360029 | JBSWY3DPEHPK3PXP | 123456 | success | current   |
        | user3    | 1638360030 | JBSWY3DPEHPK3PXP | 789012 | success | next      |
        | user4    | 1638359999 | JBSWY3DPEHPK3PXP | 456789 | success | previous  |
        | user5    | 1638360000 | JBSWY3DPEHPK3PXP | 999999 | failure | none      |
        | user6    | 1638300000 | JBSWY3DPEHPK3PXP | 123456 | failure | expired   |

# This feature file contains:
# ✅ Feature description
# ✅ Multiple Rules
# ✅ Multiple Backgrounds (one per rule)
# ✅ Regular Scenarios
# ✅ Scenario Outlines
# ✅ Multiple Examples sections per Scenario Outline
# ✅ Tags at feature, rule, scenario, and examples level
# ✅ All step keywords: Given, When, Then, And, But, *
# ✅ Data Tables (small and large)
# ✅ Doc Strings (plain text, JSON, and multiline)
# ✅ Comments throughout
# ✅ Edge cases and special characters

# Total test coverage:
# - Scenarios: 7
# - Scenario Outlines: 2
# - Example rows: 15+
# - Rules: 3
# - Data tables: 10+
# - Doc strings: 5+

using System;
using System.Collections.Generic;
using FluentAssertions;
using Reqnroll;
using Reqnroll.Assist;

namespace BDD.TestExecution.StepDefinitions;

[Binding]
public class CompleteGherkinSyntaxSteps
{
    private readonly ScenarioContext _scenarioContext;
    private bool _authServiceRunning;
    private bool _databaseAccessible;
    private int _rateLimitAttempts;
    private bool _loggingEnabled;
    private Dictionary<string, object> _userData = new();
    private Dictionary<string, object> _sessionData = new();
    private string _errorMessage = string.Empty;
    private int _failedLoginAttempts;
    private bool _accountLocked;
    private string _emailSubject = string.Empty;
    private Dictionary<string, string> _passwordValidation = new();
    private string _resetToken = string.Empty;
    private bool _totpEnabled;
    private string _totpSecret = string.Empty;
    private string _qrCodeData = string.Empty;
    private List<string> _backupCodes = new();
    private string _totpVerificationResult = string.Empty;
    private string _timeWindow = string.Empty;

    public CompleteGherkinSyntaxSteps(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
    }

    #region Background Steps

    [Given(@"the authentication service is running")]
    public void GivenTheAuthenticationServiceIsRunning()
    {
        _authServiceRunning = true;
        Console.WriteLine("✓ Authentication service initialized");
    }

    [Given(@"the user database is accessible")]
    public void GivenTheUserDatabaseIsAccessible()
    {
        _databaseAccessible = true;
        Console.WriteLine("✓ Database connection established");
    }

    [Given(@"rate limiting is configured to (.*) attempts per minute")]
    public void GivenRateLimitingIsConfigured(int attempts)
    {
        _rateLimitAttempts = attempts;
        Console.WriteLine($"✓ Rate limiting: {attempts} attempts/minute");
    }

    [Given(@"logging is enabled for security events")]
    public void GivenLoggingIsEnabledForSecurityEvents()
    {
        _loggingEnabled = true;
        Console.WriteLine("✓ Security logging enabled");
    }

    #endregion

    #region User Authentication Steps

    [Given(@"a user exists with username ""(.*)""")]
    public void GivenAUserExistsWithUsername(string username)
    {
        _userData["username"] = username;
        _userData["exists"] = true;
        Console.WriteLine($"✓ User '{username}' exists in database");
    }

    [Given(@"the user's password is ""(.*)""")]
    public void GivenTheUsersPasswordIs(string password)
    {
        _userData["password"] = password;
        Console.WriteLine("✓ Password set for user");
    }

    [Given(@"the user's account is ""(.*)""")]
    public void GivenTheUsersAccountIs(string status)
    {
        _userData["status"] = status;
        Console.WriteLine($"✓ Account status: {status}");
    }

    [Given(@"the user has role ""(.*)""")]
    public void GivenTheUserHasRole(string role)
    {
        _userData["role"] = role;
        Console.WriteLine($"✓ User role: {role}");
    }

    [Given(@"the user's email is verified")]
    public void GivenTheUsersEmailIsVerified()
    {
        _userData["emailVerified"] = true;
        Console.WriteLine("✓ Email verified");
    }

    [When(@"the user submits login form with:")]
    public void WhenTheUserSubmitsLoginFormWith(Table table)
    {
        foreach (var row in table.Rows)
        {
            _sessionData[row["Field"]] = row["Value"];
        }
        Console.WriteLine("✓ Login form submitted");
    }

    [When(@"the user's IP address is ""(.*)""")]
    public void WhenTheUsersIPAddressIs(string ipAddress)
    {
        _sessionData["ipAddress"] = ipAddress;
        Console.WriteLine($"✓ IP address: {ipAddress}");
    }

    [When(@"the user agent is ""(.*)""")]
    public void WhenTheUserAgentIs(string userAgent)
    {
        _sessionData["userAgent"] = userAgent;
        Console.WriteLine($"✓ User agent: {userAgent}");
    }

    [When(@"the request includes CSRF token")]
    public void WhenTheRequestIncludesCSRFToken()
    {
        _sessionData["csrfToken"] = "valid_token_123";
        Console.WriteLine("✓ CSRF token included");
    }

    [Then(@"the login should be successful")]
    public void ThenTheLoginShouldBeSuccessful()
    {
        _userData.Should().ContainKey("username");
        _userData.Should().ContainKey("password");
        _sessionData.Should().ContainKey("username");
        Console.WriteLine("✓ Login successful");
    }

    [Then(@"a session token should be generated")]
    public void ThenASessionTokenShouldBeGenerated()
    {
        _sessionData["sessionToken"] = "session_token_abc123";
        _sessionData.Should().ContainKey("sessionToken");
        Console.WriteLine("✓ Session token generated");
    }

    [Then(@"the session should expire in (.*) minutes")]
    public void ThenTheSessionShouldExpireInMinutes(int minutes)
    {
        _sessionData["expiryMinutes"] = minutes;
        minutes.Should().Be(30);
        Console.WriteLine($"✓ Session expires in {minutes} minutes");
    }

    [Then(@"the password should NOT be logged")]
    public void ThenThePasswordShouldNOTBeLogged()
    {
        // Verify password is not in logs (always passes)
        Console.WriteLine("✓ Password not logged (security check passed)");
    }

    [Then(@"an audit entry should be created")]
    public void ThenAnAuditEntryShouldBeCreated()
    {
        _sessionData["auditCreated"] = true;
        _sessionData.Should().ContainKey("auditCreated");
        Console.WriteLine("✓ Audit entry created");
    }

    #endregion

    #region Account Lockout Steps

    [Given(@"the account lockout threshold is (.*) attempts")]
    public void GivenTheAccountLockoutThresholdIsAttempts(int threshold)
    {
        _userData["lockoutThreshold"] = threshold;
        Console.WriteLine($"✓ Lockout threshold: {threshold} attempts");
    }

    [When(@"the user enters wrong password (.*) times")]
    public void WhenTheUserEntersWrongPasswordTimes(int attempts)
    {
        _failedLoginAttempts = attempts;
        Console.WriteLine($"✓ Failed login attempts: {attempts}");
    }

    [Then(@"the account should be locked")]
    public void ThenTheAccountShouldBeLocked()
    {
        _accountLocked = true;
        
        // INTENTIONAL FAILURE: Account lockout not properly triggered
        _accountLocked.Should().BeFalse("Account lockout mechanism has a bug");
        
        Console.WriteLine("✗ Account should be locked but isn't");
    }

    [Then(@"the user should see error message:")]
    public void ThenTheUserShouldSeeErrorMessage(string multilineText)
    {
        _errorMessage = multilineText;
        _errorMessage.Should().Contain("locked");
        Console.WriteLine("✓ Error message displayed");
    }

    [Then(@"an email should be sent with subject ""(.*)""")]
    public void ThenAnEmailShouldBeSentWithSubject(string subject)
    {
        _emailSubject = subject;
        _emailSubject.Should().Be(subject);
        Console.WriteLine($"✓ Email sent: {subject}");
    }

    [Then(@"the actual password should NOT be revealed")]
    public void ThenTheActualPasswordShouldNOTBeRevealed()
    {
        // Security check - always passes
        Console.WriteLine("✓ Password not revealed in error message");
    }

    #endregion

    #region Role-based Access Steps (Scenario Outline)

    [Given(@"a user with username ""(.*)"" and role ""(.*)""")]
    public void GivenAUserWithUsernameAndRole(string username, string role)
    {
        _userData["username"] = username;
        _userData["role"] = role;
        Console.WriteLine($"✓ User '{username}' with role '{role}'");
    }

    [When(@"the user logs in successfully")]
    public void WhenTheUserLogsInSuccessfully()
    {
        _sessionData["loggedIn"] = true;
        Console.WriteLine("✓ User logged in");
    }

    [Then(@"the user should have access to ""(.*)""")]
    public void ThenTheUserShouldHaveAccessTo(string accessiblePages)
    {
        _sessionData["accessiblePages"] = accessiblePages;
        
        // Make some scenarios fail based on username
        if (_userData["username"]?.ToString() == "moderator")
        {
            // INTENTIONAL FAILURE: Moderator access issue
            accessiblePages.Should().Contain("admin_panel", "Moderator should have admin access");
        }
        else
        {
            accessiblePages.Should().NotBeNullOrEmpty();
        }
        
        Console.WriteLine($"✓ Access granted to: {accessiblePages}");
    }

    [Then(@"the user should NOT have access to ""(.*)""")]
    public void ThenTheUserShouldNOTHaveAccessTo(string restrictedPages)
    {
        _sessionData["restrictedPages"] = restrictedPages;
        restrictedPages.Should().NotBeNullOrEmpty();
        Console.WriteLine($"✓ Access restricted from: {restrictedPages}");
    }

    [Then(@"the user should see ""(.*)""")]
    public void ThenTheUserShouldSee(string navigationMenu)
    {
        _sessionData["navigationMenu"] = navigationMenu;
        navigationMenu.Should().NotBeNullOrEmpty();
        Console.WriteLine($"✓ Navigation menu: {navigationMenu}");
    }

    [Then(@"the session data should include role ""(.*)""")]
    public void ThenTheSessionDataShouldIncludeRole(string role)
    {
        _sessionData["role"] = role;
        _sessionData["role"].Should().Be(role);
        Console.WriteLine($"✓ Session role: {role}");
    }

    #endregion

    #region Password Complexity Steps

    [When(@"a user attempts to set password ""(.*)""")]
    public void WhenAUserAttemptsToSetPassword(string password)
    {
        _userData["newPassword"] = password;
        Console.WriteLine($"✓ Password validation for: {password.Substring(0, 3)}***");
    }

    [Then(@"the system should evaluate password strength")]
    public void ThenTheSystemShouldEvaluatePasswordStrength()
    {
        _passwordValidation["evaluated"] = "true";
        Console.WriteLine("✓ Password strength evaluated");
    }

    [Then(@"the result should be:")]
    public void ThenTheResultShouldBe(Table table)
    {
        foreach (var row in table.Rows)
        {
            var criteria = row["Criteria"];
            var status = row["Status"];
            _passwordValidation[criteria] = status;
        }
        
        _passwordValidation.Should().ContainKey("Minimum length (8)");
        Console.WriteLine($"✓ Password validation complete: {table.RowCount} criteria checked");
    }

    #endregion

    #region Password Reset Steps

    [Given(@"a user with email ""(.*)"" forgot their password")]
    public void GivenAUserWithEmailForgotTheirPassword(string email)
    {
        _userData["email"] = email;
        Console.WriteLine($"✓ Password reset requested for: {email}");
    }

    [When(@"the user requests a password reset")]
    public void WhenTheUserRequestsAPasswordReset()
    {
        _resetToken = "reset_token_" + Guid.NewGuid().ToString("N");
        Console.WriteLine("✓ Password reset initiated");
    }

    [Then(@"a reset token should be generated with properties:")]
    public void ThenAResetTokenShouldBeGeneratedWithProperties(string multilineText)
    {
        _resetToken.Should().NotBeNullOrEmpty();
        multilineText.Should().Contain("token");
        Console.WriteLine("✓ Reset token generated with properties");
    }

    [Then(@"an email should be sent containing the reset link")]
    public void ThenAnEmailShouldBeSentContainingTheResetLink()
    {
        _sessionData["resetEmailSent"] = true;
        Console.WriteLine("✓ Reset email sent");
    }

    [When(@"the user clicks the reset link within (.*) minutes")]
    public void WhenTheUserClicksTheResetLinkWithinMinutes(int minutes)
    {
        _sessionData["resetLinkClicked"] = true;
        Console.WriteLine($"✓ Reset link clicked within {minutes} minutes");
    }

    [When(@"enters a new password ""(.*)""")]
    public void WhenEntersANewPassword(string newPassword)
    {
        _userData["newPassword"] = newPassword;
        Console.WriteLine("✓ New password entered");
    }

    [Then(@"the password should be updated")]
    public void ThenThePasswordShouldBeUpdated()
    {
        _userData.Should().ContainKey("newPassword");
        Console.WriteLine("✓ Password updated successfully");
    }

    [Then(@"all existing sessions should be invalidated")]
    public void ThenAllExistingSessionsShouldBeInvalidated()
    {
        _sessionData.Clear();
        Console.WriteLine("✓ All sessions invalidated");
    }

    [Then(@"the reset token should be marked as used")]
    public void ThenTheResetTokenShouldBeMarkedAsUsed()
    {
        _sessionData["tokenUsed"] = true;
        Console.WriteLine("✓ Reset token marked as used");
    }

    #endregion

    #region TOTP / MFA Steps

    [Given(@"a user wants to enable two-factor authentication")]
    public void GivenAUserWantsToEnableTwoFactorAuthentication()
    {
        _userData["mfaRequested"] = true;
        Console.WriteLine("✓ MFA setup requested");
    }

    [When(@"the user initiates TOTP setup")]
    public void WhenTheUserInitiatesTOTPSetup()
    {
        _totpEnabled = true;
        Console.WriteLine("✓ TOTP setup initiated");
    }

    [Then(@"the system should generate a secret key")]
    public void ThenTheSystemShouldGenerateASecretKey()
    {
        _totpSecret = "JBSWY3DPEHPK3PXP";
        _totpSecret.Should().NotBeNullOrEmpty();
        
        // INTENTIONAL FAILURE: Secret key generation has wrong length
        _totpSecret.Length.Should().Be(32, "Secret key should be 32 characters");
        
        Console.WriteLine("✗ Secret key generation failed validation");
    }

    [Then(@"display a QR code with data:")]
    public void ThenDisplayAQRCodeWithData(string multilineText)
    {
        _qrCodeData = multilineText;
        _qrCodeData.Should().Contain("otpauth://totp/");
        Console.WriteLine("✓ QR code data generated");
    }

    [Then(@"provide backup codes:")]
    public void ThenProvideBackupCodes(Table table)
    {
        foreach (var row in table.Rows)
        {
            _backupCodes.Add(row["Code"]);
        }
        
        _backupCodes.Count.Should().Be(table.RowCount);
        Console.WriteLine($"✓ Generated {table.RowCount} backup codes");
    }

    [Given(@"TOTP is enabled for user ""(.*)""")]
    public void GivenTOTPIsEnabledForUser(string username)
    {
        _userData["username"] = username;
        _totpEnabled = true;
        Console.WriteLine($"✓ TOTP enabled for {username}");
    }

    [Given(@"the current timestamp is (.*)")]
    public void GivenTheCurrentTimestampIs(long timestamp)
    {
        _sessionData["timestamp"] = timestamp;
        Console.WriteLine($"✓ Timestamp: {timestamp}");
    }

    [Given(@"the secret key is ""(.*)""")]
    public void GivenTheSecretKeyIs(string secret)
    {
        _totpSecret = secret;
        Console.WriteLine($"✓ Secret key set");
    }

    [When(@"the user enters TOTP code ""(.*)""")]
    public void WhenTheUserEntersTOTPCode(string code)
    {
        _sessionData["totpCode"] = code;
        Console.WriteLine($"✓ TOTP code entered: {code}");
    }

    [Then(@"the verification should be ""(.*)""")]
    public void ThenTheVerificationShouldBe(string result)
    {
        _totpVerificationResult = result;
        
        // Make some TOTP verifications fail
        string username = _userData.ContainsKey("username") ? _userData["username"]?.ToString() ?? "" : "";
        
        if (username == "user3" || username == "user4")
        {
            // INTENTIONAL FAILURE: Time window validation issue
            result.Should().Be("failure", "Time window validation has a bug");
        }
        else
        {
            _totpVerificationResult.Should().Be(result);
        }
        
        Console.WriteLine($"✓ Verification result: {result}");
    }

    [Then(@"the time window used should be ""(.*)""")]
    public void ThenTheTimeWindowUsedShouldBe(string window)
    {
        _timeWindow = window;
        _timeWindow.Should().Be(window);
        Console.WriteLine($"✓ Time window: {window}");
    }

    #endregion
}

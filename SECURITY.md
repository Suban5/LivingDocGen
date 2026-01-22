# Security Policy

## Supported Versions

We actively support the following versions of LivingDocGen with security updates:

| Version | Supported          | .NET Support |
| ------- | ------------------ | ------------ |
| 2.0.4   | :white_check_mark: | .NET 6.0+ |
| 2.0.3   | :white_check_mark: | .NET 6.0+ |
| 2.0.2   | :white_check_mark: | .NET 6.0+ |
| 2.0.1   | :white_check_mark: | .NET 6.0+ |
| 2.0.0   | :x:                | .NET 6.0+ |
| 1.0.4   | :x:                | .NET Standard 2.0/2.1, .NET 6+ |
| 1.0.3   | :x:                | .NET Standard 2.0/2.1, .NET 6+ |
| < 1.0.3 | :x:                | Not supported |

**Recommendation:** Always use the latest version (2.0.4) for the best security, performance, and features.

---

## Reporting a Vulnerability

We take security vulnerabilities seriously. If you discover a security issue in LivingDocGen, please report it responsibly.

### 📧 How to Report

**Please DO NOT open a public GitHub issue for security vulnerabilities.**

Instead, report security issues via:

1. **GitHub Security Advisory:** [Create a private security advisory](https://github.com/suban5/LivingDocGen/security/advisories/new) (Recommended)
2. **Email:** Contact the project maintainer via GitHub profile

### 📝 What to Include

When reporting a vulnerability, please include:

- **Description:** Clear description of the vulnerability
- **Impact:** What could an attacker accomplish?
- **Steps to Reproduce:** Detailed steps to reproduce the issue
- **Affected Versions:** Which versions are affected?
- **Proof of Concept:** Code snippet or demonstration (if applicable)
- **Suggested Fix:** If you have ideas on how to fix it
- **Your Contact Info:** So we can follow up with questions

### ⏱️ Response Timeline

- **Initial Response:** Within 48 hours of report
- **Status Update:** Within 7 days with severity assessment
- **Fix Timeline:** 
  - Critical: 1-2 weeks
  - High: 2-4 weeks
  - Medium: 4-8 weeks
  - Low: Next scheduled release

### 🛡️ Security Update Process

1. **Acknowledgment:** We'll confirm receipt of your report
2. **Investigation:** We'll investigate and validate the issue
3. **Fix Development:** We'll develop and test a fix
4. **Coordinated Disclosure:** We'll coordinate release timing with you
5. **Release:** We'll release a patched version
6. **Credit:** We'll credit you in release notes (if desired)

---

## Security Best Practices

### For Users

When using LivingDocGen, follow these security best practices:

1. **Keep Updated:** Always use the latest version
2. **Validate Input:** Ensure feature files and test results come from trusted sources
3. **Review Output:** Review generated HTML before sharing publicly
4. **Protect API Keys:** Never commit NuGet API keys to version control
5. **Secure Build Pipeline:** Use secure CI/CD practices when integrating LivingDocGen

### For Contributors

When contributing to LivingDocGen:

1. **Dependencies:** Keep all NuGet packages updated
2. **Code Review:** All PRs require security-conscious review
3. **Input Validation:** Always validate and sanitize user inputs
4. **No Secrets:** Never commit sensitive data (API keys, passwords, tokens)
5. **XSS Prevention:** Sanitize all HTML output to prevent XSS attacks
6. **Path Traversal:** Validate all file paths to prevent directory traversal
7. **XML Security:** Use secure XML parsing to prevent XXE attacks

---

## Known Security Considerations

### HTML Output

LivingDocGen generates HTML documentation that may contain:
- User-provided text from feature files
- Error messages from test execution
- Stack traces with file paths

**Recommendation:** Review generated documentation before sharing publicly, especially if feature files contain sensitive information.

### File System Access

LivingDocGen reads:
- Feature files (`.feature` files)
- Test result files (XML, JSON, TRX)
- Configuration files (`bdd-livingdoc.json`)

**Recommendation:** Ensure LivingDocGen only has access to intended directories. Use principle of least privilege.

### Dependencies

LivingDocGen relies on several NuGet packages. We:
- Regularly update dependencies to patch known vulnerabilities
- Monitor security advisories for our dependencies
- Use tools like `dotnet list package --vulnerable` to detect issues

---

## Vulnerability Disclosure Policy

We follow responsible disclosure principles:

1. **Private Reporting:** Security issues should be reported privately
2. **Coordinated Disclosure:** We'll work with researchers on disclosure timing
3. **Public Disclosure:** After a fix is released, we'll publish:
   - Security advisory with CVE (if applicable)
   - Details in release notes
   - Credit to the researcher (if desired)

---

## Security Hall of Fame

We'd like to thank the following security researchers for responsibly disclosing vulnerabilities:

- *No vulnerabilities reported yet*

---

## Contact

For security-related questions or concerns:

- **Security Email:** subandhyako55@gmail.com
- **GitHub Security:** [Security Advisories](https://github.com/Suban5/LivingDocGen/security/advisories)
- **General Support:** [GitHub Issues](https://github.com/Suban5/LivingDocGen/issues) (for non-security issues only)

---

## Additional Resources

- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [.NET Security Guidelines](https://docs.microsoft.com/en-us/dotnet/standard/security/)
- [NuGet Package Security](https://docs.microsoft.com/en-us/nuget/concepts/security-best-practices)

---

**Last Updated:** January 22, 2026

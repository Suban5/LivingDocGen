# Contributing to LivingDocGen

Thank you for your interest in contributing to LivingDocGen! This document provides guidelines and instructions for contributing to this project.

---

## 📋 Table of Contents

- [Code of Conduct](#code-of-conduct)
- [How Can I Contribute?](#how-can-i-contribute)
- [Development Setup](#development-setup)
- [Coding Standards](#coding-standards)
- [Submitting Changes](#submitting-changes)
- [Reporting Bugs](#reporting-bugs)
- [Suggesting Features](#suggesting-features)

---

## Code of Conduct

This project is part of a Master's thesis and welcomes contributions from the community. Please be respectful and constructive in all interactions.

### Our Standards

- **Be Respectful**: Treat everyone with respect and kindness
- **Be Constructive**: Provide helpful feedback and suggestions
- **Be Collaborative**: Work together to improve the project
- **Be Patient**: Remember that this is an academic project with limited resources

---

## How Can I Contribute?

### 1. Reporting Bugs

Found a bug? Please help us by:

1. **Search existing issues** to avoid duplicates
2. **Create a new issue** with:
   - Clear, descriptive title
   - Steps to reproduce the bug
   - Expected vs. actual behavior
   - Your environment (OS, .NET version, framework)
   - Sample code or feature files (if applicable)
   - Screenshots or error messages

**Template:**
```markdown
**Description:** Brief description of the bug

**Steps to Reproduce:**
1. Step one
2. Step two
3. Step three

**Expected Behavior:** What should happen

**Actual Behavior:** What actually happens

**Environment:**
- OS: Windows/macOS/Linux
- .NET Version: 6.0/8.0/etc.
- LivingDocGen Version: 2.0.0
- BDD Framework: Reqnroll/SpecFlow/Cucumber

**Additional Context:** Any other relevant information
```

### 2. Suggesting Features

Have an idea? We'd love to hear it!

1. **Check existing discussions** to see if it's already proposed
2. **Open a discussion** in the [GitHub Discussions](https://github.com/suban5/LivingDocGen/discussions)
3. **Describe**:
   - The problem you're trying to solve
   - Your proposed solution
   - Alternative solutions you've considered
   - How it fits with the project's goals

### 3. Contributing Code

Want to contribute code? Awesome! Here's how:

1. **Fork the repository**
2. **Create a feature branch**
3. **Make your changes**
4. **Write tests**
5. **Submit a pull request**

See [Development Setup](#development-setup) below for detailed instructions.

### 4. Improving Documentation

Documentation improvements are always welcome!

- Fix typos or unclear explanations
- Add examples or clarifications
- Improve API documentation
- Translate documentation (future)

---

## Development Setup

### Prerequisites

- .NET 6.0 SDK or higher ([Download](https://dotnet.microsoft.com/download))
- Git
- IDE: Visual Studio 2022, VS Code, or JetBrains Rider

### Setup Steps

```bash
# 1. Fork and clone the repository
git clone https://github.com/YOUR-USERNAME/LivingDocGen.git
cd LivingDocGen

# 2. Restore dependencies
dotnet restore

# 3. Build the solution
dotnet build

# 4. Run tests
dotnet test

# 5. Open in your IDE
code .  # VS Code
# or
open LivingDocGen.sln  # Visual Studio
```

### Running the CLI Locally

```bash
# Run without installing
dotnet run --project src/LivingDocGen.CLI/LivingDocGen.CLI.csproj -- generate ./samples/features ./samples/test-results

# Or build and install locally
dotnet pack src/LivingDocGen.CLI/LivingDocGen.CLI.csproj -o ./local-packages
dotnet tool install --global --add-source ./local-packages LivingDocGen.Tool
```

---

## Coding Standards

### C# Style Guide

1. **Naming Conventions**
   - PascalCase: Classes, methods, properties
   - camelCase: Local variables, parameters
   - _camelCase: Private fields
   - UPPER_CASE: Constants

2. **Formatting**
   - 4 spaces for indentation (no tabs)
   - Braces on new lines (Allman style)
   - Maximum 120 characters per line

3. **Code Organization**
   ```csharp
   public class Example
   {
       // 1. Constants
       public const int MAX_SIZE = 100;
       
       // 2. Fields
       private readonly string _field;
       
       // 3. Constructors
       public Example() { }
       
       // 4. Properties
       public string Name { get; set; }
       
       // 5. Public methods
       public void PublicMethod() { }
       
       // 6. Private methods
       private void PrivateMethod() { }
   }
   ```

4. **Comments & Documentation**
   - Use XML documentation for public APIs
   - Add inline comments only for complex logic
   - Keep comments up-to-date with code changes

   ```csharp
   /// <summary>
   /// Parses a Gherkin feature file.
   /// </summary>
   /// <param name="filePath">Path to the feature file.</param>
   /// <returns>Parsed universal feature model.</returns>
   public UniversalFeature ParseFile(string filePath)
   {
       // Implementation
   }
   ```

5. **Compatibility**
   - Libraries target .NET Standard 2.0/2.1
   - Avoid C# 10+ features (no ImplicitUsings, nullable reference types)
   - Explicit using statements
   - Compatible with .NET Framework 4.6.1+

### Testing Standards

1. **Test Naming**: `MethodName_Scenario_ExpectedBehavior`
   ```csharp
   [Fact]
   public void ParseFile_ValidFeature_ReturnsFeature()
   {
       // Arrange
       var parser = new GherkinParser();
       
       // Act
       var result = parser.ParseFile("valid.feature");
       
       // Assert
       Assert.NotNull(result);
   }
   ```

2. **Test Coverage**: Aim for >80% code coverage for new code

3. **Test Organization**: Use Arrange-Act-Assert pattern

---

## Submitting Changes

### Creating a Pull Request

1. **Create a Feature Branch**
   ```bash
   git checkout -b feature/your-feature-name
   # or
   git checkout -b bugfix/issue-number-description
   ```

2. **Make Your Changes**
   - Write clean, maintainable code
   - Follow coding standards
   - Add tests for new functionality
   - Update documentation as needed

3. **Commit Your Changes**
   ```bash
   git add .
   git commit -m "feat: add new feature description"
   ```

   **Commit Message Format:**
   - `feat:` New feature
   - `fix:` Bug fix
   - `docs:` Documentation changes
   - `refactor:` Code refactoring
   - `test:` Adding or updating tests
   - `chore:` Maintenance tasks

4. **Push to Your Fork**
   ```bash
   git push origin feature/your-feature-name
   ```

5. **Open a Pull Request**
   - Go to the original repository
   - Click "New Pull Request"
   - Select your branch
   - Fill out the PR template
   - Link related issues

### Pull Request Guidelines

✅ **DO:**
- Write clear PR descriptions
- Include tests for new features
- Update documentation
- Keep PRs focused on a single concern
- Respond to review feedback promptly

❌ **DON'T:**
- Mix multiple unrelated changes
- Submit large PRs (prefer smaller, incremental changes)
- Break existing functionality
- Ignore CI/CD failures

### PR Template

```markdown
## Description
Brief description of the changes

## Type of Change
- [ ] Bug fix
- [ ] New feature
- [ ] Documentation update
- [ ] Refactoring
- [ ] Performance improvement

## Related Issues
Fixes #123

## Testing
- [ ] Added unit tests
- [ ] Added integration tests
- [ ] All tests pass locally
- [ ] Manual testing performed

## Checklist
- [ ] Code follows project style guidelines
- [ ] Self-review completed
- [ ] Documentation updated
- [ ] No breaking changes (or documented)
```

---

## Review Process

1. **Automated Checks**: CI builds and tests run automatically
2. **Code Review**: Maintainer reviews the code
3. **Feedback**: You may receive feedback or change requests
4. **Approval**: Once approved, your PR will be merged
5. **Recognition**: Contributors are acknowledged in release notes!

---

## Project Structure

Understanding the codebase:

```
LivingDocGen/
├── src/
│   ├── LivingDocGen.Core/          # Foundation library
│   ├── LivingDocGen.Parser/        # Gherkin parser
│   ├── LivingDocGen.TestReporter/  # Test result parsers
│   ├── LivingDocGen.Generator/     # HTML generator
│   ├── LivingDocGen.MSBuild/       # MSBuild integration
│   ├── LivingDocGen.CLI/           # CLI tool
│   └── LivingDocGen.Reqnroll.Integration/  # Reqnroll hooks
├── tests/
│   └── LivingDocGen.Parser.Tests/  # Unit tests
├── samples/                         # Sample files for testing
└── docs/                            # Documentation
```

**Key Files:**
- `docs/DEVELOPMENT.md` - Detailed development guide
- `docs/ARCHITECTURE.md` - Architecture documentation
- `docs/API_REFERENCE.md` - API documentation

---

## Getting Help

Need help contributing?

- **Documentation**: Check [DEVELOPMENT.md](docs/DEVELOPMENT.md)
- **Discussions**: Ask in [GitHub Discussions](https://github.com/suban5/LivingDocGen/discussions)
- **Issues**: Browse [existing issues](https://github.com/suban5/LivingDocGen/issues)

---

## Recognition

All contributors are:
- Listed in release notes
- Acknowledged in the project
- Building something valuable for the BDD community!

---

## Academic Context

This project is part of a Master's thesis research. Contributions help:
- Improve the tool for the community
- Provide real-world validation of the research
- Advance the state of BDD documentation tools

Your contribution matters! 🙏

---

## License

By contributing, you agree that your contributions will be licensed under the MIT License.

---

**Thank you for contributing to LivingDocGen!** ❤️
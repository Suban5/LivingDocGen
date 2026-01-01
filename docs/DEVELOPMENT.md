# Development Guide

Guide for developers contributing to LivingDocGen.

---

## Table of Contents

- [Prerequisites](#prerequisites)
- [Getting Started](#getting-started)
- [Project Structure](#project-structure)
- [Building the Solution](#building-the-solution)
- [Running Tests](#running-tests)
- [Development Workflow](#development-workflow)
- [Coding Standards](#coding-standards)
- [Testing Guidelines](#testing-guidelines)
- [Debugging Tips](#debugging-tips)
- [Publishing Packages](#publishing-packages)

---

## Prerequisites

### Required Tools

1. **.NET SDK 6.0 or higher**
   ```bash
   dotnet --version  # Should be 6.0 or higher
   ```
   Download: https://dotnet.microsoft.com/download

2. **Git**
   ```bash
   git --version
   ```
   Download: https://git-scm.com/downloads

3. **IDE (Choose one)**
   - **Visual Studio 2022** (Recommended for Windows)
     - Community Edition or higher
     - Workload: ".NET desktop development"
   
   - **Visual Studio Code** (Cross-platform)
     - Extension: C# Dev Kit
     - Extension: .NET Extension Pack
   
   - **JetBrains Rider** (Cross-platform, paid)

### Optional Tools

- **NuGet CLI** (for package management)
- **dotnet-format** (for code formatting)
  ```bash
  dotnet tool install -g dotnet-format
  ```

---

## Getting Started

### 1. Clone the Repository

```bash
git clone https://github.com/Suban5/LivingDocGen.git
cd LivingDocGen
```

### 2. Restore Dependencies

```bash
dotnet restore
```

### 3. Build the Solution

```bash
dotnet build
```

### 4. Run Tests

```bash
dotnet test
```

### 5. Open in IDE

**Visual Studio:**
```bash
# Windows
start LivingDocGen.sln

# macOS
open LivingDocGen.sln
```

**VS Code:**
```bash
code .
```

---

## Project Structure

```
LivingDocGen/
├── src/
│   ├── LivingDocGen.Core/              # Foundation library
│   ├── LivingDocGen.Parser/            # Gherkin parser
│   ├── LivingDocGen.TestReporter/      # Test result parsers
│   ├── LivingDocGen.Generator/         # HTML generator
│   ├── LivingDocGen.MSBuild/           # MSBuild integration
│   ├── LivingDocGen.CLI/               # CLI tool
│   └── LivingDocGen.Reqnroll.Integration/  # Reqnroll hooks
├── tests/
│   └── LivingDocGen.Parser.Tests/      # Unit tests
├── samples/
│   ├── features/                       # Sample feature files
│   ├── StepDefinitions/                # Sample step definitions
│   └── test-results/                   # Sample test results
├── docs/                               # Documentation
├── schemas/                            # JSON schemas
├── LivingDocGen.sln                    # Solution file
├── README.md
├── CHANGELOG.md
├── LICENSE
└── CONTRIBUTING.md
```

### Key Directories

- **`src/`** - All production code
- **`tests/`** - All test projects
- **`samples/`** - Sample files for testing and demonstration
- **`docs/`** - Documentation files
- **`schemas/`** - JSON schema definitions

---

## Building the Solution

### Build All Projects

```bash
# Debug build (default)
dotnet build

# Release build
dotnet build -c Release
```

### Build Specific Project

```bash
dotnet build src/LivingDocGen.Parser/LivingDocGen.Parser.csproj
```

### Clean Build

```bash
dotnet clean
dotnet build
```

### Build Output Locations

- **Debug:** `src/[ProjectName]/bin/Debug/netstandard2.0/`
- **Release:** `src/[ProjectName]/bin/Release/netstandard2.0/`

---

## Running Tests

### Run All Tests

```bash
dotnet test
```

### Run Tests with Verbosity

```bash
dotnet test --verbosity detailed
```

### Run Tests with Coverage

```bash
# Install coverage tool
dotnet tool install --global dotnet-coverage

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Run Specific Test Class

```bash
dotnet test --filter "FullyQualifiedName~GherkinParserTests"
```

### Run Tests in Watch Mode

```bash
dotnet watch test
```

---

## Development Workflow

### 1. Create a Feature Branch

```bash
git checkout -b feature/your-feature-name
```

Branch naming conventions:
- `feature/` - New features
- `bugfix/` - Bug fixes
- `docs/` - Documentation updates
- `refactor/` - Code refactoring

### 2. Make Changes

- Write code following [Coding Standards](#coding-standards)
- Write tests for new functionality
- Update documentation if needed

### 3. Test Your Changes

```bash
# Run tests
dotnet test

# Check for errors
dotnet build

# Format code (if dotnet-format installed)
dotnet format
```

### 4. Commit Changes

```bash
git add .
git commit -m "feat: add new feature description"
```

Commit message format:
- `feat:` - New feature
- `fix:` - Bug fix
- `docs:` - Documentation changes
- `refactor:` - Code refactoring
- `test:` - Adding tests
- `chore:` - Maintenance tasks

### 5. Push and Create PR

```bash
git push origin feature/your-feature-name
```

Then create a Pull Request on GitHub.

---

## Coding Standards

### C# Style Guidelines

1. **Naming Conventions**
   - PascalCase for classes, methods, properties
   - camelCase for local variables, parameters
   - _camelCase for private fields
   - UPPER_CASE for constants

   ```csharp
   public class GherkinParser
   {
       private readonly string _filePath;
       public const int MAX_LINE_LENGTH = 1000;
       
       public UniversalFeature ParseFile(string filePath)
       {
           var feature = new UniversalFeature();
           return feature;
       }
   }
   ```

2. **Indentation**
   - Use 4 spaces (no tabs)
   - Braces on new lines (Allman style)

   ```csharp
   if (condition)
   {
       DoSomething();
   }
   ```

3. **Line Length**
   - Maximum 120 characters per line
   - Break long lines sensibly

4. **Using Directives**
   - Explicit using statements (no ImplicitUsings)
   - Order: System → External → Internal
   - One using per line

   ```csharp
   using System;
   using System.Collections.Generic;
   using System.Linq;
   using LivingDocGen.Core.Exceptions;
   ```

5. **Null Handling**
   - No nullable reference types (for .NET Standard compatibility)
   - Check for null explicitly
   - Use null-coalescing operator `??` when appropriate

   ```csharp
   public void Process(string input)
   {
       if (input == null)
           throw new ArgumentNullException(nameof(input));
       
       var value = input ?? "default";
   }
   ```

6. **Comments**
   - Use XML documentation for public APIs
   - Inline comments for complex logic only
   - Keep comments up-to-date

   ```csharp
   /// <summary>
   /// Parses a Gherkin feature file.
   /// </summary>
   /// <param name="filePath">Path to the feature file.</param>
   /// <returns>Parsed universal feature model.</returns>
   /// <exception cref="ParseException">Thrown when parsing fails.</exception>
   public UniversalFeature ParseFile(string filePath)
   {
       // Implementation
   }
   ```

### Code Organization

1. **Class Structure Order**
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

2. **Single Responsibility**
   - One class = one responsibility
   - Keep classes focused and cohesive

3. **DRY (Don't Repeat Yourself)**
   - Extract common code into reusable methods
   - Use inheritance and composition appropriately

---

## Testing Guidelines

### Test Organization

```csharp
namespace LivingDocGen.Parser.Tests
{
    public class GherkinParserTests
    {
        [Fact]
        public void ParseFile_ValidFeature_ReturnsFeature()
        {
            // Arrange
            var parser = new GherkinParser();
            var featurePath = "test-data/valid.feature";
            
            // Act
            var result = parser.ParseFile(featurePath);
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal("Login", result.Name);
        }
    }
}
```

### Test Naming Convention

```
MethodName_Scenario_ExpectedBehavior
```

Examples:
- `ParseFile_ValidFeature_ReturnsFeature`
- `Parse_InvalidSyntax_ThrowsParseException`
- `EnrichFeatures_NoTestResults_ReturnsUnchangedFeatures`

### Test Categories

1. **Unit Tests** - Test individual methods/classes
2. **Integration Tests** - Test component interactions
3. **End-to-End Tests** - Test complete pipelines

### Test Data

- Store test data in `tests/[ProjectName]/TestData/`
- Use sample files from `samples/` directory
- Clean up temporary files in test cleanup

---

## Debugging Tips

### Debug in Visual Studio

1. Set breakpoints in code
2. Press F5 to start debugging
3. Use Immediate Window for expressions
4. Watch variables in Locals/Watch windows

### Debug CLI Tool Locally

```bash
# Build CLI
dotnet build src/LivingDocGen.CLI/LivingDocGen.CLI.csproj

# Run locally (not as global tool)
dotnet run --project src/LivingDocGen.CLI/LivingDocGen.CLI.csproj -- generate ./samples/features ./samples/test-results -o debug-output.html
```

### Debug MSBuild Task

Add to test project `.csproj`:
```xml
<PropertyGroup>
  <MSBuildDebugEngine>1</MSBuildDebugEngine>
</PropertyGroup>
```

### Debug Tests

```bash
# Run with diagnostic logging
dotnet test --logger "console;verbosity=detailed"
```

### Common Issues

**Issue:** "The type or namespace name could not be found"
- Solution: Restore packages with `dotnet restore`

**Issue:** "Build failed with 0 errors"
- Solution: Clean and rebuild: `dotnet clean && dotnet build`

**Issue:** Tests not discovered
- Solution: Rebuild test project: `dotnet build tests/LivingDocGen.Parser.Tests/`

---

## Publishing Packages

### Version Numbering

Follow [Semantic Versioning](https://semver.org/):
- **MAJOR.MINOR.PATCH** (e.g., 1.0.3)
- MAJOR: Breaking changes
- MINOR: New features (backward compatible)
- PATCH: Bug fixes (backward compatible)

### Update Version

Edit version in `.csproj` files:
```xml
<PropertyGroup>
  <Version>1.0.4</Version>
</PropertyGroup>
```

Update in these files:
- `src/LivingDocGen.CLI/LivingDocGen.CLI.csproj`
- `src/LivingDocGen.MSBuild/LivingDoc.MSBuild.csproj`
- `src/LivingDocGen.Reqnroll.Integration/LivingDocGen.Reqnroll.Integration.csproj`

### Build Release Packages

```bash
# Clean previous builds
dotnet clean -c Release

# Build in Release mode
dotnet build -c Release

# Create NuGet packages
dotnet pack -c Release --no-build -o ./nupkg
```

### Test Packages Locally

```bash
# Install CLI tool locally
dotnet tool install --global --add-source ./nupkg LivingDocGen.Tool

# Test it
LivingDocGen --version

# Uninstall when done
dotnet tool uninstall -g LivingDocGen.Tool
```

### Publish to NuGet.org

1. **Get API Key**
   - Go to https://www.nuget.org
   - Sign in → API Keys → Create
   - Copy the key

2. **Push Packages**
   ```bash
   # Push each package
   dotnet nuget push ./nupkg/LivingDocGen.Tool.1.0.4.nupkg --api-key YOUR_API_KEY --source https://api.nuget.org/v3/index.json
   
   dotnet nuget push ./nupkg/LivingDocGen.MSBuild.1.0.4.nupkg --api-key YOUR_API_KEY --source https://api.nuget.org/v3/index.json
   
   dotnet nuget push ./nupkg/LivingDocGen.Reqnroll.Integration.1.0.4.nupkg --api-key YOUR_API_KEY --source https://api.nuget.org/v3/index.json
   ```

3. **Verify on NuGet.org**
   - Check package pages
   - Verify package metadata
   - Test installation

### Create GitHub Release

1. Tag the release:
   ```bash
   git tag -a v1.0.4 -m "Release v1.0.4"
   git push origin v1.0.4
   ```

2. Create release on GitHub:
   - Go to Releases → Draft a new release
   - Select tag `v1.0.4`
   - Add release notes from CHANGELOG.md
   - Attach `.nupkg` files (optional)
   - Publish release

---

## Environment Setup

### macOS/Linux

```bash
# Install .NET SDK
brew install dotnet-sdk

# Verify installation
dotnet --version

# Install optional tools
dotnet tool install -g dotnet-format
dotnet tool install -g dotnet-coverage
```

### Windows

```powershell
# Download and install .NET SDK from:
# https://dotnet.microsoft.com/download

# Verify installation
dotnet --version

# Install optional tools
dotnet tool install -g dotnet-format
dotnet tool install -g dotnet-coverage
```

### VS Code Setup

Install extensions:
```bash
code --install-extension ms-dotnettools.csdevkit
code --install-extension ms-dotnettools.csharp
```

Recommended `settings.json`:
```json
{
  "editor.formatOnSave": true,
  "omnisharp.enableRoslynAnalyzers": true,
  "omnisharp.enableEditorConfigSupport": true
}
```

---

## Useful Commands

### Build & Test

```bash
# Clean solution
dotnet clean

# Restore packages
dotnet restore

# Build solution
dotnet build

# Build specific project
dotnet build src/LivingDocGen.Parser/

# Run tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Build Release
dotnet build -c Release

# Create packages
dotnet pack -c Release -o ./nupkg
```

### Project Management

```bash
# Add new project
dotnet new classlib -n LivingDocGen.NewProject -o src/LivingDocGen.NewProject

# Add project to solution
dotnet sln add src/LivingDocGen.NewProject/LivingDocGen.NewProject.csproj

# Add project reference
dotnet add src/ProjectA/ProjectA.csproj reference src/ProjectB/ProjectB.csproj

# Add NuGet package
dotnet add package PackageName

# List packages
dotnet list package

# Check for vulnerable packages
dotnet list package --vulnerable
```

### Git Workflow

```bash
# Create feature branch
git checkout -b feature/my-feature

# Commit changes
git add .
git commit -m "feat: add new feature"

# Push branch
git push -u origin feature/my-feature

# Update from main
git fetch origin
git rebase origin/main

# Squash commits (interactive rebase)
git rebase -i HEAD~3
```

---

## CI/CD Pipeline

### GitHub Actions (Future)

Planned workflow:
- Build on push
- Run tests
- Check code coverage
- Publish packages on release

---

## Getting Help

- **Documentation:** See `docs/` directory
- **Issues:** https://github.com/Suban5/LivingDocGen/issues
- **Discussions:** https://github.com/Suban5/LivingDocGen/discussions
- **Contributing:** See `CONTRIBUTING.md`

---

**Last Updated:** January 1, 2026
**Version:** 1.0.4


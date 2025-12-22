# Changelog

All notable changes to LivingDocGen will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [1.0.3] - 2024-12-22

### 🎯 Major Framework Compatibility Update

This release significantly improves framework compatibility by migrating to .NET Standard, enabling the library to support a much wider range of .NET framework versions.

### Changed

- **Breaking:** Migrated library projects to .NET Standard 2.0/2.1 for maximum compatibility
  - `LivingDocGen.Core`: `net8.0` → `netstandard2.0`
  - `LivingDocGen.Parser`: `net8.0` → `netstandard2.0`
  - `LivingDocGen.TestReporter`: `net8.0` → `netstandard2.0`
  - `LivingDocGen.Generator`: `net8.0` → `netstandard2.1` (required by RazorEngine.NetCore dependency)
  - `LivingDocGen.MSBuild`: `net8.0` → `netstandard2.0`
  
- **Breaking:** Migrated executable projects to .NET 6.0 minimum
  - `LivingDocGen.CLI`: `net8.0` → `net6.0`
  - `LivingDocGen.Reqnroll.Integration`: `net8.0` → `net6.0`

- Removed C# 10+ features for broader compatibility:
  - Removed `ImplicitUsings` - all using directives are now explicit
  - Removed nullable reference type annotations (C# 8.0 feature)
  - Replaced target-typed `new()` expressions with explicit constructors

### Added

- Explicit using directives across all source files for clarity and compatibility
- `System.Text.Json` package dependency (version 8.0.5) to `LivingDocGen.TestReporter` for .NET Standard 2.0 support

### Fixed

- `String.Split()` overload compatibility for .NET Standard 2.0
- Nullable type handling across all models and services
- Method parameter types to remove nullable annotations

### Compatibility

**Libraries now support:**
- ✅ .NET Framework 4.6.1 and higher
- ✅ .NET Core 2.0 and higher
- ✅ .NET 5, 6, 7, 8 and future versions
- ✅ Xamarin, Mono, and Unity (via .NET Standard 2.0)

**CLI and Reqnroll Integration require:**
- ✅ .NET 6.0 runtime or higher

This provides much broader ecosystem compatibility for NuGet package consumers while maintaining modern runtime support for executables.

---

## [1.0.2] - 2024-12-XX

### Added
- Enhanced test result parsing with better error handling
- Improved theme customization options

### Fixed
- Minor bug fixes in HTML generation
- Performance improvements in large feature file parsing

---

## [1.0.1] - 2024-12-XX

### Added
- Support for SpecFlow JSON execution reports
- Additional theme: Pickles-style classic theme
- Enhanced error reporting with stack traces

### Fixed
- TRX parser compatibility issues
- Theme switching persistence in browser

---

## [1.0.0] - 2024-12-XX

### 🎉 Initial Release

First public release of LivingDocGen - Universal BDD Living Documentation Generator.

### Features

#### Core Functionality
- Universal Gherkin parser supporting all BDD frameworks
- Multi-format test result parser (NUnit, xUnit, JUnit, TRX, SpecFlow JSON)
- Single-file HTML documentation generation
- Zero-configuration setup

#### BDD Framework Support
- Reqnroll (.NET)
- SpecFlow (.NET)
- Cucumber (Java/Ruby/JS)
- JBehave (Java)

#### Test Result Format Support
- NUnit 2 & 3 (XML format)
- NUnit 4 (TRX format)
- xUnit (XML format)
- JUnit (XML format)
- MSTest (TRX format)
- SpecFlow JSON execution reports

#### Documentation Features
- 6 interactive themes (Purple, Blue, Green, Dark, Light, Pickles)
- Live theme switching with localStorage persistence
- Responsive design for all devices
- Interactive filtering by tags, status, and features
- Collapsible sections for features and scenarios
- Step-level execution results
- Error details with stack traces and line numbers
- Execution metrics and statistics

#### Integration Options
- **Global CLI Tool** (`LivingDocGen.Tool`) - Works with any framework
- **MSBuild Integration** (`LivingDocGen.MSBuild`) - Automatic generation after tests
- **Reqnroll Hooks** (`LivingDocGen.Reqnroll.Integration`) - Built-in [AfterTestRun] hook

#### Configuration
- JSON schema support for `bdd-livingdoc.json`
- Customizable output paths and file names
- Theme selection and customization
- Include/exclude tag filtering
- Custom branding support

---

## [Unreleased]

### Planned Features
- Additional export formats (PDF, Markdown)
- Custom template support
- CI/CD pipeline examples
- Azure DevOps and GitHub Actions integration guides
- Historical trend reporting
- Test execution comparison across runs

---

## Version Support

| Version | .NET Support | Status |
|---------|--------------|--------|
| 1.0.3   | .NET Standard 2.0/2.1, .NET 6+ | ✅ Current |
| 1.0.2   | .NET 8.0 only | ⚠️ Legacy |
| 1.0.1   | .NET 8.0 only | ⚠️ Legacy |
| 1.0.0   | .NET 8.0 only | ⚠️ Legacy |

---

[1.0.3]: https://github.com/Suban5/LivingDocGen/releases/tag/v1.0.3
[1.0.2]: https://github.com/Suban5/LivingDocGen/releases/tag/v1.0.2
[1.0.1]: https://github.com/Suban5/LivingDocGen/releases/tag/v1.0.1
[1.0.0]: https://github.com/Suban5/LivingDocGen/releases/tag/v1.0.0
[Unreleased]: https://github.com/Suban5/LivingDocGen/compare/v1.0.3...HEAD

# Changelog

All notable changes to LivingDocGen will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [Unreleased]

### Added

### Changed

### Fixed

### Removed

---

## [2.0.1] - 2026-01-16

### Added

- **Gherkin Comment Rendering**: Comments from `.feature` files are now displayed in HTML documentation
  - Parser captures comments associated with features and scenarios
  - HTML generator renders comments with custom styling
  - CSS includes `.comments` and `.comment` classes with monospace font and `#` prefix
  - `IncludeComments` configuration option (default: `true`)

- **HTML Report Enhancements**: Improved visual layout and user experience
  - Increased layout container height for better content visibility
  - Enhanced responsive design for optimal viewing
  - Added CLI test automation script for validation

- **Generator Performance**: Thread-safe caching and parallel processing
  - Implemented thread-safe caching mechanism
  - Added dependency injection (DI) support
  - Parallel processing for improved generation speed

### Changed

- **UI/UX Improvements**: Streamlined interface elements
  - Decreased stats and footer height for more content space
  - Features now always expanded (non-collapsible) for better accessibility
  - Simplified table headers (removed "Data Table (X×Y)" and "Examples Table (X×Y)" labels)
  - Table headers now clickable to toggle table body visibility
  - Toggle button now only affects scenarios

- **Statistics**: Enhanced calculation logic
  - Pass/fail/skip percentages now calculated based on executed scenarios (excluding untested)
  - Added `FailRate` and `SkipRate` properties to `DocumentStatistics`
  - `HtmlGenerationOptions.IncludeComments` now defaults to `true`
  - `GenerationConfig` in Reqnroll Integration includes `IncludeComments` property

### Fixed

- Layout spacing and container height issues
- Statistics calculation accuracy for pass/fail/skip rates
- Table header interactions

### Developer Notes

- Test projects updated to use project references instead of NuGet packages for local development
- Added comprehensive test script for CLI validation

---

## [2.0.0] - 2026-01-01

### 🎉 Major Release: Reqnroll Integration Refactoring & Clean Architecture

This major release introduces significant architectural improvements to the Reqnroll integration package, focusing on clarity, maintainability, and honest API design.

### Changed - Reqnroll Integration (BREAKING)

- **Breaking:** Refactored hook architecture to use explicit Bootstrap API pattern
  - Removed misleading `[Binding]` attributes from package hooks (they were never auto-discovered)
  - Renamed `LivingDocumentationHooks` → `LivingDocBootstrap` (clearer naming)
  - Changed namespace: `LivingDocGen.Reqnroll.Integration.Hooks` → `LivingDocGen.Reqnroll.Integration.Bootstrap`
  - Changed class modifier: `public class` → `public static class` (proper API design)

- **Breaking:** Folder structure renamed for clarity
  - `Hooks/` → `Bootstrap/` (more accurate representation)

- **Improved:** Documentation now honestly explains Reqnroll limitation
  - Bridge file requirement clearly documented with explanation
  - Complete troubleshooting guide for common issues
  - Visual diagrams showing architecture and data flow

### Added - Reqnroll Integration

- Comprehensive README with complete setup instructions
- Bridge file code template ready for copy-paste
- Test results integration guide (NUnit runsettings configuration)
- VS Code integration troubleshooting (Test Explorer limitations)
- FAQ section addressing common questions

### Added - CLI Tool

- Disabled Git commit hash in version output for cleaner display
  - Added `IncludeSourceRevisionInInformationalVersion=false`
  - Version now shows `2.0.0` instead of `2.0.0+<git-hash>`

### Added - Documentation

- Main README updated with detailed Reqnroll integration guide
- New section: "Reqnroll Integration Deep Dive"
  - Bridge pattern explanation with diagrams
  - Configuration options and defaults
  - Complete troubleshooting section
- Enhanced FAQ with Reqnroll-specific questions

### Fixed

- Honest API design: Package no longer pretends hooks are auto-discovered
- Clear separation between bootstrap API and actual Reqnroll hooks
- Improved user experience with upfront transparency about requirements

### Migration Guide (v1.x → v2.0)

If you're using `LivingDocGen.Reqnroll.Integration`, update your bridge file:

**Old (v1.x):**
```csharp
using LivingDocGen.Reqnroll.Integration.Hooks;
LivingDocumentationHooks.BeforeTestRun();
LivingDocumentationHooks.AfterTestRun();
```

**New (v2.0):**
```csharp
using LivingDocGen.Reqnroll.Integration.Bootstrap;
LivingDocBootstrap.BeforeTestRun();
LivingDocBootstrap.AfterTestRun();
```

Simply update the namespace and class name in your bridge file - functionality remains identical.

---

## [1.0.4] - 2025-01-01

### Changed

- Updated documentation with comprehensive API references
- Improved component README files with clearer examples
- Enhanced DEVELOPMENT.md with additional debugging tips

### Fixed

- Minor documentation inconsistencies
- Updated copyright year to 2026
- Corrected target framework references in documentation

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

## [1.0.2] - 2024-12-15

### Added
- Enhanced test result parsing with better error handling
- Improved theme customization options

### Fixed
- Minor bug fixes in HTML generation
- Performance improvements in large feature file parsing

---

## [1.0.1] - 2024-12-10

### Added
- Support for SpecFlow JSON execution reports
- Additional theme: Pickles-style classic theme
- Enhanced error reporting with stack traces

### Fixed
- TRX parser compatibility issues
- Theme switching persistence in browser

---

## [1.0.0] - 2024-12-01

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
| 1.0.4   | .NET Standard 2.0/2.1, .NET 6+ | ✅ Current |
| 1.0.3   | .NET Standard 2.0/2.1, .NET 6+ | ✅ Stable |
| 1.0.2   | .NET 8.0 only | ⚠️ Legacy |
| 1.0.1   | .NET 8.0 only | ⚠️ Legacy |
| 1.0.0   | .NET 8.0 only | ⚠️ Legacy |

---

[1.0.4]: https://github.com/suban5/LivingDocGen/releases/tag/v1.0.4
[1.0.3]: https://github.com/suban5/LivingDocGen/releases/tag/v1.0.3
[1.0.2]: https://github.com/suban5/LivingDocGen/releases/tag/v1.0.2
[1.0.1]: https://github.com/suban5/LivingDocGen/releases/tag/v1.0.1
[1.0.0]: https://github.com/suban5/LivingDocGen/releases/tag/v1.0.0
[Unreleased]: https://github.com/suban5/LivingDocGen/compare/v1.0.4...HEAD

# Changelog - LivingDocGen.Reqnroll.Integration

All notable changes to the LivingDocGen Reqnroll Integration will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [Unreleased]

### Added

- **Enhanced diagnostic logging in LivingDocBootstrap.cs**
  - Comprehensive console output during test execution
  - Full file paths for all key operations (test results, features, HTML output)
  - File metadata: size (bytes + KB), modification timestamps
  - Configuration values display: feature path, output location, theme
  - Structured error messages with error type and troubleshooting guidance
  - Debug log file (LIVINGDOC_DEBUG.txt) with detailed execution diagnostics
  - Browser-ready file:// links for generated HTML reports
- **Debug log file documentation in README.md**
  - Explanation of LIVINGDOC_DEBUG.txt contents and location
  - When to check the debug log for troubleshooting
  - Example debug log entries with real diagnostic output

### Changed

- **Upgraded Reqnroll from 2.4.1 to 3.3.2**
  - **Breaking Change**: Consuming projects must use Reqnroll 3.3.2 or higher
  - Improved compatibility with latest Reqnroll features
  - Enhanced test result parsing for Scenario Outlines
- **Comprehensive README.md rewrite for accessibility**
  - Added "Integration vs CLI" distinction section (no CLI installation needed)
  - Expanded installation instructions with system requirements and 3 installation options
  - Detailed bridge file setup with visual project structure examples
  - Rewritten troubleshooting guide with 4 problem/solution scenarios:
    * Problem 1: No HTML Report Created (6-step diagnostic)
    * Problem 2: Features Directory Not Found (fix options with examples)
    * Problem 3: Report Shows No Test Results (detailed explanation)
    * Problem 4: Bridge File Not Working (5-step resolution)
  - Configuration explanations simplified with comparison tables
  - Replaced CI/CD integration examples with practical Reqnroll usage examples
  - Improved clarity with plain language throughout (accessible to beginners)
- Updated Gherkin library to 35.0.0 (aligned with Reqnroll 3.3.2)
  - Ensures version consistency with Reqnroll 3.3.2 dependency
  - Resolves MissingMethodException with Gherkin API compatibility

### Fixed

- Test results now correctly display for Scenario Outlines (inherited from TestReporter)
  - Fixed NUnit parser to recursively find all nested test-case elements
  - Resolves missing test statistics in generated HTML reports
  - Test statistics now accurate: 24 passed, 10 failed, 6 skipped (40 total scenarios)

### Removed

---

## [2.0.5] - 2026-01-26

### Added

- Tag filtering functionality for better scenario organization (inherited from Core)
  - Filter scenarios by tags using dropdown selector
  - Feature-level and scenario-level tag support
  - Case-insensitive tag matching

### Changed

- Reorganized controls layout for improved UX (inherited from Core)
  - New logical order: Status filters → Tag filter → Search → Clear All → Theme
  - Better visual grouping and filtering workflow

### Fixed

- Tag filtering extraction and matching issues (inherited from Core)
  - Fixed tag selector targeting and Font Awesome icon interference
  - Tag filtering now displays matching scenarios correctly

- Search navigation improvements (inherited from Core)
  - Fixed ReferenceError with prev/next buttons
  - Improved button state management

- Untested scenario count calculation (inherited from Core)
  - Now uses formula-based calculation for accuracy
  - Correctly shows all untested scenarios

### Removed

- Sidebar search feature (inherited from Core)
  - Simplified navigation, main search still available

---

## How to Use

### Installation

Install the NuGet package in your Reqnroll test project:

```bash
dotnet add package LivingDocGen.Reqnroll.Integration
```

Or via Package Manager:

```powershell
Install-Package LivingDocGen.Reqnroll.Integration
```

### Setup (Required)

Due to Reqnroll's architecture, you need to create a **bridge file** in your test project to connect the integration:

**Step 1:** Create a file named `LivingDocGenHooks.cs` in your test project:

```csharp
using Reqnroll;
using LivingDocGen.Reqnroll.Integration.Bootstrap;

namespace YourTestProject.Hooks
{
    [Binding]
    public class LivingDocGenHooks
    {
        [BeforeTestRun]
        public static void BeforeTestRun()
        {
            LivingDocBootstrap.BeforeTestRun();
        }

        [AfterTestRun]
        public static void AfterTestRun()
        {
            LivingDocBootstrap.AfterTestRun();
        }
    }
}
```

**Step 2:** Create a `livingdocgen.json` configuration file in your test project root:

```json
{
  "featuresPath": "./Features",
  "testResultsPath": "./bin/Debug/net8.0/TestResultsPath/",
  "outputPath": "./living-doc.html",
  "title": "My Project Living Documentation",
  "theme": "blue",
  "includeComments": true
}
```

**Step 3:** Configure test results in your `.runsettings` file:

```xml
<?xml version="1.0" encoding="utf-8"?>
<RunSettings>
  <NUnit>
    <WorkDirectory>./TestResults</WorkDirectory>
  </NUnit>
</RunSettings>
```

**Step 4:** Run your tests:

```bash
dotnet test --settings test.runsettings
```

Living documentation will be automatically generated in your output directory after test execution completes.

### Configuration Options

All options from `livingdocgen.json`:

- `featuresPath` - Path to Gherkin feature files (default: `"./Features"`)
- `testResultsPath` - Path to test results file (required)
- `outputPath` - Output HTML file path (default: `"./living-doc.html"`)
- `title` - Documentation title (default: `"Living Documentation"`)
- `theme` - Theme name: `default`, `blue`, `green`, `dark`, `light`, `pickles`
- `includeComments` - Include Gherkin comments in output (default: `true`)

### Why the Bridge File?

Reqnroll doesn't auto-discover hooks from external assemblies (NuGet packages). The bridge file is a simple pattern that:
- Lives in your test project (so Reqnroll can find it)
- Forwards calls to our integration package
- Gives you full control over when documentation is generated

For more details, see: https://github.com/suban5/LivingDocGen

---

## [Unreleased]

### Added

- Tag filtering functionality (inherited from Generator)
  - Filter scenarios by tags with dropdown selector
  - Feature-level and scenario-level tag support
  - Case-insensitive tag matching

### Changed

- Reorganized controls layout for better UX (inherited from Generator)
  - Status filters → Tag filter → Search → Clear All → Theme

### Fixed

- Tag filtering now correctly displays matching scenarios (inherited from Generator)
- Search navigation ReferenceError resolved (inherited from Generator)
- Untested scenario count now calculated correctly (inherited from Generator)
  - Uses formula: Total - (Passed + Failed + Skipped)
  - Accurately shows all untested scenarios

### Removed

- Sidebar search feature (inherited from Generator)
  - Main search in top controls provides comprehensive functionality

---

## [2.0.4] - 2026-01-22

### Fixed

- **Critical Bug Fixes** (inherited from Generator):
  - Generated reports with 50+ features now have fully functional sidebar navigation
  - Search works correctly with dynamically loaded feature content
  - Tag filtering displays features properly in main content area
  - Background, Rule, and Examples sections toggle correctly
  - Fixed content overflow when Rule and Background sections collapsed

- **UI/UX Improvements** (inherited from Generator):
  - More compact step display (keywords and text on same line)
  - Search result navigation with previous/next buttons
  - Keyboard shortcuts for search navigation (Enter/Shift+Enter/Esc)
  - Improved search performance (feature titles and scenario names only)

### Details

- All 12 bug fixes from Generator v2.0.4 applied to generated reports
- Large test suites (50+ feature files) now produce fully functional HTML reports
- Search, filtering, and navigation work seamlessly with lazy-rendered content
- Collapsible sections (Background, Rules, Examples) now behave correctly

**Impact**: Test projects with extensive feature coverage now generate reliable, fully-functional living documentation.

---

## [2.0.3] - 2026-01-22

### Changed

- Phase 2 performance optimizations for large test suites (inherited from Generator)
  - Lazy rendering: Reports with 50+ features load progressively on scroll
  - Handles 200+ feature files with 500+ scenarios smoothly
  - Initial page load 87% faster (12s → 1.5s), time to interactive 86% faster (18s → 2.5s)
  - Memory efficient: 66% reduction in browser memory usage (350MB → 120MB)
  - Smooth 60fps scrolling with instant toggle response
  - Automatic activation for reports with 50+ features

---

## [2.0.2] - 2026-01-19

### Added

### Changed

- **Performance optimizations for large test suites (1000+ scenarios)**
  - Replaced `Console.WriteLine` with `Trace.WriteLine` for proper test runner output visibility
  - Reduced test result file wait time from 3 seconds to 1 second
  - Updated XML documentation to recommend `[BeforeTestRun]`/`[AfterTestRun]` hooks instead of `[BeforeScenario]`
  - Optimized for minimal overhead with large scenario counts

- Improved HTML report performance (inherited from Generator)
  - 3-4x faster rendering for 500-1000 scenarios
  - 5-10x faster for 1000-1800 scenarios
  - Smooth animations with no UI freezing

### Fixed

### Removed

- Expand All/Collapse All button from reports (inherited from Generator)

---

## [2.0.1] - 2026-01-16

### Added

- **Gherkin Comment Support**: Display comments from `.feature` files in generated documentation
  - Enabled by default via `includeComments` configuration option
  - Comments rendered with custom styling in HTML output

- **Performance Enhancements**: Thread-safe and parallel processing
  - Implemented thread-safe caching mechanism
  - Added dependency injection (DI) support
  - Parallel processing for faster documentation generation

- **Enhanced HTML Report**: Improved visual layout
  - Better content visibility with optimized layout height
  - Streamlined stats and footer design

### Changed

- **UI/UX Improvements**: Better user experience
  - Features now always expanded (non-collapsible)
  - Simplified table headers (removed dimension labels)
  - Clickable table headers to toggle table body
  - Toggle button now only affects scenarios

- **Statistics Accuracy**: Improved calculation logic
  - Pass/fail/skip rates now based on executed scenarios only
  - Excludes untested scenarios from percentage calculations
  - Added `FailRate` and `SkipRate` to statistics output

### Fixed

- Statistics calculation accuracy for pass/fail/skip percentages
- Layout spacing and container height issues
- Table header interactions and toggles

---

## [2.0.0] - 2026-01-01

### 🎉 Major Release: Reqnroll Integration Refactoring & Clean Architecture

This major release introduces significant architectural improvements, focusing on clarity, maintainability, and honest API design.

### Changed - BREAKING

- **Breaking:** Refactored hook architecture to use explicit Bootstrap API pattern
  - Removed misleading `[Binding]` attributes from package hooks (they were never auto-discovered)
  - Renamed `LivingDocumentationHooks` → `LivingDocBootstrap` (clearer naming)
  - Changed namespace: `LivingDocGen.Reqnroll.Integration.Hooks` → `LivingDocGen.Reqnroll.Integration.Bootstrap`
  - Changed class modifier: `public class` → `public static class` (proper API design)

- **Breaking:** Folder structure renamed for clarity
  - `Hooks/` → `Bootstrap/` (more accurate representation)

- **Breaking:** Migrated to .NET 6.0 minimum (from .NET 8.0)
  - Integration package now targets `net6.0`
  - Better compatibility across different .NET versions

### Added

- Comprehensive README with complete setup instructions
- Bridge file code template ready for copy-paste
- Test results integration guide (NUnit runsettings configuration)
- VS Code integration troubleshooting (Test Explorer limitations)
- FAQ section addressing common questions
- Complete troubleshooting guide for common issues
- Visual diagrams showing architecture and data flow

### Improved

- Documentation now honestly explains Reqnroll limitation
- Clear separation between bootstrap API and actual Reqnroll hooks
- Improved user experience with upfront transparency about requirements

### Migration Guide (v1.x → v2.0)

If you're upgrading from v1.x, update your bridge file:

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
- Improved README with clearer examples

### Fixed

- Minor documentation inconsistencies
- Updated copyright year to 2026

---

## [1.0.3] - 2024-12-22

### Changed

- **Breaking:** Migrated to .NET 6.0 minimum (from .NET 8.0)
  - Integration package now targets `net6.0`
  - Removed C# 10+ features for broader compatibility
  - Removed `ImplicitUsings` - all using directives are now explicit
  - Removed nullable reference type annotations

### Added

- Explicit using directives across all source files for clarity

### Compatibility

**Package now requires:**
- ✅ .NET 6.0 runtime or higher
- ✅ Works with Reqnroll on .NET 6, 7, 8 and future versions

---

## [1.0.2] - 2024-12-15

### Added

- Enhanced configuration options
- Better error handling for missing configuration files

### Fixed

- Minor bug fixes in bootstrap initialization
- Performance improvements

---

## [1.0.1] - 2024-12-10

### Added

- Enhanced error reporting
- Better integration with Reqnroll plugin system

### Fixed

- Configuration file path resolution issues

---

## [1.0.0] - 2024-12-01

### 🎉 Initial Release

First public release of LivingDocGen.Reqnroll.Integration.

### Features

#### Core Integration
- Seamless integration with Reqnroll BDD framework
- Automatic documentation generation after test execution
- Support for Reqnroll hooks and plugins
- Bootstrap mechanism for automatic initialization

#### Configuration
- JSON configuration file support (`livingdocgen.json`)
- Flexible path configuration
- Theme selection support
- Test results integration

#### Documentation Generation
- Automatic feature file discovery
- Test results correlation
- Single-file HTML output
- Interactive documentation with themes

---

[Unreleased]: https://github.com/suban5/LivingDocGen/compare/v2.0.5...HEAD
[2.0.5]: https://github.com/suban5/LivingDocGen/releases/tag/v2.0.5
[2.0.4]: https://github.com/suban5/LivingDocGen/releases/tag/v2.0.4
[2.0.3]: https://github.com/suban5/LivingDocGen/releases/tag/v2.0.3
[2.0.2]: https://github.com/suban5/LivingDocGen/releases/tag/v2.0.2
[2.0.1]: https://github.com/suban5/LivingDocGen/releases/tag/v2.0.1
[2.0.0]: https://github.com/suban5/LivingDocGen/releases/tag/v2.0.0
[1.0.4]: https://github.com/suban5/LivingDocGen/releases/tag/v1.0.4
[1.0.3]: https://github.com/suban5/LivingDocGen/releases/tag/v1.0.3
[1.0.2]: https://github.com/suban5/LivingDocGen/releases/tag/v1.0.2
[1.0.1]: https://github.com/suban5/LivingDocGen/releases/tag/v1.0.1
[1.0.0]: https://github.com/suban5/LivingDocGen/releases/tag/v1.0.0

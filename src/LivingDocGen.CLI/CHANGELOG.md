# Changelog - LivingDocGen.CLI

All notable changes to the LivingDocGen CLI tool will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [Unreleased]

### Added

### Changed

- Updated Gherkin library to 35.0.0 (inherited from Parser)
  - Aligned with Reqnroll 3.3.2 dependency for version consistency
- Improved test result parsing for Scenario Outlines (inherited from TestReporter)
  - Now recursively finds all nested test-case elements
  - Test statistics display correctly for parameterized scenarios

### Fixed

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

Install as a global .NET tool:

```bash
dotnet tool install --global LivingDocGen.CLI
```

Or as a local tool in your project:

```bash
dotnet new tool-manifest  # if you don't have one already
dotnet tool install LivingDocGen.CLI
```

### Basic Usage

Generate living documentation from feature files and test results:

```bash
# Basic command
livingdocgen --features "path/to/FeaturesFiles" --test-results "path/to/TestResultFile/results.xml" --output "./living-doc.html"

#without specifying exact testresult just test result path (useful when using multiple report)
livingdocgen --features "path/to/FeaturesFiles" --test-results "path/to/TestResultFile/" --output "./living-doc.html"

# With custom theme
livingdocgen --features "./Features" --test-results "./TestResults/results.xml" --output "./living-doc.html" --theme dark

# Using a configuration file
livingdocgen --config livingdocgen.json
```

### Configuration File (livingdocgen.json)

Create a `livingdocgen.json` file in your project root:

```json
{
  "featuresPath": "./Features",
  "testResultsPath": "./TestResults/results.xml",
  "outputPath": "./Documentation/living-doc.html",
  "title": "My Project Living Documentation",
  "theme": "blue",
  "includeComments": true
}
```

### Supported Test Result Formats

- NUnit 2 & 3 (XML)
- NUnit 4 (TRX)
- xUnit (XML)
- JUnit (XML)
- MSTest (TRX)
- SpecFlow JSON execution reports

### Available Themes

- `default` (Purple)
- `blue`
- `green`
- `dark`
- `light`
- `pickles`

### Command-Line Options

- `--features, -f` - Path to Gherkin feature files directory
- `--test-results, -t` - Path to test results file(s)
- `--output, -o` - Output HTML file path
- `--title` - Documentation title (default: "Living Documentation")
- `--theme` - Theme name (default, blue, green, dark, light, pickles)
- `--config, -c` - Path to configuration JSON file
- `--help, -h` - Show help information
- `--version, -v` - Show version information

For more information, visit: https://github.com/suban5/LivingDocGen

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
  - Sidebar navigation now works correctly with lazy rendering (50+ features)
  - Search functionality restored for dynamically loaded content
  - Tag filtering now properly displays filtered features
  - Background, Rule, and Examples sections toggle correctly
  - Rule and Background sections fully collapse without content overflow

- **UI/UX Improvements** (inherited from Generator):
  - Compact step display: keywords and text on same line (saves 40% vertical space)
  - Search result navigation with previous/next buttons and keyboard shortcuts
  - Simplified search scope (feature titles and scenario names only)
  - Fixed collapse behavior for all collapsible sections

### Details

- Fixed 12 critical bugs in Phase 2 lazy rendering implementation
- Added search navigation buttons with Enter/Shift+Enter keyboard shortcuts
- Removed inline onclick handlers (now uses event delegation)
- Step structure changed from separate divs to inline spans
- CSS fixes for rule-body and background-body collapse behavior

**Impact**: Reports with 50+ features now fully functional with all navigation, search, and filtering working correctly.

---

## [2.0.3] - 2026-01-22

### Changed

- Phase 2 performance optimizations for large reports (inherited from Generator)
  - Lazy rendering: Reports with 50+ features load progressively on scroll
  - Initial page load 87% faster for 200-feature reports (12s → 1.5s)
  - Time to interactive 86% faster (18s → 2.5s)
  - Smooth 60fps scrolling and instant toggle response (<16ms)
  - Memory usage reduced by 66% (350MB → 120MB)
  - Unified event delegation for all toggle operations
  - Optimized IntersectionObserver reduces overhead by 80-90%

---

## [2.0.2] - 2026-01-19

### Added

### Changed

- Improved HTML report performance for large feature sets (inherited from Generator)
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
  - `--include-comments` option (enabled by default)
  - Comments rendered with custom styling in HTML output

- **Enhanced HTML Report**: Improved visual layout and user experience
  - Better content visibility with optimized container height
  - CLI test automation script for comprehensive validation

### Changed

- **UI/UX Improvements**: Streamlined interface elements
  - Reduced stats and footer height for better space utilization
  - Features always expanded (non-collapsible) for improved accessibility
  - Simplified table headers (removed dimension labels)
  - Clickable table headers to toggle table body visibility
  - Toggle button now only affects scenarios

- **Statistics Display**: Enhanced accuracy and clarity
  - Pass/fail/skip percentages calculated based on executed scenarios only
  - Excludes untested scenarios from percentage calculations
  - More accurate representation of test results

### Fixed

- Statistics calculation logic for accurate pass/fail/skip rates
- Layout container height and spacing issues
- Table header interaction improvements

---

## [2.0.0] - 2026-01-01

### Added

- **CLI Tool:** Disabled Git commit hash in version output for cleaner display
  - Added `IncludeSourceRevisionInInformationalVersion=false`
  - Version now shows `2.0.0` instead of `2.0.0+<git-hash>`

### Changed

- **Breaking:** Migrated to .NET 6.0 minimum (from .NET 8.0)
  - CLI now compatible with .NET 6.0 runtime and higher
  - Better compatibility across different .NET versions

### Fixed

- Improved error reporting and messaging
- Enhanced theme switching performance

---

## [1.0.4] - 2025-01-01

### Changed

- Updated documentation with comprehensive API references
- Improved component README files with clearer examples

### Fixed

- Minor documentation inconsistencies
- Updated copyright year to 2026

---

## [1.0.3] - 2024-12-22

### Changed

- **Breaking:** Migrated to .NET 6.0 minimum (from .NET 8.0)
  - CLI executable now targets `net6.0`
  - Removed C# 10+ features for broader compatibility
  - Removed `ImplicitUsings` - all using directives are now explicit
  - Removed nullable reference type annotations

### Added

- Explicit using directives across all source files for clarity

### Compatibility

**CLI now requires:**
- ✅ .NET 6.0 runtime or higher
- ✅ Works on .NET 6, 7, 8 and future versions

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

First public release of LivingDocGen CLI - Universal BDD Living Documentation Generator.

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

#### Configuration
- JSON schema support for `livingdocgen.json`
- Customizable output paths and file names
- Theme selection and customization
- Include/exclude tag filtering
- Custom branding support

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

# LivingDocGen CLI

[![NuGet](https://img.shields.io/nuget/v/LivingDocGen.Tool.svg)](https://www.nuget.org/packages/LivingDocGen.Tool/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](../../LICENSE)

The **LivingDocGen CLI** is a cross-platform .NET Global Tool that generates beautiful, interactive living documentation from your Gherkin feature files and test results.

## ✨ What's New in v3.0.0 🎉

**Release Date:** March 5, 2026

### ⚠️ Breaking Changes

- **Default output mode changed to `chunked`** — The new multi-file scalable format is now the default. Legacy single-HTML mode remains available via `--output-mode legacy` but is deprecated.

### Chunked Output Architecture

- ✅ **Scalable Multi-File Reports** — Manifest, index, and per-feature JSON chunks
  - Reports with 3000+ features load in under 2 seconds
  - On-demand chunk loading with LRU cache and idle prefetch
  - Lightweight `index.html` shell with JSON artifacts alongside

- ✅ **Web Worker Search/Filter** — Off-main-thread query evaluation
  - Compact inverted index with galloping set-intersection
  - Three-tier fallback: Worker → local index → manifest-only filter
  - Delta DOM updates for sidebar filtering

- ✅ **Scenario and Table Virtualization** — Windowed rendering
  - Features with >200 scenarios render first 30; remaining on scroll
  - Tables with >200 rows load in 50-row chunks on demand

- ✅ **Runtime Contract Validation** — Integrity protection
  - Schema version guards, SHA-256 hash verification, buildId consistency
  - Fetch-with-retry with exponential backoff

### New CLI Options

- `--output-mode chunked|legacy` — Choose output format (default: `chunked`)
- Chunked output auto-created in `{outputBaseName}-chunked/` directory

**Impact:** Major architectural upgrade enabling scalable reports for very large BDD test suites (3000+ features).

See [CHANGELOG.md](CHANGELOG.md) for complete release notes.

## ✨ What's New in v2.0.7 🎉

**Release Date:** February 11, 2026

### UI Layout Enhancements

- ✅ **Full-Width Responsive Layout** - Maximum content visibility
  - Layout utilizes full available width (removed 1400px max-width)
  - Ultra-wide support for 2560px+ screens
  - Main content expands when sidebar is collapsed

- ✅ **Smart Header Auto-Hide** - Maximize BDD scenario visibility
  - Header automatically hides when scrolling down
  - Controls/filter bar remains fixed at top
  - Header reappears when scrolling up (150px+ threshold)

- ✅ **Enhanced BDD Scenario Styling** - Better visual prominence
  - Increased scenario card shadow and border thickness (4px left border)
  - Failed scenarios have red glow effect and pulsing status icon
  - Larger scenario titles (1.2rem) with gradient backgrounds

- ✅ **Minimized Footer** - Fixed bottom-right corner, near-invisible until hovered

### Navigation Improvements

- ✅ **Navigation Arrows for All Filters** - Works with status and tag filters
- ✅ **Nested Folder Tree** - Full hierarchy support with expand/collapse toggle
- ✅ **Fixed Search Navigation** - Scrolls directly to matching scenario

### Bug Fixes

- ✅ **Fixed Scroll Flickering** - CSS transition optimization for large documents
- ✅ **Fixed Sidebar Path Display** - Shows "Features" as root correctly

**Impact:** Significantly improved UI for better BDD scenario visibility and navigation experience.

See [CHANGELOG.md](CHANGELOG.md) for complete release notes.

## ✨ What's New in v2.0.6 🎉

**Release Date:** February 10, 2026

**Improvements:**
- ✅ **Gherkin 35.0.0 Library** - Updated Gherkin library for version consistency
  - Aligned with Reqnroll 3.3.2 dependency
  - Improved parser compatibility
- ✅ **Enhanced Scenario Outline Parsing** - Better test result parsing
  - Recursive test-case element finding (inherited from TestReporter)
  - Test statistics display correctly for parameterized scenarios

**Impact:** Improved compatibility with latest Reqnroll version and more accurate test results for Scenario Outlines.

See [CHANGELOG.md](CHANGELOG.md) for complete release notes.

## ✨ What's New in v2.0.5 🎉

**Release Date:** January 26, 2026

**Tag Filtering & UX Improvements:**
- ✅ **Tag Filtering** - Filter scenarios by tags with dropdown selector
  - Feature-level and scenario-level tag support
  - Case-insensitive tag matching
  - Integrated with unified filter system
- ✅ **Improved Controls Layout** - Better UX organization
  - New logical order: Status filters → Tag filter → Search → Clear All → Theme
  - Enhanced visual grouping and filtering workflow
- ✅ **Search Navigation** - Fixed prev/next button functionality
- ✅ **Accurate Counts** - Formula-based untested scenario calculation
- ✅ **Simplified Navigation** - Removed redundant sidebar search

**Impact:** Enhanced filtering capabilities and more intuitive controls layout for better user experience.

See [CHANGELOG.md](CHANGELOG.md) for complete release notes.

## 📦 Installation

### As a Global Tool
Install the tool globally to use it from any directory:

```bash
dotnet tool install --global LivingDocGen.Tool
```

### As a Local Tool
Install it locally in your project manifest:

```bash
dotnet new tool-manifest # if you haven't created one yet
dotnet tool install LivingDocGen.Tool
```

### Updating to Latest Version

To update an already installed global tool to the latest version:

```bash
dotnet tool update --global LivingDocGen.Tool
```

To update to a specific version:

```bash
dotnet tool update --global LivingDocGen.Tool --version 2.0.4
```

To check your current version:

```bash
LivingDocGen --version
```

## 🚀 Usage

The main command is `generate`, which produces the HTML documentation.

### Basic Generation
```bash
LivingDocGen generate ./Features ./TestResults
```

### Specify Output File
```bash
LivingDocGen generate ./Features ./TestResults -o ./docs/index.html
```

### Customize Title and Theme
```bash
LivingDocGen generate ./Features ./TestResults --title "My Project Docs" --theme dark
```

## 🛠 Commands

### `generate`
Generates the HTML living documentation.

**Arguments:**
- `features`: Path to feature files or directory (optional if using config file).
- `test-results`: Path to test results or directory (optional).

**Options:**
- `-o, --output <path>`: Output HTML file path (default: `living-documentation.html`).
- `-t, --title <text>`: Documentation title.
- `-th, --theme <name>`: Theme name (`purple`, `blue`, `green`, `dark`, `light`, `pickles`).
- `-c, --color <hex>`: Primary color (e.g., `#FF0000`).
- `--output-mode <mode>`: Output mode — `chunked` (default, scalable multi-file format) or `legacy` (single HTML, deprecated).
- `--config <path>`: Path to `livingdocgen.json` configuration file.
- `-v, --verbose`: Show detailed output.

### `parse`
Parses feature files and outputs the structure as JSON. Useful for debugging or custom integrations.

```bash
LivingDocGen parse ./Features -o features.json
```

**Options:**
- `-o, --output <path>`: Output JSON file path.
- `-f, --framework <name>`: Force specific framework parser (`cucumber`, `specflow`, `reqnroll`, `jbehave`).
- `-v, --verbose`: Show detailed output.

### `test-results`
Parses test result files (Trx, NUnit, xUnit, etc.) and outputs the standardized execution data as JSON.

```bash
LivingDocGen test-results ./TestResults -o results.json
```

**Options:**
- `-o, --output <path>`: Output JSON file path.
- `-v, --verbose`: Show detailed output.

## ⚙️ Configuration

You can use a `livingdocgen.json` file instead of passing CLI arguments. The tool automatically looks for this file in the current directory.

**Example `livingdocgen.json`:**
```json
{
  "enabled": true,
  "featuresPath": "./Features",
  "testResultsPath": "./TestResults",
  "output": "./docs/documentation.html",
  "title": "My Awesome Project",
  "theme": "dark",
  "primaryColor": "#3b82f6"
}
```

## 🏗 Development

To build and run the CLI locally from source:

```bash
# Clone the repository
git clone https://github.com/suban5/LivingDocGen.git
cd LivingDocGen

# Build the CLI project
dotnet build src/LivingDocGen.CLI/LivingDocGen.CLI.csproj

# Run locally (without installing as global tool)
dotnet run --project src/LivingDocGen.CLI/LivingDocGen.CLI.csproj -- generate ./samples/features ./samples/test-results
```

## ⚙️ Target Framework

- **.NET 6.0** - Requires .NET 6.0 runtime or higher
  - Works on Windows, macOS, and Linux
  - Cross-platform CLI tool

## 📚 More Information

For detailed documentation, see:
- [Main README](../../README.md)
- [Development Guide](../../docs/DEVELOPMENT.md)
- [API Reference](../../docs/API_REFERENCE.md)

```bash
# Navigate to the CLI project
cd src/LivingDocGen.CLI

# Run directly
dotnet run -- generate ../../samples/features ../../samples/test-results -o ../../docs/dev-output.html
```

## ✅ Verification

The LivingDocGen.Tool v2.0.0 has been tested and verified with:

**Test Result Format Support:**
- ✅ NUnit 2 XML format
- ✅ NUnit 3 XML format test results
- ✅ NUnit 4 / MSTest TRX format test results  
- ✅ xUnit XML format test results
- ✅ SpecFlow JSON execution reports

**CLI Features:**
- ✅ All three CLI commands (`generate`, `parse`, `test-results`)
- ✅ Verbose mode for detailed output (`-v` flag)
- ✅ Configuration file support (`livingdocgen.json`)

**Customization:**
- ✅ Multiple themes (purple, blue, green, dark, light, pickles)
- ✅ Theme customization (6 built-in themes)
- ✅ Custom color support via `--color` option

**Platform Support:**
- ✅ Cross-platform compatibility (Windows, macOS, Linux)
- ✅ .NET 6.0+ runtime support

## 📝 Todo List

Future enhancements planned:
- [ ] Add interactive mode for guided documentation generation
- [ ] Support for custom template engines (beyond embedded HTML)
- [ ] Add `--watch` mode to regenerate docs on file changes
- [ ] GitHub Actions integration examples
- [ ] Docker container support
- [ ] Plugin system for custom parsers and generators

# LivingDocGen Architecture

This document describes the architecture, design decisions, and internal structure of LivingDocGen.

---

## Table of Contents

- [Overview](#overview)
- [Architecture Diagram](#architecture-diagram)
- [Project Structure](#project-structure)
- [Core Components](#core-components)
- [Data Flow](#data-flow)
- [Design Decisions](#design-decisions)
- [Extension Points](#extension-points)

---

## Overview

LivingDocGen is a modular, framework-agnostic BDD living documentation generator. It follows a pipeline architecture where data flows through distinct phases: parsing, enrichment, and generation.

### Key Architectural Principles

1. **Separation of Concerns:** Each project has a single, well-defined responsibility
2. **Framework Agnostic:** Universal parsers work with any BDD framework
3. **Pluggable Design:** Easy to add new parsers, generators, or formatters
4. **Single Responsibility:** Each class focuses on one specific task
5. **Dependency Injection Ready:** Services designed for DI container usage

---

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                     LivingDocGen Ecosystem                      │
└─────────────────────────────────────────────────────────────────┘
                              │
         ┌────────────────────┼────────────────────┐
         │                    │                    │
         ▼                    ▼                    ▼
┌─────────────────┐  ┌─────────────────┐  ┌──────────────────┐
│ LivingDocGen.CLI│  │ LivingDocGen.   │  │ LivingDocGen.    │
│    (net6.0)     │  │    MSBuild      │  │   Reqnroll.      │
│                 │  │ (netstandard2.0)│  │  Integration     │
│ Global CLI Tool │  │ Build Task      │  │   (net6.0)       │
└────────┬────────┘  └────────┬────────┘  └────────┬─────────┘
         │                    │                     │
         └────────────────────┼─────────────────────┘
                              │
                    ┌─────────▼────────┐
                    │ LivingDocGen.    │
                    │   Generator      │
                    │ (netstandard2.1) │
                    │                  │
                    │ HTML Generator   │
                    └─────────┬────────┘
                              │
         ┌────────────────────┼────────────────────┐
         │                    │                    │
         ▼                    ▼                    ▼
┌─────────────────┐  ┌─────────────────┐  ┌──────────────────┐
│ LivingDocGen.   │  │ LivingDocGen.   │  │ LivingDocGen.    │
│    Parser       │  │  TestReporter   │  │     Core         │
│(netstandard2.0) │  │(netstandard2.0) │  │ (netstandard2.0) │
│                 │  │                 │  │                  │
│ Gherkin Parser  │  │ Test Result     │  │ Exceptions &     │
│                 │  │ Parsers         │  │ Validators       │
└─────────────────┘  └─────────────────┘  └──────────────────┘
```

---

## Project Structure

### 1. **LivingDocGen.Core** (netstandard2.0)

**Purpose:** Shared foundation for all other projects

**Components:**
- `Exceptions/` - Custom exception types
  - `BDDException` - Base exception for all BDD-related errors
  - `ConfigurationException` - Configuration loading/validation errors
  - `ParseException` - Parsing errors (feature files, test results)
  - `ValidationException` - Input validation failures
  
- `Validators/` - Input validation utilities
  - `FileValidator` - Validates file paths and accessibility

**Dependencies:** None (foundation layer)

**Design:** Minimal dependencies, maximum reusability

---

### 2. **LivingDocGen.Parser** (netstandard2.0)

**Purpose:** Universal Gherkin feature file parser

**Components:**
- `Core/`
  - `GherkinLexer` - Tokenizes feature file text
  - `GherkinTokenizer` - Converts tokens to structured data
  
- `Models/`
  - `UniversalModels.cs` - Universal BDD model (Feature, Scenario, Step, etc.)
  
- `Parsers/`
  - `GherkinParser` - Main parser orchestrator
  
- `Services/`
  - `FeatureFileService` - File system operations for feature files

**Key Design:**
- **Framework Agnostic:** Works with Reqnroll, SpecFlow, Cucumber, JBehave
- **Tag Support:** Full tag parsing with inheritance
- **Internationalization:** Supports Gherkin keywords in multiple languages
- **Examples/Outline Support:** Scenario Outline with Examples parsing
- **DocString & DataTable:** Full support for multi-line strings and tables

**Dependencies:** `LivingDocGen.Core`

---

### 3. **LivingDocGen.TestReporter** (netstandard2.0)

**Purpose:** Multi-format test result parser

**Components:**
- `Core/`
  - `TestResultDetector` - Auto-detects test result format
  
- `Models/`
  - `TestExecutionModels.cs` - Test execution results model
  
- `Parsers/`
  - `NUnit2ResultParser` - NUnit 2.x XML format
  - `NUnitResultParser` - NUnit 3.x XML format
  - `XUnitResultParser` - xUnit XML format
  - `JUnitResultParser` - JUnit XML format
  - `TrxResultParser` - MSTest/NUnit 4 TRX format
  - `SpecFlowJsonResultParser` - SpecFlow JSON execution reports
  
- `Services/`
  - `TestResultMerger` - Merges multiple test runs

**Key Design:**
- **Auto-Detection:** Automatically identifies test result format
- **Multi-Run Support:** Merges results from multiple test executions
- **Regression Testing:** Handles multiple runs of the same scenarios
- **Error Extraction:** Extracts error messages and stack traces
- **Statistics Calculation:** Computes pass rates, durations, totals

**Dependencies:** `LivingDocGen.Core`, `System.Text.Json`

---

### 4. **LivingDocGen.Generator** (netstandard2.1)

**Purpose:** HTML documentation generation with Razor templates

**Components:**
- `Models/`
  - `EnrichedModels.cs` - Feature/Scenario/Step with execution data
  - `ThemeConfig.cs` - Theme configuration and customization
  
- `Services/`
  - `DocumentEnrichmentService` - Merges features with test results
  - `HtmlGeneratorService` - Generates HTML using Razor templates
  - `ThemeService` - Manages themes and customization
  - `TemplateService` - Razor template compilation and rendering

**Key Design:**
- **Razor Templates:** Uses RazorEngine.NetCore for templating
- **Single File Output:** Embeds all CSS/JS for portability
- **6 Built-in Themes:** Purple, Blue, Green, Dark, Light, Pickles
- **Theme Switching:** Client-side theme switching with localStorage
- **Responsive:** Mobile-first responsive design
- **Interactive Filtering:** Filter by status, tags, features

**Dependencies:** `LivingDocGen.Core`, `LivingDocGen.Parser`, `LivingDocGen.TestReporter`, `RazorEngine.NetCore`

**Why netstandard2.1?** RazorEngine.NetCore requires netstandard2.1 minimum

---

### 5. **LivingDocGen.MSBuild** (netstandard2.0)

**Purpose:** MSBuild task integration for automatic generation

**Components:**
- `buildMultiTargeting/` - MSBuild targets and props files
- MSBuild task implementation

**Key Design:**
- **AfterTest Hook:** Automatically runs after `dotnet test`
- **Configuration Discovery:** Reads `bdd-livingdoc.json` if present
- **Fallback Defaults:** Works without configuration file
- **Multi-Targeting Support:** Works across all .NET project types

**Dependencies:** `LivingDocGen.Generator`, MSBuild SDK

---

### 6. **LivingDocGen.CLI** (net6.0)

**Purpose:** Global .NET tool for CLI usage

**Components:**
- `Program.cs` - CLI entry point and command handling
- `Models/BddConfiguration.cs` - Configuration model
- `Services/ConfigurationService.cs` - Config loading

**Commands:**
- `generate` - Generate documentation from features and test results
- `validate` - Validate feature files for syntax errors
- `version` - Display tool version

**Key Design:**
- **User-Friendly:** Clear help text and error messages
- **Flexible Input:** Accepts multiple feature and result paths
- **Configuration Support:** Reads `bdd-livingdoc.json`
- **Cross-Platform:** Works on Windows, macOS, Linux

**Dependencies:** `LivingDocGen.Generator`

---

### 7. **LivingDocGen.Reqnroll.Integration** (net6.0)

**Purpose:** Automatic generation via Reqnroll hooks

**Components:**
- `Hooks/` - Reqnroll `[AfterTestRun]` hook implementation

**Key Design:**
- **Zero Configuration:** Works automatically when package is installed
- **AfterTestRun Hook:** Generates docs after all tests complete
- **Reqnroll Aware:** Uses Reqnroll's test execution context

**Dependencies:** `LivingDocGen.Generator`, `Reqnroll`

---

## Core Components

### Universal Gherkin Parser

```
Input: .feature files (any framework)
  │
  ├──► GherkinLexer: Tokenize text
  │      │
  │      └──► Tokens: Feature, Scenario, Given, When, Then, And, But
  │
  ├──► GherkinTokenizer: Structure tokens
  │      │
  │      └──► AST: Feature → Scenarios → Steps
  │
  └──► GherkinParser: Build model
         │
         └──► UniversalFeature: Language-agnostic model
```

### Test Result Parser

```
Input: Test result files (NUnit/xUnit/JUnit/TRX/JSON)
  │
  ├──► TestResultDetector: Identify format
  │      │
  │      └──► Format: NUnit, xUnit, JUnit, TRX, SpecFlowJson
  │
  ├──► Specific Parser: Parse format
  │      │
  │      └──► TestExecutionReport: Standardized model
  │
  └──► TestResultMerger: Merge multiple runs
         │
         └──► Merged Report: Latest results for each scenario
```

### Document Enrichment

```
Input: Features + Test Results
  │
  ├──► Match Scenarios: Feature → Scenario → Test Result
  │      │
  │      └──► Matching Strategy: Name-based, tag-based
  │
  ├──► Enrich Model: Add execution data
  │      │
  │      └──► EnrichedFeature: Feature + Status + Duration + Errors
  │
  └──► Calculate Statistics: Pass rate, totals, durations
```

### HTML Generation

```
Input: Enriched Features
  │
  ├──► Select Theme: Load theme configuration
  │      │
  │      └──► Theme: CSS variables, colors, fonts
  │
  ├──► Render Template: Compile and execute Razor
  │      │
  │      └──► HTML: Structured markup
  │
  ├──► Embed Assets: Inline CSS and JavaScript
  │      │
  │      └──► Self-Contained: Single file with all resources
  │
  └──► Output: living-documentation.html
```

---

## Data Flow

### Complete Pipeline

```
┌───────────────┐
│ Feature Files │
│  (.feature)   │
└───────┬───────┘
        │
        ▼
┌───────────────┐      ┌──────────────────┐
│ Gherkin       │      │ Test Result Files│
│ Parser        │      │ (XML/JSON/TRX)   │
└───────┬───────┘      └────────┬─────────┘
        │                       │
        │                       ▼
        │              ┌──────────────────┐
        │              │ Test Result      │
        │              │ Parser           │
        │              └────────┬─────────┘
        │                       │
        └───────┬───────────────┘
                │
                ▼
        ┌───────────────┐
        │ Document      │
        │ Enrichment    │
        └───────┬───────┘
                │
                ▼
        ┌───────────────┐
        │ HTML          │
        │ Generator     │
        └───────┬───────┘
                │
                ▼
        ┌───────────────┐
        │ Output HTML   │
        └───────────────┘
```

---

## Design Decisions

### 1. **Why .NET Standard 2.0/2.1?**

**Decision:** Target .NET Standard instead of multi-targeting specific frameworks

**Rationale:**
- ✅ Maximum compatibility (supports .NET Framework 4.6.1+, .NET Core 2.0+, all modern .NET)
- ✅ Single compilation target reduces complexity
- ✅ Smaller NuGet packages
- ✅ Easier maintenance and testing

**Trade-off:** Can't use C# 10+ features (ImplicitUsings, nullable reference types)

**Compatibility Matrix:**

| Component | Target Framework | Compatible With |
|-----------|------------------|-----------------|
| Core, Parser, TestReporter, MSBuild | .NET Standard 2.0 | .NET Framework 4.6.1+, .NET Core 2.0+, .NET 5+ |
| Generator | .NET Standard 2.1 | .NET Core 3.0+, .NET 5+ |
| CLI, Reqnroll Integration | .NET 6.0 | .NET 6.0+ runtime |

**Verified Through Integration Tests:**
- ✅ .NET Framework 4.7.2
- ✅ .NET 6.0
- ✅ .NET 7.0
- ✅ .NET 8.0

This ensures the tool works with **virtually all BDD test projects** regardless of their target framework.

---

### 2. **Why Single-File HTML Output?**

**Decision:** Generate self-contained HTML with embedded CSS/JS

**Rationale:**
- ✅ Easy to share (email, Slack, Teams)
- ✅ No external dependencies
- ✅ Works offline
- ✅ Can be opened directly from file system
- ✅ Portable across environments

**Trade-off:** Larger file size (~200-500KB vs. ~10KB + assets)

---

### 3. **Why Razor Templates?**

**Decision:** Use RazorEngine.NetCore for HTML generation

**Rationale:**
- ✅ Familiar syntax for .NET developers
- ✅ Type-safe models
- ✅ Strong editor support (IntelliSense)
- ✅ Compile-time checking
- ✅ Easy to maintain and customize

**Alternatives Considered:**
- ❌ String concatenation: Unmaintainable, error-prone
- ❌ Handlebars: Less familiar to .NET developers
- ❌ Custom template engine: Reinventing the wheel

---

### 4. **Why Framework-Agnostic Parser?**

**Decision:** Build universal Gherkin parser instead of framework-specific parsers

**Rationale:**
- ✅ Works with any BDD framework (Reqnroll, SpecFlow, Cucumber, JBehave)
- ✅ No framework dependencies
- ✅ User can switch frameworks without changing tool
- ✅ Simpler codebase (one parser vs. many)

**Implementation:** Parse Gherkin syntax directly, ignore framework-specific metadata

---

### 5. **Why Auto-Detection of Test Results?**

**Decision:** Automatically detect test result format

**Rationale:**
- ✅ Zero configuration for users
- ✅ Works with mixed test frameworks
- ✅ Reduces user errors

**Implementation:** Inspect XML root element, JSON structure to identify format

---

### 6. **Why Client-Side Theme Switching?**

**Decision:** Implement theme switching in browser with JavaScript

**Rationale:**
- ✅ No regeneration needed to change themes
- ✅ Instant feedback
- ✅ Saved to localStorage for persistence
- ✅ Works offline

**Implementation:** CSS variables + JavaScript to swap themes dynamically

---

## Extension Points

### Adding a New Test Result Parser

1. Create new parser class in `LivingDocGen.TestReporter/Parsers/`
2. Implement parsing logic to convert to `TestExecutionReport`
3. Add detection logic to `TestResultDetector`
4. Register parser in service configuration

Example:
```csharp
public class CustomResultParser
{
    public TestExecutionReport Parse(string filePath)
    {
        // Parse your custom format
        // Return TestExecutionReport
    }
}
```

### Adding a New Theme

1. Add theme configuration to `ThemeConfig.cs`
2. Define CSS variables for colors, fonts, spacing
3. Add theme to theme selector in HTML template
4. Test responsive behavior

Example:
```csharp
public static ThemeConfig OrangeTheme => new ThemeConfig
{
    Name = "Orange",
    PrimaryColor = "#FF6B35",
    SecondaryColor = "#F7931E",
    // ...
};
```

### Adding a New Output Format

1. Create new generator service (e.g., `PdfGeneratorService`)
2. Implement generation logic using enriched models
3. Add CLI command or MSBuild option
4. Document usage

---

## Performance Considerations

### Parser Performance
- **Lazy Loading:** Feature files parsed on-demand
- **Streaming:** Large files processed in chunks
- **Caching:** Parsed features cached during single execution

### Generator Performance
- **Template Compilation:** Razor templates compiled once, reused
- **Minimal DOM:** Efficient HTML structure
- **CSS Variables:** Fast theme switching without recalculation

### Memory Management
- **IDisposable:** Proper cleanup of file handles and streams
- **StringBuilder:** Used for string concatenation
- **LINQ Efficiency:** Avoid multiple enumeration

---

## Testing Strategy

### Unit Tests
- Parser logic (GherkinParser, test result parsers)
- Model validation
- Service methods
- Located in: `tests/LivingDocGen.Parser.Tests/`

### Integration Tests
- **End-to-end pipeline testing across multiple .NET versions**
- **Verified compatibility with:**
  - ✅ `.NET Framework 4.7.2` (IntegrationTest.NetFramework472)
  - ✅ `.NET 6.0` (IntegrationTest.Net6)
  - ✅ `.NET 7.0` (IntegrationTest.Net7)
  - ✅ `.NET 8.0` (IntegrationTest.Net8)
- Multiple BDD framework combinations (Reqnroll, NUnit)
- Theme rendering and HTML generation
- MSBuild integration testing

### Sample Data
- Located in `samples/` directory
- Covers all Gherkin syntax features
- Multiple test result formats (NUnit 2, 3, 4, xUnit, TRX, SpecFlow JSON)

---

## Future Architecture Improvements

### Planned Enhancements
1. **Plugin System:** Allow third-party parsers and generators
2. **Parallel Processing:** Parse multiple files concurrently
3. **Incremental Generation:** Only regenerate changed features
4. **Custom Templates:** Allow user-provided Razor templates
5. **Historical Tracking:** Store and compare multiple test runs
6. **PDF Export:** Generate PDF documentation
7. **Markdown Export:** Generate Markdown documentation

---

**Last Updated:** January 1, 2026
**Version:** 1.0.4


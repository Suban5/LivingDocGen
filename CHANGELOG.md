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

## [3.0.0] - 2026-03-05

### Added

- **Generator**: Chunked output contract models for scalable report architecture (PR-1)
  - `FeatureManifest` — report-level metadata with chunk map, capability flags, and index hash
  - `FeatureIndex` — normalized search/filter index using integer ordinals and inverted token indexes
  - `FeatureChunk` — individual feature payload with scenario summary, content stats, and render hints
  - `WorkerMessage` — typed worker protocol with query cancellation, metrics, and sequence tracking
  - `ContractBase` — shared versioning fields (`schemaVersion`, `generatorVersion`, `compatibilityMinVersion`, `extensions`)
  - `ContractVersion` — schema version constants with compatibility validation
  - `ContractHashValidator` — SHA-256 hash computation, content integrity validation, and deterministic ID generation
  - `ContractValidator` — structural validation for manifest, index, and chunk contracts
  - `ContractSerializer` — JSON serialization with camelCase naming, hash computation, and round-trip support

- **Generator**: Chunked output pipeline with dual-mode generation (PR-2)
  - `TokenizerService` — NFC-normalized, case-folded token extraction with configurable delimiters
  - `IndexBuilderService` — builds `FeatureIndex` with inverted token, status, and tag indexes
  - `ManifestBuilderService` — constructs `FeatureManifest` mapping features to chunk files with deterministic IDs
  - `ChunkEmitterService` — renders per-feature HTML and computes content stats for virtualization hints
  - `ChunkedOutputPipeline` — orchestrates chunk → index → manifest → disk writes with cancellation support
  - Emits `feature-manifest.json`, `feature-index.json`, and `features/{featureId}.json` artifacts

- **Generator**: Runtime loader and chunk rendering for chunked output mode (PR-3)
  - `ChunkedRuntimeJavaScript` — generates runtime JavaScript for manifest-driven on-demand chunk loading
  - `LRUCache` — bounded in-memory cache with configurable capacity and DOM unmounting on eviction
  - Manifest loader — async fetch and parse of `feature-manifest.json` with error handling and degraded mode banner
  - Chunk fetcher — lazy-fetch of `features/{featureId}.json` with in-flight request deduplication
  - Chunk renderer — mounts fetched HTML into main content area, hides inactive features
  - Sidebar builder — constructs folder-tree sidebar navigation from manifest `featureMap` entries
  - Idle prefetch — prefetches adjacent features via `requestIdleCallback` for reduced click latency
  - Chunked search/filter — filters sidebar using manifest metadata (status, tags, name) without DOM traversal
  - Tag population from manifest for filter dropdown
  - Legacy function overrides (`selectFeature`, `applyAllFilters`) to intercept existing UI calls
  - `GenerateShellHtml` — lightweight HTML shell with empty sidebar/content placeholders for chunked mode
  - `ChunkedOutputPipeline` now emits `index.html` shell alongside JSON artifacts

- **Generator**: Web Worker search/filter with set-intersection filtering (PR-4)
  - `SearchWorkerJavaScript` — generates Web Worker JavaScript for off-main-thread query evaluation
    - Builds in-memory inverted index from `feature-index.json` using compact `Int32Array` postings
    - Set-intersection filtering: status + tags + text tokens combined via sorted integer intersection
    - Galloping (exponential) search for large size-ratio intersections
    - Stale query prevention via monotonic `querySeq` and `cancelledQueries` tracking
    - Protocol v1.1: `INIT_INDEX`, `READY`, `QUERY`, `QUERY_CANCEL`, `RESULT`, `ERROR`, `METRICS`
  - `SearchBridgeJavaScript` — main-thread bridge for worker lifecycle and delta DOM updates
    - Spawns Web Worker via Blob URL (inline source, no separate file hosting required)
    - Debounced query dispatch with cancellation of superseded queries
    - Delta DOM updates: tracks previous visible state, only toggles changed sidebar items
    - Three-tier fallback: Worker → synchronous local index → manifest-only feature-level filter
    - Overrides `applyChunkedFilters()` and `applyAllFilters()` for seamless integration
    - Exposes `window.getSearchMetrics()` for diagnostics
  - Search/filter logic removed from `ChunkedRuntimeJavaScript` (delegated to SearchBridge)
  - `HtmlGeneratorService` emits SearchBridge JS in shell HTML after ChunkedRuntime JS

- **CLI**: New `--output-mode` flag for selecting output format
  - Supports `legacy` (default, existing single-HTML behavior) and `chunked` (new scalable format)
  - Chunked output directory auto-created as `{outputBaseName}-chunked/`
  - No behavior change for existing users — legacy mode is the default

- **Generator**: Query correctness golden tests for search/filter validation (PR-4.5)
  - `QueryCorrectnessGoldenTests` — 81 deterministic tests with canonical 4-feature, 12-scenario dataset
  - Covers all query dimensions: status-only, tag-only, text-only, and all pairwise/triple combinations
  - Validates set-intersection query logic, feature ordinal derivation, and edge cases
  - Determinism and reproducibility verification across multiple index rebuilds
  - Exhaustive combinatorial coverage: 4 statuses × 5 tag sets = 20 combinations

- **Generator**: Virtualization for scenarios and large tables (PR-5)
  - Scenario list windowing: features with >200 scenarios render only the first 30 visible; remaining revealed on scroll via IntersectionObserver or "Show More" button
  - Data table row chunking: tables with >200 rows render initial window of 50 rows; remaining stored as JSON and appended in 50-row chunks on demand
  - Examples table row chunking: same windowing for Scenario Outline examples tables with status preservation
  - Preserves sticky table headers and horizontal scrolling UX on chunked tables
  - Uses `DocumentFragment` for batch DOM insertion to minimize reflows

- **Generator**: End-to-end integration tests for full chunked pipeline (PR-7)
  - `ChunkedPipelineEndToEndTests` — 39 tests exercising all real services (no mocks)
  - Wires up complete pipeline: FeatureRenderer → ChunkEmitterService → IndexBuilderService → ManifestBuilderService → HtmlGeneratorService → ChunkedOutputPipeline
  - Canonical dataset: 4 features, 12 scenarios with mixed statuses, tags, and text
  - File artifact validation: manifest, index, chunks, and shell HTML existence and structure
  - Manifest correctness: feature map entries, scenario ranges, capabilities, schema version, index hash
  - Index correctness: status distributions, inverted token index, tag index, hash integrity
  - Chunk correctness: scenario summaries, rendered HTML content, render hints
  - Cross-artifact consistency: BuildId, feature IDs, chunk hashes, and names match across all artifacts
  - Shell HTML validation: valid HTML document, chunked runtime JS, search bridge, worker inline
  - Determinism: separate pipeline instances produce identical index content and chunk HTML
  - Edge cases: empty documentation, single feature/scenario, cancellation, custom options
  - Scale test: 50 features × 10 scenarios (500 total) validates pipeline at scale

- **Generator**: Runtime compatibility layer with contract validation (PR-2.5)
  - `ContractLoaderJavaScript` — new JS module providing runtime contract validation for all chunked artifacts
  - Schema version guards mirroring C# `ContractVersion.IsCompatible()` (major-must-match, minor-gte-minimum)
  - BuildId consistency guards detecting partial deploys and stale CDN artifacts
  - SHA-256 hash integrity validation via Web Crypto API for chunks and feature index
  - Structured fetch-with-retry with configurable exponential backoff (default: 2 retries, 500ms base delay)
  - Error classification: `NETWORK_ERROR`, `VERSION_INCOMPATIBLE`, `HASH_MISMATCH`, `BUILD_MISMATCH`, `PARSE_ERROR`, `NOT_FOUND`
  - Runtime state machine (`LOADING` → `READY` | `DEGRADED`) with structured error log and timestamps
  - Warning banner for non-fatal issues (minor version mismatch, hash mismatch, buildId mismatch)
  - Diagnostics API (`window.getContractDiagnostics()`) exposing runtime state, version info, and error history
  - Version constants injected from C# `ContractVersion` at build time to guarantee parity

### Changed

- **Generator**: `ChunkedRuntimeJavaScript` now delegates manifest/chunk loading to `ContractLoader` (PR-2.5)
  - `loadManifest()` uses `ContractLoader.loadManifest()` for schema validation, retry, and buildId tracking
  - `fetchChunk()` uses `ContractLoader.loadChunk(entry)` for hash integrity and buildId consistency checks
- **Generator**: `SearchBridgeJavaScript.initSearchSystem()` delegates index loading to `ContractLoader` (PR-2.5)
  - Uses `ContractLoader.loadIndex(manifest)` for hash validation against `manifest.indexHash`
  - Retains raw-fetch fallback when ContractLoader is unavailable (non-chunked mode)
- **Generator**: `HtmlGeneratorService.GenerateShellHtml()` updated script loading order (PR-2.5)
  - ContractLoader JS loaded between shared JS and ChunkedRuntime JS
  - Chunked mode is now the default for all new report generation
  - Legacy mode remains available via `--output-mode legacy` but is deprecated
  - Deprecation warning displayed when legacy mode is used
  - `outputMode` setting added to config file (`advanced.outputMode`)

- **Generator**: `HtmlGenerationOptions.OutputMode` default changed from `Legacy` to `Chunked`
  - `OutputMode.Legacy` marked with `[Obsolete]` attribute
  - Consuming code using `Legacy` will see compiler deprecation warning

- **Generator**: Reduced search/filter overhead for large reports with heavy data tables
  - Precomputed scenario tag/search metadata to avoid DOM scans and table text reads
  - Chunked lazy rendering during search/filter to keep the UI responsive
  - Added `content-visibility` hints for scenario bodies and data tables

- **Reqnroll.Integration**: v3.0.0 release — inherits all Generator v3.0.0 changes
  - Chunked output architecture, Web Worker search, virtualization, runtime contract validation
  - Default output mode changed from `legacy` to `chunked` (breaking change)
  - Legacy mode still available via `--output-mode legacy`

### Fixed

- **Generator**: Feature descriptions now included in inverted token index for chunked search
  - Previously, only feature and scenario names were tokenized; descriptions were computed but not indexed

### Removed

---

## [2.0.7] - 2026-02-11

### Added

- **Generator**: Full-width responsive layout for maximum content visibility
  - Layout now utilizes full available width on larger screens (removed 1400px max-width)
  - Ultra-wide support for 2560px+ screens with optimized padding
  - Main content area expands when sidebar is collapsed for more reading space

- **Generator**: Smart header auto-hide behavior on scroll
  - Header automatically hides when scrolling down to maximize BDD scenario visibility
  - Controls/filter bar remains fixed at top for easy access when header is hidden
  - Header reappears when scrolling up significantly (150px+ scroll)
  - Accumulated scroll delta prevents flickering from small scroll variations

- **Generator**: Navigation arrows (▲▼) enabled for all filter types
  - Caret up/down buttons now work with status filters (Passed, Failed, Skipped, Untested)
  - Navigation also enabled for tag filtering - not just text search
  - Shows scenario count (e.g., "1 of 34") when any filter is active
  - Allows navigation through all visible scenarios matching current filters

- **Sidebar Navigation**: Nested folder tree structure with full hierarchy support
  - Displays complete folder hierarchy from Features folder (e.g., Performance/Banking/...)
  - Features and subfolders within same parent folder are both visible
  - Folder expansion up to depth 3 by default for better initial visibility
  - Root folder name extracted from base path (e.g., "Features" instead of "Root")
  - Smart toggle button to expand/collapse all folders with dynamic icon
    - Shows folder-open icon when expanded, folder-closed when collapsed
    - Tooltip updates based on current state
  - Visual depth indicators with indentation and connector lines
  - Folder count badges show total features including nested items

### Changed

- **Generator**: Enhanced BDD scenario styling for better prominence
  - Increased scenario card shadow and border thickness (4px left border)
  - Failed scenarios have subtle red glow effect and pulsing status icon
  - Larger scenario titles (1.2rem) with improved font weight
  - Scenario body has gradient background for visual separation
  - More pronounced hover effect (4px transform instead of 2px)

- **Generator**: Minimized footer to maximize content area
  - Footer now fixed in bottom-right corner (doesn't take layout space)
  - Very low opacity (0.3) until hovered
  - Smaller text and padding for minimal footprint

- **Generator**: Redesigned search UI layout for better usability
  - Changed from absolute positioning to flexbox layout to prevent element overlapping
  - Search elements now in proper order: [search input + ×] [count] [▲] [▼]
  - Reduced tag filter dropdown max-width from 180px to 140px for compact layout
  - Smaller search box (max-width 350px) for better space distribution

- **Generator**: Major refactoring of HtmlGeneratorService for improved maintainability
  - Reduced from 5,492 lines to 1,309 lines (76% reduction)
  - Extracted CSS generation to `CssGenerator.cs` (~2,300 lines) with interface `ICssGenerator`
  - Extracted JavaScript generation to `JavaScriptGenerator.cs` (~1,800 lines) with interface `IJavaScriptGenerator`
  - Added dependency injection support for extracted services
  - Removed legacy flat folder tree methods (`BuildFolderTree`, `GenerateFolderTree`)
  - Improved code organization following Single Responsibility Principle
  - No functional changes - all existing behavior preserved

- **Sidebar Navigation**: Filenames now displayed instead of feature names
  - Sidebar shows file names (e.g., "Test_login" from "Test_login.feature")
  - Feature names shown in tooltip and main content for context
  - Removes duplicate folder wrapper (no nested "Features" folder)
  - Total feature count displayed in sidebar header: "Features (116)"

### Fixed

- **Generator**: Fixed search navigation not scrolling to specific scenario
  - Caret up/down buttons now scroll directly to matching scenario instead of feature level only
  - Fixed incorrect CSS selector (.feature-card → .feature[data-feature-id]) that prevented navigation
  - Removed conflicting scroll-to-top behavior in updateSearchUI()

- **Generator**: Fixed scroll-induced flickering in HTML reports for large documents
  - Added CSS transition optimization with `scrolling` class to disable transitions during scroll
  - Batched DOM operations in `updateSidebarActive` using DocumentFragment
  - Unified scroll handlers with passive event listeners and `requestAnimationFrame`
  - Throttled IntersectionObserver callbacks to prevent excessive updates

- **Generator**: Fixed sidebar navigation not showing feature file names
  - Removed CSS `max-height` limits that clipped deeply nested folder content
  - Increased base max-height to accommodate large folder hierarchies

- **Generator**: Fixed main content showing "Loading..." indefinitely
  - `renderFeatureContent` now properly removes `lazy-feature` class and `data-lazy` attribute
  - Preserves `hidden` class state during lazy rendering

- **Generator**: Fixed JavaScript runtime errors in HTML reports
  - Resolved "Identifier 'sidebar' has already been declared" duplicate declaration
  - Fixed "selectFeature is not defined" by moving function declarations before onclick handlers
  - Moved `selectFeature`, `toggleFolder`, `handleFeatureKeydown`, `handleFolderKeydown` to script start

- **Generator**: Fixed sidebar showing full system path instead of relative path
  - `FindCommonBasePath` now preserves leading slash for Unix absolute paths
  - Sidebar correctly shows "Features" as root instead of `/Users/.../Features`

- **Reqnroll.Integration**: Fixed project root detection during test execution
  - Now uses assembly location instead of current directory
  - Correctly finds project root when `dotnet test` is run from solution directory

### Removed

---

## [2.0.6] - 2026-02-10

### ⚠️ BREAKING CHANGES

- **Reqnroll.Integration**: Now requires **.NET 8.0 or higher**
  - Dropped support for .NET 6.0 and .NET 7.0
  - Projects using .NET 6/7 must upgrade to .NET 8 before updating this package
  - Worker process and all dependencies target net8.0

### Added

- **Worker**: New standalone worker process for reliable documentation generation
  - Detached process runs independently of test host (resolves VSTest shutdown issues)
  - File-based job queue via `livingdoc-job-*.json` files
  - Intelligent test result waiting with stability detection
  - Supports configurable timeout (default 3 minutes)
  - Comprehensive console logging with timestamps

- **Multi-Format Test Results**: Configurable test result file patterns
  - New `testResults.format` shorthand: trx, nunit, xunit, junit, specflow, all
  - New `testResults.patterns` array for custom patterns: `["*.trx", "*.xml"]`
  - Auto-detection of test result format via parsers
  - Backward compatible with legacy `testResultFormat` config

- **NUnit XML Support via runsettings**
  - Example `test.runsettings` for NUnit XML output
  - `OutputXmlFolderMode` configuration for proper folder structure
  - Works without `--logger` command line flag

### Changed

- **Reqnroll.Integration**: Simplified to Worker-only mode
  - Removed InProcess mode (was unreliable with dotnet test)
  - Removed DeferredExternal mode (CLI dependency eliminated)
  - Single execution path via detached Worker process
  - Significantly reduced codebase complexity

- **Reqnroll.Integration**: Enhanced diagnostic logging in LivingDocBootstrap.cs
  - Comprehensive console output with full file paths and metadata
  - Project root, test results path, and test runner information
  - Configuration values display (feature path, output location, theme)
  - File metadata for test results (path, size in bytes/KB, modification timestamp)
  - HTML generation confirmation with output path and size
  - Structured error messages with error type, paths, and solutions
  - Debug log file (LIVINGDOC_DEBUG.txt) for detailed troubleshooting

- **Reqnroll.Integration**: Upgraded Reqnroll from 2.4.1 to 3.3.2
  - Requires Reqnroll 3.3.2 or higher for consuming projects
  - Compatible with Gherkin 35.0.0
  - Improved test result parsing for Scenario Outlines
  - Enhanced logging and error diagnostics

- **Parser**: Updated Gherkin library to 35.0.0 (aligned with Reqnroll 3.3.2)

### Removed

- **IntegrationTest.Net6**: Removed .NET 6 integration test project
- **IntegrationTest.Net7**: Removed .NET 7 integration test project
- **Reqnroll.Integration**: Removed InProcess execution mode files
  - Deleted `PostTestJobRunner.cs`
  - Deleted `TestResultAwaiter.cs`
  - Deleted `LivingDocExecutionMode.cs`
  - Deleted `LivingDocExecutionPolicy.cs`
  - Deleted `TestExecutionEnvironment.cs`

### Fixed

- **TestReporter**: Fixed NUnit test result parsing for nested test cases
  - Changed from `.Elements()` to `.Descendants()` to recursively find all test-case elements
  - Resolves missing test results for Scenario Outlines (parameterized tests)

---

## [2.0.5] - 2026-01-26

### Added

- **Generator**: Tag filtering functionality
  - Filter scenarios by tags with dropdown selector
  - Feature-level and scenario-level tag support
  - Case-insensitive tag matching
  - Integrated with unified filter system (works with status and search filters)
  - Lazy rendering support for tag filtering

### Changed

- **Generator**: Reorganized controls layout for better UX
  - New order: Status filters → Tag filter → Search input → Clear All → Theme
  - Improved visual grouping of related controls
  - Better logical flow for filtering workflow

### Fixed

- **Generator**: Tag filtering extraction and matching
  - Fixed tag selector to target `.tags` div in `.feature-body`
  - Removed Font Awesome icon interference when extracting tag text
  - Fixed tag matching between dropdown and scenario tags
  - Tag filtering now correctly displays matching scenarios

- **Generator**: Search navigation improvements
  - Added missing `updateSearchUI()` function
  - Fixed ReferenceError when using search prev/next buttons
  - Fixed button state management for search navigation

- **Generator**: Fixed untested scenario count calculation
  - Changed calculation from enumeration-based to formula-based: Total - (Passed + Failed + Skipped)
  - Now correctly shows all untested scenarios (e.g., 615 instead of 9 for 630 total with 15 executed)
  - More reliable and accurate untested count

### Removed

- **Generator**: Removed sidebar search feature
  - Simplified sidebar navigation by removing redundant search
  - Main search functionality still available in top controls

---

## [2.0.4] - 2026-01-22

### Fixed

- **Generator**: Critical bug fixes for Phase 2 lazy rendering implementation
  - Fixed sidebar navigation broken with lazy rendering (50+ features)
  - Fixed search functionality to work with dynamically rendered content
  - Fixed tag filtering not displaying features in main content
  - Fixed Background, Rule, and Examples section toggle functionality
  - Fixed Rule and Background sections showing content when collapsed
  - All fixes inherited by CLI and Reqnroll.Integration packages

- **Generator**: UI/UX improvements
  - Compact step display: Gherkin keywords and step text now on same line (40% vertical space reduction)
  - Added search result navigation with previous/next buttons
  - Search now includes keyboard shortcuts (Enter/Shift+Enter/Esc)
  - Simplified search scope to feature titles and scenario names only (faster, more reliable)
  - Fixed collapse CSS for rule-body and background-body (no content overflow)

- **CLI**: Inherits all Generator bug fixes and improvements (see above)

- **Reqnroll.Integration**: Inherits all Generator bug fixes and improvements (see above)

### Technical Details

- **12 Critical Bug Fixes**:
  1. Incorrect element ID reference (content → main-content)
  2. Missing on-demand rendering in selectFeature()
  3. Initial visibility state mismatch for lazy features
  4. Double-nesting bug in renderFeatureContent()
  5. Stale element reference after lazy rendering
  6. Search broken with lazy rendering
  7. Search result navigation missing
  8. Search scope too broad (causing collapse issues)
  9. Tag filter had no event listener
  10. Toggle functionality broken (inline onclick handlers)
  11. Step display too verbose (separate lines)
  12. Rule/Background collapse showing content

- **JavaScript Enhancements**:
  - Added searchResults array and currentSearchIndex tracking
  - New updateSearchUI() function for button states
  - New navigateSearchResults(direction) function
  - Enhanced performSearch() with lazy rendering support
  - Event delegation now handles all toggle operations

- **CSS Updates**:
  - Added .search-nav-btn styles for navigation buttons
  - Repositioned search UI elements for new buttons
  - Changed .step-keyword display: inline for compact layout
  - Fixed .rule-body and .background-body padding when collapsed

---

## [2.0.3] - 2026-01-22

### Changed

- **Generator**: Phase 2 performance optimizations for large reports (200+ features, 500+ scenarios)
  - Lazy content rendering: Features with 50+ files now render progressively on scroll
  - Progressive loading: Only first 10 features render immediately, remaining load as needed
  - Unified event delegation: Single click handler for all toggle operations (scenarios, backgrounds, rules, tables)
  - Optimized IntersectionObserver: Feature-level tracking instead of scenario-level for large reports
  - Performance improvements: Initial load 87% faster (12s → 1.5s), time to interactive 86% faster (18s → 2.5s)
  - Memory usage reduced by 66% (350MB → 120MB)
  - Toggle response time improved by 97% (200-500ms → <16ms)
  - Scroll performance: 15-30fps → 55-60fps

- **CLI**: Inherits Phase 2 performance optimizations from Generator (see above)

- **Reqnroll.Integration**: Inherits Phase 2 performance optimizations from Generator (see above)

- **MSBuild**: Inherits Phase 2 performance optimizations from Generator (see above)

### Technical Details

- Added `LazyRenderingThreshold = 50` constant for automatic activation
- New method: `GenerateFeatureDataJson()` for JSON embedding
- Completely rewrote JavaScript event delegation system
- Split IntersectionObserver into feature-level and scenario-level modes
- Added CSS for lazy loading placeholders
- Performance logging in browser console
- ~340 lines of code added/modified in HtmlGeneratorService

---

## [2.0.2] - 2026-01-19

### Changed

- **Generator**: Phase 1 performance optimizations for large reports (1000+ scenarios)
  - CSS containment for isolated rendering (features and scenarios)
  - GPU-accelerated animations using transform properties
  - Event delegation for scenario toggles (single listener vs thousands)
  - requestAnimationFrame for smooth 60fps batch updates
  - Smart debouncing with adaptive delays (300ms standard, 400ms for large reports)
  - Read/write batching to prevent layout thrashing
  - Optimized transitions using max-height instead of display:none
  - Performance improvements: 3-4x faster for 500-1000 scenarios, 5-10x faster for 1000-1800 scenarios

- **Reqnroll.Integration**: Performance optimizations for large test suites
  - Replaced Console.WriteLine with Trace.WriteLine for proper test output visibility
  - Reduced test result wait time from 3s to 1s
  - Updated documentation example to use [BeforeTestRun]/[AfterTestRun] hooks
  - Recommended bridge pattern now uses double-checked locking for minimal overhead

### Removed

- **Generator**: Expand All/Collapse All button (simplified UI, individual scenario toggles remain)

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

[Unreleased]: https://github.com/suban5/LivingDocGen/compare/v3.0.0...HEAD
[3.0.0]: https://github.com/suban5/LivingDocGen/releases/tag/v3.0.0
[2.0.7]: https://github.com/suban5/LivingDocGen/releases/tag/v2.0.7
[2.0.6]: https://github.com/suban5/LivingDocGen/releases/tag/v2.0.6
[2.0.5]: https://github.com/suban5/LivingDocGen/releases/tag/v2.0.5
[2.0.4]: https://github.com/suban5/LivingDocGen/releases/tag/v2.0.4
[2.0.3]: https://github.com/suban5/LivingDocGen/releases/tag/v2.0.3
[1.0.4]: https://github.com/suban5/LivingDocGen/releases/tag/v1.0.4
[1.0.3]: https://github.com/suban5/LivingDocGen/releases/tag/v1.0.3
[1.0.2]: https://github.com/suban5/LivingDocGen/releases/tag/v1.0.2
[1.0.1]: https://github.com/suban5/LivingDocGen/releases/tag/v1.0.1
[1.0.0]: https://github.com/suban5/LivingDocGen/releases/tag/v1.0.0


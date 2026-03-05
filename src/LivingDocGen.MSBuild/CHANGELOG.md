# Changelog - LivingDocGen.MSBuild

All notable changes to the LivingDocGen MSBuild integration will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- Chunked output contract models for scalable report architecture (inherited from Generator)
  - Manifest, index, and chunk data contracts with schema versioning and hash validation

- Chunked output pipeline with dual-mode generation (inherited from Generator)
  - Tokenizer, index builder, manifest builder, chunk emitter, and orchestrating pipeline
  - Emits scalable multi-file format with manifest, index, and per-feature chunk artifacts

- Runtime loader and chunk rendering for chunked output mode (inherited from Generator)
  - LRU cache with bounded capacity and DOM unmounting on eviction
  - Manifest-driven on-demand chunk fetching with in-flight deduplication
  - Sidebar built from manifest metadata instead of inline HTML
  - Lightweight shell HTML (`index.html`) emitted alongside JSON artifacts

- Web Worker search/filter with set-intersection filtering (inherited from Generator, PR-4)
  - Off-main-thread query evaluation via dedicated Web Worker
  - Compact inverted index with galloping intersection for large datasets
  - Three-tier fallback: Worker → synchronous local index → manifest-only filter
  - Delta DOM updates for efficient sidebar visibility toggling

- Virtualization for scenarios and large tables (inherited from Generator, PR-5)
  - Scenario list windowing: features with >200 scenarios render only the first 30; remaining revealed on scroll or click
  - Data table and examples table row chunking: tables with >200 rows render initial 50 rows; remaining loaded on demand
  - Sticky headers and horizontal scrolling preserved on chunked tables

- MSBuild task integration
- Automatic living documentation generation during build process
- Configuration through MSBuild properties
- Multi-targeting support for different .NET versions

### Changed

- Default output mode switched from `legacy` to `chunked` (inherited from Generator, PR-6)
  - Chunked mode is now the default; legacy mode is deprecated

- Reduced search/filter overhead for large reports with heavy data tables (inherited from Generator)
  - Precomputed scenario tag/search metadata to avoid DOM scans and table text reads
  - Chunked lazy rendering during search/filter to keep the UI responsive
  - Added `content-visibility` hints for scenario bodies and data tables

- Phase 2 performance optimizations for large reports (inherited from Generator)
  - Lazy rendering: Reports with 50+ features load progressively on scroll
  - Initial page load 87% faster (12s → 1.5s for 200 features)
  - Smooth 60fps scrolling, instant toggle response (<16ms)
  - Memory usage reduced by 66%

- Phase 1 performance optimizations for large feature sets (inherited from Generator)
  - 3-4x faster rendering for 500-1000 scenarios
  - 5-10x faster for 1000-1800 scenarios
  - Smooth animations with no UI freezing

### Fixed

- Feature descriptions now included in inverted token index for chunked search (inherited from Generator)

### Removed

- Expand All/Collapse All button from reports (inherited from Generator)

## [1.0.0] - 2026-01-15

### Added
- Initial release of LivingDocGen MSBuild integration
- MSBuild task for automatic documentation generation
- Support for configuration via MSBuild properties
- Multi-targeting support (.NET Framework, .NET Core, .NET 5+)
- Build-time documentation generation

[Unreleased]: https://github.com/yourusername/LivingDocGen/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/yourusername/LivingDocGen/releases/tag/v1.0.0

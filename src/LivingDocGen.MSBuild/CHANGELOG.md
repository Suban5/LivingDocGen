# Changelog - LivingDocGen.MSBuild

All notable changes to the LivingDocGen MSBuild integration will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- MSBuild task integration
- Automatic living documentation generation during build process
- Configuration through MSBuild properties
- Multi-targeting support for different .NET versions

### Changed

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

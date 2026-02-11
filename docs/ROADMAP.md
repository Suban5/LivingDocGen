# 🗺️ Project Roadmap

## Current Status

| Phase | Status | Description |
|-------|--------|-------------|
| Phase 1 | ✅ **Complete** | Universal parser, CLI tool, basic HTML generation |
| Phase 2 | ✅ **Complete** | Reqnroll integration, multi-framework support, themes |
| Phase 3 | 🚧 **In Progress** | MSBuild integration, advanced features |
| Phase 4 | 📋 **Planned** | AI/NLP analysis, user study |

### Recent Completions (v2.0.7)
- ✅ **Full-Width Responsive Layout** - Maximum content visibility
  - Layout now utilizes full available width on larger screens (removed 1400px max-width)
  - Ultra-wide support for 2560px+ screens with optimized padding
  - Main content area expands when sidebar is collapsed
- ✅ **Smart Header Auto-Hide** - Maximize BDD scenario visibility
  - Header automatically hides when scrolling down
  - Controls/filter bar remains fixed at top when header hidden
  - Header reappears when scrolling up significantly (150px+)
  - Accumulated scroll delta prevents flickering from small variations
- ✅ **Navigation Arrows for All Filters** - Improved search UX
  - Caret up/down buttons work with status filters (Passed, Failed, Skipped, Untested)
  - Navigation enabled for tag filtering - not just text search
  - Shows scenario count when any filter is active
- ✅ **Enhanced BDD Scenario Styling** - Better visual prominence
  - Increased scenario card shadow and border thickness (4px left border)
  - Failed scenarios have subtle red glow effect and pulsing status icon
  - Larger scenario titles (1.2rem) with improved font weight
- ✅ **Minimized Footer** - Maximum content area
  - Footer now fixed in bottom-right corner (doesn't take layout space)
  - Very low opacity (0.3) until hovered
- ✅ **Nested Folder Tree Navigation** - Full hierarchy support
  - Complete folder hierarchy from Features root
  - Smart expand/collapse toggle with dynamic icon
  - Visual depth indicators and folder count badges
- ✅ **Major HTML Generator Refactoring** - Improved maintainability
  - Reduced HtmlGeneratorService from 5,492 to 1,309 lines (76% reduction)
  - Extracted CSS to `CssGenerator.cs` (~2,300 lines)
  - Extracted JavaScript to `JavaScriptGenerator.cs` (~1,800 lines)
- ✅ **Fixed Scroll-Induced Flickering** - Large document optimization
  - CSS transition optimization with `scrolling` class
  - Batched DOM operations and unified scroll handlers
  - Throttled IntersectionObserver callbacks
- ✅ **Fixed Project Root Detection** - Reqnroll.Integration
  - Uses assembly location instead of current directory
  - Works correctly when `dotnet test` runs from solution directory

### Previous Completions (v2.0.6)
- ✅ **⚠️ BREAKING CHANGE: .NET 8.0+ Requirement for Reqnroll.Integration**
  - Dropped support for .NET 6.0 and .NET 7.0
  - Worker process and all dependencies target net8.0
  - Projects using .NET 6/7 must upgrade to .NET 8
- ✅ **Worker Process Architecture** - New standalone worker for reliable documentation generation
  - Detached process runs independently of test host
  - Resolves VSTest shutdown issue completely
  - File-based job queue via `livingdoc-job-*.json` files
  - Intelligent test result waiting with stability detection
  - Configurable timeout (default 3 minutes)
  - Comprehensive console logging with timestamps
- ✅ **Multi-Format Test Results** - Configurable test result file patterns
  - New `testResults.format` shorthand: trx, nunit, xunit, junit, specflow, all
  - New `testResults.patterns` array for custom patterns
  - Auto-detection of test result format via parsers
  - Backward compatible with legacy `testResultFormat` config
- ✅ **Reqnroll 3.3.2 Upgrade**
  - Compatible with Gherkin 35.0.0
  - Improved test result parsing for Scenario Outlines

### Previous Completions (v2.0.5)
- ✅ **Tag Filtering Functionality** - Complete tag-based scenario filtering
  - Filter scenarios by tags with dropdown selector
  - Feature-level and scenario-level tag support
  - Case-insensitive tag matching
  - Integrated with unified filter system
- ✅ **Improved Controls Layout** - Better UX organization
  - New logical order: Status filters → Tag filter → Search → Clear All → Theme
  - Improved visual grouping and filtering workflow
- ✅ **Tag Filtering Fixes** - Resolved extraction and matching issues
  - Fixed tag selector targeting
  - Removed Font Awesome icon interference
  - Corrected tag matching logic
- ✅ **Search Navigation Enhancements** - Better search UX
  - Fixed prev/next button functionality
  - Improved button state management
- ✅ **Untested Count Accuracy** - More reliable calculations
  - Formula-based calculation: Total - (Passed + Failed + Skipped)
  - Correctly displays all untested scenarios
- ✅ **Simplified Navigation** - Removed redundant sidebar search

### Previous Completions (v2.0.4)
- ✅ **Critical Bug Fixes for Lazy Rendering** - 12 issues resolved
  - Fixed sidebar navigation with lazy rendering (50+ features)
  - Restored search functionality with dynamically loaded content
  - Fixed tag filtering to display features properly
  - Fixed Background/Rule/Examples toggle functionality
  - Fixed content overflow in collapsed sections
  - Added search result navigation (prev/next buttons)
  - Implemented compact step display (40% space reduction)
  - Resolved element reference issues
  - Implemented event delegation for all toggles
  - Fixed double-nesting bug
  - Optimized search scope
  - Corrected CSS collapse behavior

### Previous Completions (v2.0.3)
- ✅ **Phase 2 Performance Optimizations** - Lazy rendering for large reports (200+ features)
- ✅ **Unified Event Delegation** - Single handler for all toggle operations
- ✅ **Progressive Loading** - Content loads on scroll, 87% faster initial load
- ✅ **Memory Optimization** - 66% reduction in browser memory usage
- ✅ **Optimized Observers** - Feature-level tracking for large reports

---

## 🚧 In Development

### MSBuild Integration
**Status:** In Development  
**Target Release:** v2.1.0

Automatic documentation generation after `dotnet test` without any manual steps:

```bash
dotnet add package LivingDocGen
dotnet test
# → Automatically creates living-documentation.html ✨
```

**Features:**
- Zero-configuration MSBuild targets
- Automatic triggering after test completion
- Configurable via `.csproj` properties
- Integration with existing build pipelines

---

## 📋 Planned Features

### Phase 3: Advanced Features (v2.x)

#### Enhanced Visualizations
- **Traceability Graphs** - Visual dependency mapping between scenarios
- **Trend Analysis** - Test execution trends over time
- **Code Coverage Integration** - Show which code is covered by BDD tests
- **Feature Dependency Diagrams** - Visualize feature relationships

#### Extended Framework Support
- **Playwright Test Results** - Support for Playwright test output
- **Cypress Test Results** - Integration with Cypress test runs
- **Jest/Mocha Test Results** - JavaScript testing framework support
- **Robot Framework** - Support for Robot Framework results

#### Quality Improvements
- **Performance Optimization** - Faster parsing and generation for large projects
- **Incremental Generation** - Only regenerate changed features
- ~~**Parallel Processing**~~ - ✅ **Complete in v2.0.1** - Multi-threaded parsing and generation
- **Memory Optimization** - Reduced memory footprint for large test suites

### Phase 4: AI/NLP Analysis (v3.x)

#### Scenario Quality Analysis
- **Quality Scoring** - AI-powered scenario quality assessment
- **Duplicate Detection** - Identify similar or redundant scenarios
- **Refactoring Suggestions** - AI-generated improvement recommendations
- **Best Practice Compliance** - Check scenarios against Gherkin best practices

#### Smart Features
- **Auto-Tagging** - AI-suggested tags based on scenario content
- **Scenario Grouping** - Intelligent feature organization suggestions
- **Step Pattern Recognition** - Identify common step patterns for reuse
- **Natural Language Improvements** - Suggestions for clearer scenario descriptions

#### Integration & Insights
- **Requirement Traceability** - Link scenarios to requirements (Jira, Azure DevOps)
- **Risk Assessment** - Identify high-risk areas with low test coverage
- **Test Gap Analysis** - Detect missing test scenarios
- **Predictive Failure Analysis** - ML-based failure prediction

---

## 🎯 Future Integrations

### IDE Extensions

#### Visual Studio Extension
- Test Explorer integration
- One-click documentation generation
- Live preview window
- Inline scenario validation

#### VS Code Extension
- Testing panel integration
- Preview documentation in editor
- Gherkin syntax validation
- Quick actions for common tasks

### Cloud Integrations

#### CI/CD Platform Integrations
- **GitHub Actions Marketplace** - Pre-built action for documentation generation
- **Azure DevOps Extension** - Pipeline task for living documentation
- **GitLab CI Component** - Reusable component for GitLab pipelines
- **Jenkins Plugin** - Native Jenkins integration

#### Cloud Storage
- **Azure Blob Storage** - Publish documentation to Azure
- **AWS S3** - Deploy to S3 buckets
- **Google Cloud Storage** - GCP integration
- **Confluence** - Auto-publish to Confluence pages

---

## 🔬 Research & Innovation

### Academic Contributions

This project is part of a Master's thesis focused on:

1. **Universal Parser Design** - Can we create a single parser for all BDD frameworks?
2. **User Experience** - Can we deliver better UX than existing tools (Pickles, Allure, SpecFlow+)?
3. **AI/NLP Integration** - Can AI improve BDD scenario quality through automated analysis?

### Innovation Goals

- ✅ **Framework-Agnostic Architecture** - Successfully demonstrated
- ✅ **Smart Merging Algorithm** - Timestamp-based test result consolidation working
- ✅ **Single-File DHTML** - Beautiful, self-contained reports achieved
- ✅ **Step-Level Failure Detection** - Precise error location identification complete
- 🔄 **AI Quality Analysis** - In progress (Phase 4)
- 📋 **NLP-Based Suggestions** - Planned (Phase 4)

---

## 📊 Metrics & Goals

### Performance Targets

| Metric | Current | Target (v3.0) |
|--------|---------|---------------|
| **Parse Speed** | 100 features/sec | 500 features/sec |
| **Memory Usage** | ~200MB (1000 scenarios) | ~100MB (1000 scenarios) |
| **Generation Time** | 2-5 seconds | <1 second |
| **File Size** | 500KB-2MB | 300KB-1MB (optimized) |

### Feature Coverage

| Feature | Status | Target Version |
|---------|--------|----------------|
| Gherkin Parsing | ✅ Complete | v1.0 |
| Test Result Integration | ✅ Complete | v1.0 |
| CLI Tool | ✅ Complete | v1.0 |
| Reqnroll Integration | ✅ Complete | v2.0 |
| MSBuild Integration | 🚧 In Progress | v2.1 |
| AI Quality Analysis | 📋 Planned | v3.0 |
| IDE Extensions | 📋 Planned | v3.5 |
| Cloud Integrations | 📋 Planned | v4.0 |

---

## 🤝 Community Feedback

We're actively seeking feedback on:

1. **Feature Priorities** - Which planned features are most important to you?
2. **Framework Support** - Which additional test frameworks should we support?
3. **Integration Needs** - What tools/platforms do you want to integrate with?
4. **UI/UX Improvements** - How can we make the generated documentation better?

**Share your thoughts:**
- [GitHub Discussions](https://github.com/Suban5/LivingDocGen/discussions)
- [Feature Requests](https://github.com/Suban5/LivingDocGen/issues/new?template=feature_request.md)
- [Community Survey](https://github.com/Suban5/LivingDocGen/discussions/categories/polls)

---

## 📅 Release Schedule

| Version | Target Date | Focus |
|---------|-------------|-------|
| v2.0.7 | ✅ Feb 11, 2026 | Full-width UI layout, auto-hide header, enhanced BDD styling |
| v2.0.6 | ✅ Feb 10, 2026 | Worker architecture, .NET 8+ requirement (breaking change) |
| v2.0.5 | ✅ Jan 26, 2026 | Tag filtering and UX improvements |
| v2.0.4 | ✅ Jan 22, 2026 | Critical lazy rendering bug fixes |
| v2.0.3 | ✅ Jan 22, 2026 | Phase 2 performance (lazy rendering) |
| v2.0.2 | ✅ Jan 19, 2026 | Phase 1 performance optimizations |
| v2.0.1 | ✅ Jan 16, 2026 | Gherkin comments, parallel processing |
| v2.0.0 | ✅ Jan 1, 2026 | Reqnroll integration, Bootstrap API |
| v2.1.0 | Feb 2026 | MSBuild integration |
| v2.2.0 | Mar 2026 | Additional performance improvements |
| v3.0.0 | Jun 2026 | AI/NLP quality analysis |
| v3.5.0 | Sep 2026 | IDE extensions |
| v4.0.0 | Dec 2026 | Cloud integrations |

*Note: Dates are estimates and may change based on development progress and community feedback.*

---

## 🎓 Academic Timeline

### Thesis Milestones

| Milestone | Status | Date |
|-----------|--------|------|
| Literature Review | ✅ Complete | Oct 2025 |
| Universal Parser Design | ✅ Complete | Nov 2025 |
| CLI & Core Implementation | ✅ Complete | Dec 2025 |
| Reqnroll Integration | ✅ Complete | Jan 2026 |
| MSBuild Integration | 🚧 In Progress | Feb 2026 |
| AI/NLP Module | 📋 Planned | Mar-May 2026 |
| User Study | 📋 Planned | Jun 2026 |
| Thesis Writing | 📋 Planned | Jul-Aug 2026 |
| Defense | 📋 Planned | Sep 2026 |

---

## 🔄 Version History

See [CHANGELOG.md](../CHANGELOG.md) for detailed release notes.

---

**Last Updated:** February 11, 2026

**Have suggestions for the roadmap?** [Open a discussion](https://github.com/Suban5/LivingDocGen/discussions)!

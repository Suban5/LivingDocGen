# LivingDocGen.Generator

**LivingDocGen.Generator** is the core rendering engine of the LivingDocGen solution. It is responsible for merging parsed feature files with test execution results and generating the final, interactive HTML documentation.

## 🎯 Purpose

This library takes the structured data from `LivingDocGen.Parser` (features) and `LivingDocGen.TestReporter` (results), enriches them into a unified model, and renders a self-contained HTML file. It focuses on performance, accessibility, and user experience.

## 🚀 Key Features

*   **Single-File Output**: Generates a standalone HTML file with embedded CSS and JavaScript. No external assets required.
*   **Smart Enrichment**: Automatically matches feature files to test results using intelligent fuzzy matching (handling naming conventions, spaces, and special characters).
*   **Performance Optimized**:
    *   Uses `StringBuilder` pre-allocation and batched operations.
    *   Implements thread-safe caching for HTML encoding and CSS generation (with auto-eviction after 20 themes).
    *   Parallel feature parsing with `Task.WhenAll` for improved throughput.
    *   Optimized for large reports (tested with 500+ features).
*   **Modern Architecture**:
    *   Dependency injection support with `ILogger<T>` integration.
    *   Full async/await pattern with `CancellationToken` support.
    *   Thread-safe operations with local dictionaries to prevent race conditions.
    *   Comprehensive input validation and error handling.
*   **Interactive UI**:
    *   Search and filtering (by tag, status, text).
    *   Master-detail layout with resizable sidebar.
    *   Dark/Light mode support.
*   **Theming System**: Built-in support for multiple color themes.

## 🏗 Architecture

### Services

1.  **`DocumentEnrichmentService`**:
    *   Merges `UniversalFeature` objects with `TestExecutionReport`.
    *   Uses thread-safe, method-local dictionaries for O(1) scenario matching.
    *   Handles complex matching logic (Scenario Outlines, parameterized tests).
    *   Implements intelligent fuzzy matching with configurable thresholds.
    *   Full logging support with `ILogger<T>` integration.

2.  **`HtmlGeneratorService`**:
    *   The main rendering engine with thread-safe operations.
    *   Generates semantic HTML5 markup with embedded CSS/JavaScript.
    *   Implements CSS theme caching with automatic eviction (max 20 themes).
    *   Uses pre-allocated `StringBuilder` for optimal performance.

3.  **`LivingDocumentationGenerator`**:
    *   Orchestrates the entire documentation generation pipeline.
    *   Parallel feature parsing for improved performance.
    *   Async/await with `CancellationToken` support for cancellable operations.
    *   Constructor-based dependency injection for all services.

### Models

*   **`LivingDocumentation`**: The root model containing all enriched features and statistics.
*   **`ThemeConfig`**: Defines the color palette and visual styles.

## 🎨 Themes

The generator supports several built-in themes defined in `ThemeConfig.cs`:

*   **Purple** (Default)
*   **Blue** (Ocean Blue)
*   **Green** (Forest)
*   **Dark** (Night Mode)
*   **Light** (Clean)
*   **Pickles** (Classic PicklesDoc style)

## 💻 Usage

### Basic Usage

```csharp
using LivingDocGen.Generator.Services;
using Microsoft.Extensions.Logging;

// 1. Initialize services with dependency injection
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());

var parserService = new UniversalParserService(
    loggerFactory.CreateLogger<UniversalParserService>());
var testReporter = new TestReportService(
    loggerFactory.CreateLogger<TestReportService>());
var enrichmentService = new DocumentEnrichmentService(
    loggerFactory.CreateLogger<DocumentEnrichmentService>());
var htmlGenerator = new HtmlGeneratorService(
    loggerFactory.CreateLogger<HtmlGeneratorService>());

var generator = new LivingDocumentationGenerator(
    parserService,
    testReporter,
    enrichmentService,
    htmlGenerator,
    loggerFactory.CreateLogger<LivingDocumentationGenerator>());

// 2. Generate documentation with cancellation support
var cancellationToken = new CancellationTokenSource(TimeSpan.FromMinutes(5)).Token;

var result = await generator.GenerateDocumentationAsync(
    new[] { "./Features" },
    new[] { "./TestResults/nunit-results.xml" },
    new HtmlGenerationOptions 
    {
        Title = "My Project Documentation",
        Theme = "dark"
    },
    cancellationToken);

// 3. Save to file
await File.WriteAllTextAsync("living-documentation.html", result, cancellationToken);
```

### Without Dependency Injection (Simple)

```csharp
using LivingDocGen.Generator.Services;

// 1. Initialize services (no logging)
var enrichmentService = new DocumentEnrichmentService();
var generatorService = new HtmlGeneratorService();

// 2. Enrich data (combine features + results)
var livingDoc = enrichmentService.EnrichDocumentation(parsedFeatures, testReport);

// 3. Generate HTML
var html = generatorService.GenerateHtml(livingDoc, new HtmlGenerationOptions 
{
    Title = "My Project Documentation",
    Theme = "dark"
});

// 4. Save to file
File.WriteAllText("living-documentation.html", html);
```

### Advanced: With Cancellation and Progress Tracking

```csharp
using var cts = new CancellationTokenSource();

// Cancel after timeout or on user request
cts.CancelAfter(TimeSpan.FromMinutes(10));

try
{
    var html = await generator.GenerateDocumentationAsync(
        featurePaths,
        testResultPaths,
        options,
        cts.Token);
    
    Console.WriteLine("✅ Documentation generated successfully!");
}
catch (OperationCanceledException)
{
    Console.WriteLine("⚠️ Generation was cancelled.");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error: {ex.Message}");
}
```

## 📝 Recent Improvements (v2.0.0)

### Performance & Scalability
- ✅ **Thread-Safe Operations**: All services use local dictionaries to prevent race conditions
- ✅ **CSS Cache Eviction**: Automatic cleanup after 20 cached themes to prevent memory leaks
- ✅ **Parallel Processing**: Feature files are parsed concurrently using `Task.WhenAll`
- ✅ **Optimized String Building**: Pre-allocated `StringBuilder` with capacity hints

### Architecture & Code Quality
- ✅ **Dependency Injection**: Full DI support with `ILogger<T>` integration
- ✅ **Async/Await Pattern**: All I/O operations are truly asynchronous
- ✅ **Cancellation Support**: `CancellationToken` support throughout the pipeline
- ✅ **Input Validation**: Comprehensive null checks and argument validation
- ✅ **Error Handling**: Structured logging replaces all `Console.WriteLine` calls

### Developer Experience
- ✅ **Comprehensive Logging**: Detailed diagnostic information at Debug/Info/Warning/Error levels
- ✅ **Better Error Messages**: Clear, actionable error messages with context
- ✅ **Configuration Constants**: Named constants for tunable thresholds (IndexBuildingThreshold, MinimumPartialMatchLength, MaxCssThemes)

## 📝 Todo List

- [ ] Add support for **PDF export** via headless browser rendering.
- [ ] Implement **custom theme builder** UI or configuration.
- [ ] Add **multi-language documentation** support (i18n).
- [ ] Support for **code snippet syntax highlighting** in step descriptions.

## ⚙️ Target Framework

- **.NET Standard 2.1** - Required by RazorEngine.NetCore dependency
  - Compatible with .NET Core 3.0+, .NET 5, 6, 7, 8+
  - Not compatible with .NET Framework (use .NET Core 3.0+ or .NET 5+)


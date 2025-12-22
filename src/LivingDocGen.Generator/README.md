# LivingDocGen.Generator

**LivingDocGen.Generator** is the core rendering engine of the LivingDocGen solution. It is responsible for merging parsed feature files with test execution results and generating the final, interactive HTML documentation.

## 🎯 Purpose

This library takes the structured data from `LivingDocGen.Parser` (features) and `LivingDocGen.TestReporter` (results), enriches them into a unified model, and renders a self-contained HTML file. It focuses on performance, accessibility, and user experience.

## 🚀 Key Features

*   **Single-File Output**: Generates a standalone HTML file with embedded CSS and JavaScript. No external assets required.
*   **Smart Enrichment**: Automatically matches feature files to test results using intelligent fuzzy matching (handling naming conventions, spaces, and special characters).
*   **Performance Optimized**:
    *   Uses `StringBuilder` pre-allocation and batched operations.
    *   Implements caching for HTML encoding and CSS generation.
    *   Optimized for large reports (tested with 500+ features).
*   **Interactive UI**:
    *   Search and filtering (by tag, status, text).
    *   Master-detail layout with resizable sidebar.
    *   Dark/Light mode support.
*   **Theming System**: Built-in support for multiple color themes.

## 🏗 Architecture

### Services

1.  **`DocumentEnrichmentService`**:
    *   Merges `UniversalFeature` objects with `TestExecutionReport`.
    *   Uses O(1) dictionary lookups for fast scenario matching.
    *   Handles complex matching logic (Scenario Outlines, parameterized tests).

2.  **`HtmlGeneratorService`**:
    *   The main rendering engine.
    *   Generates semantic HTML5 markup.
    *   Embeds interactive JavaScript for client-side filtering and navigation.

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

```csharp
using LivingDocGen.Generator.Services;

// 1. Initialize services
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

## 📝 Todo List

- [ ] Add support for **PDF export** via headless browser rendering.
- [ ] Implement **custom theme builder** UI or configuration.
- [ ] Add **multi-language documentation** support (i18n).
- [ ] Support for **code snippet syntax highlighting** in step descriptions.

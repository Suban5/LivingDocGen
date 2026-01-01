# LivingDocGen.Parser

**LivingDocGen.Parser** is the core parsing library responsible for reading Gherkin `.feature` files and converting them into a normalized, framework-agnostic object model.

## 🎯 Purpose

This library abstracts away the complexities of parsing Gherkin syntax. It uses the official [Gherkin](https://github.com/cucumber/gherkin) library under the hood but maps the AST (Abstract Syntax Tree) to a simplified, universal model that is easier to work with for documentation generation.

## 🚀 Key Features

*   **Universal Model**: Converts feature files into a standardized `UniversalFeature` object, regardless of the underlying BDD framework.
*   **Official Gherkin Support**: Uses the industry-standard `Gherkin` NuGet package for robust parsing of all Gherkin dialects and versions.
*   **Framework Agnostic**: Supports Cucumber, SpecFlow, and Reqnroll out of the box.
*   **Resilient Parsing**: Handles comments, tags, rules, backgrounds, and scenario outlines with data tables.

## 🏗 Architecture

### Core Components

1.  **`UniversalParserService`**: The main entry point. It orchestrates the parsing process and handles framework detection.
2.  **`IFeatureParser`**: The interface for framework-specific adapters.
3.  **`GherkinParser`**: The implementation that uses the official Gherkin library. Since SpecFlow, Reqnroll, and Cucumber all use standard Gherkin, this single parser handles them all.

### Universal Models

The library maps the complex Gherkin AST to these simplified models:

*   **`UniversalFeature`**: The root object representing a `.feature` file.
*   **`UniversalScenario`**: Represents both Scenarios and Scenario Outlines.
*   **`UniversalStep`**: Represents Given/When/Then steps.
*   **`UniversalExample`**: Represents data tables in Scenario Outlines.
*   **`UniversalRule`**: Supports the Gherkin `Rule` keyword.

## 💻 Usage

```csharp
using LivingDocGen.Parser.Services;
using LivingDocGen.Parser.Models;

// 1. Initialize the service
var parserService = new UniversalParserService();

// 2. Parse a single file
UniversalFeature feature = parserService.ParseFeature("path/to/test.feature");

Console.WriteLine($"Feature: {feature.Name}");
foreach (var scenario in feature.Scenarios)
{
    Console.WriteLine($"  Scenario: {scenario.Name}");
}

// 3. Parse an entire directory
List<UniversalFeature> allFeatures = parserService.ParseDirectory("path/to/features");
```

## ✅ Supported Frameworks

*   Cucumber
*   SpecFlow
*   Reqnroll

## 📝 Todo List

- [ ] Add support for **JBehave** (requires a custom parser as it uses a different syntax flavor).
- [ ] Add support for **Karate** feature files.
## ⚙️ Target Framework

- **.NET Standard 2.0** - Compatible with:
  - .NET Framework 4.6.1+
  - .NET Core 2.0+
  - .NET 5, 6, 7, 8+
  - Xamarin, Mono, Unity
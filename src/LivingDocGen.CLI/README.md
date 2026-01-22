# LivingDocGen CLI

[![NuGet](https://img.shields.io/nuget/v/LivingDocGen.Tool.svg)](https://www.nuget.org/packages/LivingDocGen.Tool/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](../../LICENSE)

The **LivingDocGen CLI** is a cross-platform .NET Global Tool that generates beautiful, interactive living documentation from your Gherkin feature files and test results.

## ✨ What's New in v2.0.3

**Phase 2 Performance Optimizations** - Handles large reports (200+ features) effortlessly:
- ⚡ **87% faster initial load** - 12s → 1.5s for 200-feature reports
- 🚀 **Lazy rendering** - Progressive content loading on scroll
- 💾 **66% less memory** - 350MB → 120MB browser usage
- 🎯 **Instant toggles** - <16ms response time (was 200-500ms)
- 📜 **Smooth scrolling** - Buttery 60fps performance

Automatically activates for reports with 50+ features. No configuration needed!

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

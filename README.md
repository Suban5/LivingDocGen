# 🎓 LivingDocGen – Universal BDD Living Documentation Generator

> Transform your BDD tests into beautiful, interactive living documentation automatically.

[![.NET](https://img.shields.io/badge/.NET-6.0%2B-512BD4)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-MIT-green)](LICENSE)
[![NuGet](https://img.shields.io/badge/NuGet-v2.0.5-blue)](https://www.nuget.org/packages/LivingDocGen.Tool/)

---

## 🎯 What is LivingDocGen?

A **framework-agnostic tool** that converts Gherkin feature files and test results into a single, interactive HTML report.

**Perfect for:**
- 📊 Visualizing test coverage and results
- 🔍 Identifying failures at step level  
- 📝 Sharing test documentation with stakeholders
- 🚀 Integrating into CI/CD pipelines

**Key Benefits:**
- ✅ **Single command** - No complex setup
- ✅ **Single HTML file** - No external dependencies
- ✅ **Multiple frameworks** - Reqnroll, SpecFlow, Cucumber, JBehave
- ✅ **Auto-generation** - Runs after every test

---

## 🚀 Quick Start

### 1. CLI Tool (Universal - Works Everywhere)

```bash
# Install global tool
dotnet tool install --global LivingDocGen.Tool

# Generate documentation
LivingDocGen generate ./Features ./TestResults -o living-doc.html

# Open in browser
open living-doc.html  # macOS
start living-doc.html # Windows
```

### 2. Reqnroll Integration (Auto-Generate via Hooks)

**Step 1:** Install package

```bash
dotnet add package LivingDocGen.Reqnroll.Integration
```

**Step 2:** Create bridge file `Hooks/LivingDocGenBridge.cs`

See the [complete bridge setup guide](docs/BRIDGE_SETUP.md) for the code template and configuration.

**Step 3:** Run tests

```bash
dotnet test --settings test.runsettings
```

Documentation is automatically generated as `living-documentation.html`!

**Why the bridge file?** Reqnroll only discovers hooks from your test assembly, not from NuGet packages. The bridge file calls the package's API from your test project. [Learn more →](docs/BRIDGE_SETUP.md)

---

## 📋 Requirements

### .NET Compatibility

| Component | .NET Versions |
|-----------|---------------|
| **CLI Tool** | .NET 6.0+ runtime |
| **Reqnroll Integration** | .NET 6.0+ runtime |
| **Library Packages** | .NET Framework 4.6.1+, .NET Core 2.0+, .NET 5+ |

### Supported Frameworks

**BDD Frameworks:**
- Reqnroll, SpecFlow, Cucumber, JBehave
- Any framework that produces Gherkin `.feature` files

**Test Result Formats:**
- NUnit 2, 3 (XML), NUnit 4 (XML and TRX)
- xUnit (XML)
- JUnit (XML)
- MSTest (TRX)
- SpecFlow/Cucumber (JSON)

---

## 🎨 Features

### Interactive HTML Reports
- **6 Beautiful Themes** - Purple (default), Blue, Green, Dark, Light, Pickles-style
- **Live Theme Switching** - Change themes in browser, saved to localStorage
- **Responsive Design** - Perfect on desktop, tablet, mobile
- **Interactive Filtering** - Filter by tags, status, features
- **Collapsible Sections** - Expand/collapse scenarios and features
- **Gherkin Comments** - Display comments from feature files in documentation

### Test Execution Details
- **Step-Level Results** - See exactly which step passed/failed
- **Execution Metrics** - Pass rate, duration, statistics
- **Error Details** - Stack traces, error messages, line numbers
- **Smart Merging** - Handles multiple test runs intelligently

### Multi-Framework Support
- **Framework-Agnostic** - Works with any Gherkin-based framework
- **Universal Parser** - Single parser for all frameworks
- **Zero Configuration** - Sensible defaults, customize if needed

### Performance
- **100 scenarios**: ~500ms generation time
- **500 scenarios**: ~2s generation time
- **1000 scenarios**: ~4s generation time
- **Single HTML file**: No external dependencies, fast loading
- **In-process**: No external process spawning overhead

---

## ⚙️ Configuration

### Default Behavior (Zero Configuration)

Works out-of-the-box with:
- **Features Path:** `./Features`
- **Test Results Path:** `./TestResults` (auto-detect latest)
- **Output File:** `./living-documentation.html`
- **Theme:** Purple
- **Title:** `{ProjectName} - Living Documentation`

### Custom Configuration

Create `livingdocgen.json` in your project root:

```json
{
  "featurePath": "Features",
  "testResultsPath": "TestResults",
  "outputPath": "docs/living-doc.html",
  "title": "My App - BDD Specs",
  "theme": "blue",
  "includeTestResults": true,
  "includeComments": true
}
```

**Available themes:** `purple`, `blue`, `green`, `dark`, `light`, `pickles`

**Configuration Options:**
- `includeComments` (default: `true`) - Display Gherkin comments (lines starting with `#`) in HTML output

---

## 🛠️ CLI Reference

### Generate Documentation

```bash
# Basic usage
LivingDocGen generate <features-path> <test-results-path>

# With options
LivingDocGen generate ./Features ./TestResults \
  -o docs.html \
  --theme blue \
  --title "E-Commerce Tests" \
  --verbose
```

### CLI Options

| Option | Alias | Description | Default |
|--------|-------|-------------|---------|
| `--output` | `-o` | Output file path | `living-documentation.html` |
| `--theme` | `-th` | Theme name | `purple` |
| `--title` | `-t` | Documentation title | Auto-detected |
| `--color` | `-c` | Primary color (hex) | `#6366f1` |
| `--verbose` | `-v` | Detailed logging | `false` |

---

## 💼 Usage Examples

### Example 1: CI/CD Pipeline (GitHub Actions)

```yaml
name: Tests and Documentation

on: [push, pull_request]

jobs:
  test-and-document:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'
      
      - name: Run tests
        run: dotnet test
      
      - name: Generate documentation
        run: |
          dotnet tool install --global LivingDocGen.Tool
          LivingDocGen generate ./Features ./TestResults -o docs.html
      
      - name: Publish documentation
        uses: actions/upload-artifact@v3
        with:
          name: living-documentation
          path: docs.html
```

### Example 2: Manual CLI Usage

```bash
# Install global tool
dotnet tool install --global LivingDocGen.Tool

# Generate with custom settings
LivingDocGen generate \
  ./Features \
  ./TestResults \
  --output ./docs/test-report-$(date +%Y%m%d).html \
  --theme dark \
  --title "Nightly Test Run - $(date +%Y-%m-%d)" \
  --verbose
```

### Example 3: Reqnroll Project

See the [Bridge Setup Guide](docs/BRIDGE_SETUP.md) for complete Reqnroll integration examples.

---

## 📦 Project Architecture

### Published NuGet Packages

- **[LivingDocGen.Tool](src/LivingDocGen.CLI/README.md)** - Universal CLI tool (Global .NET Tool)
- **[LivingDocGen.Reqnroll.Integration](src/LivingDocGen.Reqnroll.Integration/README.md)** - Reqnroll hooks integration
- **[LivingDocGen](src/LivingDocGen.MSBuild/README.md)** - MSBuild integration *(coming soon)*

### Internal Libraries

- **[LivingDocGen.Parser](src/LivingDocGen.Parser/README.md)** - Universal Gherkin parsing
- **[LivingDocGen.TestReporter](src/LivingDocGen.TestReporter/README.md)** - Test result parsing
- **[LivingDocGen.Generator](src/LivingDocGen.Generator/README.md)** - HTML rendering engine
- **[LivingDocGen.Core](src/LivingDocGen.Core/README.md)** - Shared infrastructure

### How It Works

```
┌─────────────────────────────────────────┐
│  1. Run Tests: dotnet test              │
└─────────────────────────────────────────┘
                  │
                  ▼
┌─────────────────────────────────────────┐
│  2. Parser Analyzes:                    │
│     • Feature files (Gherkin)           │
│     • Test results (NUnit/xUnit/etc)    │
└─────────────────────────────────────────┘
                  │
                  ▼
┌─────────────────────────────────────────┐
│  3. Generator Creates:                  │
│     • Single-file HTML                  │
│     • Interactive UI with themes        │
│     • Step-by-step results              │
└─────────────────────────────────────────┘
                  │
                  ▼
     living-documentation.html ✨
```

---

## 🔨 Building from Source

```bash
# Clone repository
git clone https://github.com/Suban5/LivingDocGen.git
cd LivingDocGen

# Build
dotnet build

# Run tests
dotnet test

# Run CLI locally
dotnet run --project src/LivingDocGen.CLI/LivingDocGen.CLI.csproj -- \
  generate ./samples/features ./samples/test-results
```

### Testing the CLI

A comprehensive test script is available to validate CLI functionality:

```bash
# Run CLI integration tests
cd tests
./test-cli.sh
```

The test script validates:
- ✅ Version and help commands
- ✅ HTML generation with various themes
- ✅ Custom titles and output paths
- ✅ Config file support
- ✅ Error handling for invalid inputs

All generated test outputs are saved to `tests/cli-test-output/` for manual inspection.

---

## ❓ FAQ

**Common questions:**
- Do I need to change my test code?
- How do I customize themes?
- Why does Reqnroll need a bridge file?
- What if test results aren't showing up?

See the [complete FAQ](docs/FAQ.md) for answers to these and many more questions.

---

## 🤝 Contributing

This is a **Master's thesis project**, but contributions are welcome!

- 🐛 [Report bugs](https://github.com/Suban5/LivingDocGen/issues)
- 💡 [Suggest features](https://github.com/Suban5/LivingDocGen/discussions)
- 🔧 [Submit pull requests](https://github.com/Suban5/LivingDocGen/pulls)

---

## 📜 Version History

**Latest Version: v2.0.5** (January 26, 2026)  
**Previous Version: v2.0.4** (January 22, 2026)

See [CHANGELOG.md](CHANGELOG.md) for detailed release notes.

---

## 📄 License

MIT License - see [LICENSE](LICENSE) file for details.

**Copyright © 2024-2026 Suban Dhyako**

---

## 📬 Contact & Support

- **GitHub:** [github.com/suban5/LivingDocGen](https://github.com/suban5/LivingDocGen)
- **Issues:** [Report a bug or request a feature](https://github.com/suban5/LivingDocGen/issues)
- **Discussions:** [Ask questions or share ideas](https://github.com/suban5/LivingDocGen/discussions)

---

<div align="center">

### ⭐ If you find this project useful, please give it a star! ⭐

Made with ❤️ for the BDD community

**[Documentation](docs/) • [FAQ](docs/FAQ.md) • [Bridge Setup](docs/BRIDGE_SETUP.md) • [Changelog](CHANGELOG.md)**

</div>

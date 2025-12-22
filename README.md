# 🎓 LivingDocGen – Universal BDD Living Documentation Generator

> Automatically generate beautiful, interactive living documentation from your BDD tests.

---

## 📘 Introduction

LivingDocGen is a **framework‑agnostic, zero‑configuration** tool that turns your Gherkin feature files and test results into a single, interactive HTML report. 

It is designed for both **QA engineers** and **developers** who want:

- A clear view of which features and scenarios exist
- Fast insight into which tests pass or fail
- Step‑level error details without digging through XML or logs

Every time you run your tests, LivingDocGen can produce up‑to‑date “living documentation” that reflects the current behavior of your system.

---

## ✨ What Problem Does This Solve?

**Before:**
- ❌ Run tests manually: `dotnet test`
- ❌ Find test results buried in XML files
- ❌ Run complex CLI commands to generate docs
- ❌ Repeat this every time tests change

**After:**
- ✅ Run tests: `dotnet test`
- ✅ Documentation auto-generates: `living-documentation.html` ✨
- ✅ Open in browser and see beautiful, interactive reports
- ✅ Zero manual steps!

---

## 🧩 Installation

### Option 1: Use the NuGet Package (Reqnroll/SpecFlow)

Add the package to your **test project**:

```bash
cd YourTestProject
dotnet add package LivingDocGen
```

Then simply run your tests:

```bash
dotnet test
```

The file `living-documentation.html` is generated automatically (see your project output or configured path) and can be opened in any modern browser.

### Option 2: Install the Global CLI Tool

Install the CLI as a .NET global tool (package name from `LivingDocGen.CLI`):

```bash
dotnet tool install --global LivingDocGen.Tool
```

You can then run the `LivingDocGen` command from any test project directory.

---

## 🚀 Quick Start (Usage Overview)

### Automatic Integration (Reqnroll/SpecFlow)

1. **Install the package** in your test project (see above).
2. **Run your tests** with `dotnet test`.
3. **Open** the generated `living-documentation.html` in a browser:

  ```bash
  # macOS
  open living-documentation.html

  # Windows
  start living-documentation.html
  ```

### Global CLI Tool (Any BDD Project)

From the root of a test project that has `Features/` and `TestResults/` folders:

```bash
LivingDocGen generate ./Features ./TestResults -o docs.html
```

This command:

- Reads all `.feature` files under `./Features`
- Reads supported test result files under `./TestResults`
- Produces a single `docs.html` living documentation file

---

## 🎯 Key Features

### 🔄 Automatic Generation
- **MSBuild Integration**: Hooks into your test execution pipeline
- **Zero Configuration**: Works out-of-the-box with sensible defaults
- **Smart Detection**: Automatically finds your features and test results

### 🎨 Beautiful Interactive Reports
- **6 Stunning Themes**: Purple (default), Blue, Green, Dark, Light, Pickles-style
- **Live Theme Switching**: Change themes in the browser, saved to localStorage
- **Responsive Design**: Perfect on desktop, tablet, and mobile
- **Interactive Filtering**: Filter by tags, status, features
- **Collapsible Sections**: Expand/collapse scenarios and features

### 📊 Rich Test Execution Details
- **Step-Level Results**: See exactly which step passed/failed
- **Execution Metrics**: Pass rate, duration, statistics
- **Error Details**: Stack traces, error messages, line numbers
- **Smart Merging**: Handles regression testing with multiple test runs

### 🔧 Multi-Framework Support

**BDD Frameworks:**
- ✅ Reqnroll (.NET)
- ✅ SpecFlow (.NET)
- ✅ Cucumber (Java/Ruby/JS)
- ✅ JBehave (Java)

**Test Result Formats:**
- ✅ **NUnit** (XML format - NUnit 2 & 3)
- ✅ **NUnit 4** (TRX format - Microsoft Test Results)
- ✅ **xUnit** (XML format)
- ✅ **JUnit** (XML format)
- ✅ **MSTest** (TRX format)
- ✅ **SpecFlow** (JSON execution report)

---

## 🏗️ How It Works

```
┌─────────────────────────────────────────────────────────┐
│  1. You Run Tests: dotnet test                          │
└─────────────────────────────────────────────────────────┘
                        │
                        ▼
┌─────────────────────────────────────────────────────────┐
│  2. MSBuild Target Triggers After Tests Complete        │
└─────────────────────────────────────────────────────────┘
                        │
                        ▼
┌─────────────────────────────────────────────────────────┐
│  3. Parser Analyzes:                                     │
│     • Feature files (Gherkin syntax)                     │
│     • Test results (NUnit/xUnit/JUnit/TRX/JSON)         │
└─────────────────────────────────────────────────────────┘
                        │
                        ▼
┌─────────────────────────────────────────────────────────┐
│  4. Generator Creates:                                   │
│     • Single-file HTML with embedded CSS/JS              │
│     • Interactive UI with filtering and themes           │
│     • Step-by-step execution results                     │
└─────────────────────────────────────────────────────────┘
                        │
                        ▼
┌─────────────────────────────────────────────────────────┐
│  5. Output: living-documentation.html ✨                 │
└─────────────────────────────────────────────────────────┘
```

---



## ⚙️ Configuration

### Zero Configuration (Default Behavior)

Works out-of-the-box with these defaults:

- **Features Path**: `./Features`
- **Test Results Path**: `./TestResults`
- **Output File**: `./living-documentation.html`
- **Title**: Your project name
- **Theme**: Purple
- **Auto-Generate**: After tests complete

### Custom Configuration (Optional)

#### Option 1: Configuration File (Recommended)

Create `livingdocgen.json` in your project root:

```json
{
  "enabled": true,
  "autoGenerate": "AfterTest",
  "paths": {
    "features": "./Features",
    "testResults": "./TestResults",
    "output": "./docs/living-documentation.html"
  },
  "documentation": {
    "title": "My E-Commerce Platform - Test Documentation",
    "theme": "blue",
    "primaryColor": "#3b82f6"
  },
  "advanced": {
    "verbose": false,
    "includeSkipped": true,
    "includePending": true
  }
}
```

#### Option 2: MSBuild Properties (in .csproj)

```xml
<PropertyGroup>
  <!-- Enable/Disable generation (default: true) -->
  <LivingDocEnabled>true</LivingDocEnabled>
  
  <!-- Custom output path -->
  <LivingDocOutput>$(OutputPath)docs/index.html</LivingDocOutput>
  
  <!-- Documentation Title -->
  <LivingDocTitle>My Project - Living Documentation</LivingDocTitle>
  
  <!-- Theme (purple, blue, green, dark, light) -->
  <LivingDocTheme>blue</LivingDocTheme>
</PropertyGroup>
```

### Configuration Priority (Highest to Lowest)

1. **MSBuild Properties** (in `.csproj`)
2. **Configuration File** (`livingdocgen.json`)
3. **Default Values**

### Available Themes

| Theme | Primary Color | Best For |
|-------|---------------|----------|
| `purple` | Indigo/Purple | Default, modern look |
| `blue` | Sky Blue | Corporate, professional |
| `green` | Emerald Green | Fresh, testing focus |
| `dark` | Purple on dark | Night coding, eye comfort |
| `light` | Royal Blue | High contrast, minimal |
| `pickles` | Amber/Yellow | Matching Pickles style |

**💡 Tip**: Theme can be changed in the browser after generation!

---

## 🛠️ CLI Commands Reference

### Generate Documentation

```bash
# Basic usage
LivingDocGen generate <features-path> <test-results-path>

# With output file
LivingDocGen generate ./Features ./TestResults -o documentation.html

# With custom theme and title
LivingDocGen generate ./Features ./TestResults \
  --theme blue \
  --title "E-Commerce Platform Tests" \
  -o docs.html

# Verbose mode (detailed logging)
LivingDocGen generate ./Features ./TestResults --verbose
```

### Parse Features Only

```bash
# Parse and output JSON
LivingDocGen parse ./Features -o features.json

# Parse with specific framework
LivingDocGen parse ./Features --framework reqnroll -o features.json

# Verbose output
LivingDocGen parse ./Features -v
```

### Parse Test Results Only

```bash
# Parse test results
LivingDocGen test-results ./TestResults -o results.json

# Parse specific file
LivingDocGen test-results ./TestResults/nunit-results.xml -v
```

### CLI Options

| Option | Alias | Description | Default |
|--------|-------|-------------|---------|
| `--output` | `-o` | Output file path | `living-documentation.html` |
| `--theme` | `-th` | Theme name | `purple` |
| `--title` | `-t` | Documentation title | Auto-detected |
| `--color` | `-c` | Primary color (hex) | `#6366f1` |
| `--verbose` | `-v` | Detailed logging | `false` |
| `--framework` | `-f` | BDD framework | Auto-detected |

---

## 🎨 Theme System

### Available Themes & Colors

<table>
<tr>
<th>Theme</th>
<th>Preview</th>
<th>Use Case</th>
</tr>
<tr>
<td><code>purple</code></td>
<td>🟣 Indigo gradient</td>
<td>Modern, professional (default)</td>
</tr>
<tr>
<td><code>blue</code></td>
<td>🔵 Sky blue gradient</td>
<td>Corporate, trustworthy</td>
</tr>
<tr>
<td><code>green</code></td>
<td>🟢 Emerald gradient</td>
<td>Fresh, testing focus</td>
</tr>
<tr>
<td><code>dark</code></td>
<td>⚫ Dark mode</td>
<td>Night coding, low light</td>
</tr>
<tr>
<td><code>light</code></td>
<td>⚪ Clean light</td>
<td>High contrast, minimal</td>
</tr>
<tr>
<td><code>pickles</code></td>
<td>🟡 Amber/Yellow</td>
<td>Familiar Pickles style</td>
</tr>
</table>

### Changing Themes

**During Generation:**
```bash
LivingDocGen generate ./Features ./TestResults --theme dark
```

**In the Browser:**
1. Open generated HTML report
2. Find theme selector dropdown at the top
3. Select your preferred theme
4. Theme is saved in browser localStorage!



---

## 🔧 Advanced Features

### Smart Test Result Merging

**Scenario**: You run tests, some fail, you fix bugs, and rerun only failed tests.

**Problem**: You have 2 result files from the same test suite.

**Solution**: The tool automatically merges them, showing the **most recent result** for each test!

```bash
# Initial run (11:00 AM - some tests failed)
dotnet test --logger "nunit;LogFilePath=results/initial-run.xml"

# Fix bugs...

# Rerun failed tests (2:30 PM - now passing)
dotnet test --filter "TestCategory=Failed" \
  --logger "nunit;LogFilePath=results/rerun.xml"

# Generate documentation
LivingDocGen generate ./Features ./results -o docs.html

# Result: Shows latest status (2:30 PM results) for each test! ✨
```



### Step-Level Failure Detection

The tool shows **exactly which step failed** in a scenario:

- ✅ **Step keyword**: Given/When/Then/And/But
- ✅ **Step text**: Full step description
- ✅ **Line number**: Where in the feature file
- ✅ **Error message**: What went wrong
- ✅ **Stack trace**: Full debugging info

### Scenario Outlines with Data Tables

Fully supports:
- Multiple `Examples:` sections
- Large data tables (10+ columns/rows)
- Proper rendering of all example combinations

### Tags & Filtering

Generated documentation includes:
- Tag filtering (`@smoke`, `@critical`, `@regression`, etc.)
- Status filtering (Passed/Failed/Skipped)
- Feature filtering
- Search functionality

---

## 💼 Real-World Usage Examples

### Example 1: Reqnroll Test Project

**Project structure:**
```
MyReqnrollTests/
├── Features/
│   ├── Login.feature
│   ├── ShoppingCart.feature
│   └── Checkout.feature
├── StepDefinitions/
├── MyReqnrollTests.csproj
```

**Setup (.csproj):**
```xml
<ItemGroup>
  <PackageReference Include="Reqnroll" Version="2.0.0" />
  <PackageReference Include="Reqnroll.NUnit" Version="2.0.0" />
  <PackageReference Include="LivingDocGen" Version="1.0.0" />
</ItemGroup>

<PropertyGroup>
  <BDDLivingDocTheme>blue</BDDLivingDocTheme>
  <BDDLivingDocTitle>E-Commerce Test Suite</BDDLivingDocTitle>
</PropertyGroup>
```

**Usage:**
```bash
dotnet test
# → Automatically creates living-documentation.html
```

### Example 2: CI/CD Pipeline (GitHub Actions)

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

### Example 3: Manual CLI Usage

```bash
# Install global tool
dotnet tool install --global LivingDocGen.Tool

# Navigate to project
cd /path/to/your/test/project

# Generate with custom settings
LivingDocGen generate \
  ./Features \
  ./TestResults \
  --output ./docs/test-report-$(date +%Y%m%d).html \
  --theme dark \
  --title "Nightly Test Run - $(date +%Y-%m-%d)" \
  --verbose

# Open in browser
open ./docs/test-report-*.html
```

---

## 📦 Project Architecture

### Project Structure

```
LivingDocGen/
├── src/
│   ├── LivingDocGen.CLI/                 # Command-line interface & global tool
│   ├── LivingDocGen.Parser/              # Universal Gherkin parser
│   ├── LivingDocGen.TestReporter/        # Test results parser
│   ├── LivingDocGen.Generator/           # HTML documentation generator
│   ├── LivingDocGen.MSBuild/             # MSBuild integration targets
│   ├── LivingDocGen.Reqnroll.Integration/# Reqnroll hooks integration
│   └── LivingDocGen.Core/                # Shared models & utilities
├── tests/
│   └── LivingDocGen.Parser.Tests/        # Unit tests
├── samples/
│   ├── features/                   # Sample Gherkin files
│   └── test-results/               # Sample test result files
├── docs/                            # Documentation
└── examples/                        # Integration examples
```

### Component Overview

For detailed documentation on each component, please refer to their respective README files:

*   **[LivingDocGen.CLI](src/LivingDocGen.CLI/README.md)**: Command-line interface and global tool usage.
*   **[LivingDocGen.Core](src/LivingDocGen.Core/README.md)**: Shared infrastructure, exceptions, and validators.
*   **[LivingDocGen.Generator](src/LivingDocGen.Generator/README.md)**: HTML rendering engine and theming system.
*   **[LivingDocGen.MSBuild](src/LivingDocGen.MSBuild/README.md)**: MSBuild integration and NuGet package details.
*   **[LivingDocGen.Parser](src/LivingDocGen.Parser/README.md)**: Universal Gherkin parsing logic.
*   **[LivingDocGen.TestReporter](src/LivingDocGen.TestReporter/README.md)**: Test result parsing and normalization.
*   **[LivingDocGen.Reqnroll.Integration](src/LivingDocGen.Reqnroll.Integration/README.md)**: Reqnroll-specific integration hooks.

```
┌──────────────────────────────────────────────────────────┐
│              LivingDocGen.CLI (Entry Point)               │
│  • Command-line interface                                 │
│  • .NET Global Tool                                       │
└──────────────────────────────────────────────────────────┘
                          │
                          ▼
┌──────────────────────────────────────────────────────────┐
│            LivingDocGen.Parser (Gherkin Parsing)          │
│  • Universal feature file parser                          │
│  • Cucumber, SpecFlow, Reqnroll, JBehave                 │
│  • Normalizes to UniversalFeature model                   │
└──────────────────────────────────────────────────────────┘
                          │
                          ▼
┌──────────────────────────────────────────────────────────┐
│      LivingDocGen.TestReporter (Test Results Parsing)     │
│  • NUnit (2, 3, 4 / XML & TRX)                           │
│  • xUnit, JUnit (XML)                                     │
│  • MSTest (TRX)                                           │
│  • SpecFlow/Cucumber (JSON)                               │
└──────────────────────────────────────────────────────────┘
                          │
                          ▼
┌──────────────────────────────────────────────────────────┐
│          LivingDoc.Generator (HTML Documentation)               │
│  • Single-file HTML with embedded CSS/JS                  │
│  • 6 beautiful themes                                      │
│  • Interactive filtering & search                          │
│  • Step-level execution details                            │
└──────────────────────────────────────────────────────────┘
                          │
                          ▼
              living-documentation.html ✨
```

---

## 🔨 Building from Source

### Prerequisites
- .NET 8 SDK or later
- Git

### Clone & Build

```bash
# Clone repository
git clone https://github.com/Suban5/LivingDocGen.git
cd LivingDocGen

# Restore dependencies
dotnet restore

# Build all projects
dotnet build

# Run tests
dotnet test

# Run CLI locally
dotnet run --project src/LivingDocGen.CLI/LivingDocGen.CLI.csproj -- generate ./samples/features ./samples/test-results
```

### Package for Distribution

```bash
# Build MSBuild integration package
dotnet pack src/LivingDocGen.MSBuild/LivingDoc.MSBuild.csproj -c Release -o ./nupkg

# Build global tool package
dotnet pack src/LivingDocGen.CLI/LivingDocGen.CLI.csproj -c Release -o ./nupkg

# Install local packages for testing
dotnet add package LivingDocGen --source ./nupkg
```

---

## 🎓 Academic Context

This project is part of a **Master's thesis** in Computer Engineering focused on:

### Research Questions
1. Can we design a **universal parser** for heterogeneous BDD frameworks?
2. Can we create a **better UX** for living documentation than existing tools?
3. Can **AI/NLP** improve the quality of BDD scenarios through automated analysis?

### Innovation & Contributions
- ✅ **Framework-Agnostic Architecture**: Works with any Gherkin-based framework
- ✅ **Smart Merging Algorithm**: Timestamp-based test result consolidation
- ✅ **Single-File DHTML**: Beautiful, self-contained, interactive reports
- ✅ **Step-Level Failure Detection**: Precise error location identification
- ✅ **Multi-Format Parser**: 6 test result formats supported
- 🔄 **AI Quality Analysis**: (Planned) NLP-based scenario quality scoring


---

## 🌟 What's Next?

### Planned Features (Phase 2 & 3)

- [ ] **AI/NLP Quality Analysis**
  - Scenario quality scoring
  - Duplicate scenario detection
  - Refactoring suggestions
  
- [ ] **Enhanced Visualizations**
  - Traceability graphs
  - Trend analysis over time
  - Code coverage integration
  
- [ ] **Extended Framework Support**
  - Playwright test results
  - Cypress test results
  - Jest/Mocha test results
  
- [ ] **Visual Studio Extension**
  - Test Explorer integration
  - One-click documentation generation
  - Live preview window

### Current Status

| Phase | Status | Description |
|-------|--------|-------------|
| Phase 1 | ✅ **Complete** | Universal parser, CLI tool, basic HTML generation |
| Phase 2 | ✅ **Complete** | MSBuild integration, multi-framework support, themes |
| Phase 3 | 🚧 In Progress | Advanced features, AI/NLP analysis |
| Phase 4 | 📋 Planned | User study, performance optimization |
| Phase 5 | 📋 Planned | Final thesis, publication |

---

## ❓ Frequently Asked Questions

### Q: Do I need to change my test code?
**A:** No! Zero code changes required. Just add the NuGet package and run tests.

### Q: Which test frameworks are supported?
**A:** NUnit (2, 3, 4), xUnit, JUnit, MSTest, SpecFlow, Cucumber.

### Q: Can I use this without MSBuild integration?
**A:** Yes! Install as a global tool: `dotnet tool install --global LivingDocGen.Tool`

### Q: How do I customize the theme?
**A:** Set `BDDLivingDocTheme` in `.csproj` or `theme` in `livingdocgen.json`. You can also change it in the browser after generation!

### Q: Does it work with CI/CD?
**A:** Absolutely! Use the global tool in your pipeline.

### Q: Can I generate documentation without test results?
**A:** Yes! Run: `LivingDocGen generate ./Features` (omit test results path)

### Q: What if I have multiple test result files?
**A:** The tool automatically merges them using smart timestamp-based logic.

### Q: Is the output a single file?

### Q: Does it support Scenario Outlines?
**A:** Yes! Full support for Scenario Outlines with multiple Examples sections and large data tables.

---

## 🤝 Contributing

This is a **Master's thesis project**, but contributions and feedback are very welcome!

### How to Contribute

1. **Report Issues**: Found a bug? [Open an issue](https://github.com/Suban5/LivingDocGen/issues)
2. **Suggest Features**: Have an idea? Share it in discussions
3. **Submit PRs**: Want to contribute code? Fork and submit a PR!
4. **Provide Feedback**: Use the tool and let me know what works (or doesn't)

### Development Setup

```bash
# Fork and clone
git clone https://github.com/YOUR-USERNAME/LivingDocGen.git
cd LivingDocGen

# Create a branch
git checkout -b feature/your-feature-name

# Make changes and test
dotnet build
dotnet test

# Commit and push
git commit -am "Add your feature"
git push origin feature/your-feature-name

# Open a Pull Request
```

---

## 📄 License

**MIT License**

```
Copyright (c) 2025 Subandhya Kumar

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

---

## 📬 Contact & Support

- **GitHub Repository**: [https://github.com/Suban5/LivingDocGen](https://github.com/Suban5/LivingDocGen)
- **Issues**: [Report a bug or request a feature](https://github.com/Suban5/LivingDocGen/issues)
- **Discussions**: [Ask questions or share ideas](https://github.com/Suban5/LivingDocGen/discussions)

---

## 🙏 Acknowledgments

This project uses the following excellent open-source libraries:

- **[Gherkin.NET](https://github.com/cucumber/gherkin-dotnet)** - Official Cucumber Gherkin parser
- **[System.CommandLine](https://github.com/dotnet/command-line-api)** - Modern CLI framework
- **NUnit, xUnit, JUnit** - For test result parsing
- **Various .NET libraries** - Listed in project files

Special thanks to the BDD community for inspiration and the existing tools (Cucumber, SpecFlow, Pickles, Allure) that paved the way.

---

<div align="center">

### ⭐ If you find this project useful, please give it a star! ⭐

Made with ❤️ for the BDD community

**[↑ Back to Top](#-universal-bdd-living-documentation-generator)**

</div>

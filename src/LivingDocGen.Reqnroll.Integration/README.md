# LivingDocGen.Reqnroll.Integration

Automatic living documentation generation for [Reqnroll](https://reqnroll.net/) test projects.

## 🎯 What It Does

This NuGet package automatically generates beautiful HTML living documentation after your Reqnroll tests complete. Simply add the package to your test project - no additional setup required.

**Input:**
- `.feature` files in your `Features/` folder
- Test execution results (NUnit/xUnit/MSTest/SpecFlow formats)

**Output:**
- `living-documentation.html` - Interactive HTML report with test results

## 📦 Installation

### Step 1: Add the Package

```bash
dotnet add package LivingDocGen.Reqnroll.Integration
```

### Step 2: Create Bridge File (Required)

**Why needed?** Reqnroll only discovers hooks from your test assembly, not from referenced DLLs. This bridge file calls the package hooks.

Create `Hooks/LivingDocGenBridge.cs` in your test project:

```csharp
using System;
using Reqnroll;
using LivingDocGen.Reqnroll.Integration.Bootstrap;

namespace YourTestProject.Hooks
{
    [Binding]
    public class LivingDocGenBridge
    {
        private static bool _testRunStarted = false;
        private static bool _testRunEnded = false;
        private static readonly object _lock = new object();

        [BeforeScenario(Order = int.MinValue)]
        public static void BeforeFirstScenario()
        {
            lock (_lock)
            {
                if (!_testRunStarted)
                {
                    _testRunStarted = true;
                    LivingDocBootstrap.BeforeTestRun();
                }
            }
        }
        
        [AfterTestRun(Order = int.MaxValue)]
        public static void AfterAllTests()
        {
            lock (_lock)
            {
                if (!_testRunEnded)
                {
                    _testRunEnded = true;
                    LivingDocBootstrap.AfterTestRun();
                }
            }
        }
    }
}
```

**That's it!** Documentation will now generate automatically after running tests.

## ✨ Key Features

- 📦 **Self-Contained** - Everything bundled in one package, no global tools needed
- 🎯 **Minimal Setup** - Just package + one bridge file (due to Reqnroll limitation)
- ⚙️ **Optional Configuration** - Customize via `livingdocgen.json` if needed
- ⚡ **Fast** - In-process generation (~500ms for 100 scenarios)
- 🎨 **6 Themes** - Purple, Blue, Green, Dark, Light, Pickles
- 📊 **Test Integration** - Shows pass/fail/skip status from your test results

## 🚀 Usage

### Setup Requirements

1. ✅ Package installed
2. ✅ Bridge file created (see Installation above)
3. ✅ Features folder with .feature files

### Default Behavior (No Additional Configuration)

```bash
dotnet test
```

**What happens:**
1. Your Reqnroll tests execute
2. After all tests complete, documentation is generated automatically
3. Output: `living-documentation.html` in your project root

**Console Output:**
```
🚀 LivingDocGen - Test run starting
   Project Root: /path/to/MyProject.Tests
   Test Runner: Visual Studio Test Explorer / dotnet test

📊 Generating Living Documentation...
   ✓ Found 5 feature file(s)
   ✓ Using test results: TestResult_20260101_123456.xml
   ✅ Living documentation generated successfully!
   📄 file:///path/to/MyProject.Tests/living-documentation.html
```

### Custom Configuration (Optional)

Create `livingdocgen.json` in your test project root:

```json
{
  "featurePath": "Features",
  "testResultsPath": "TestResults",
  "outputPath": "docs/living-doc.html",
  "title": "My Project - Living Documentation",
  "theme": "blue",
  "includeTestResults": true
}
```

**Configuration Options:**

| Property | Default | Description |
|----------|---------|-------------|
| `featurePath` | `Features/` | Folder containing .feature files |
| `testResultsPath` | `TestResults/` | Test results folder (auto-detected) |
| `outputPath` | `living-documentation.html` | Output HTML file path |
| `title` | `{ProjectName} - Living Documentation` | Document title |
| `theme` | `purple` | Visual theme (purple, blue, green, dark, light, pickles) |
| `includeTestResults` | `true` | Include test execution results |

## 📊 Supported Test Frameworks

- ✅ **NUnit** 2/3/4 (XML)
- ✅ **xUnit** (XML)
- ✅ **MSTest** (TRX)
- ✅ **SpecFlow** (JSON)

The integration automatically finds and parses the latest test result file.

## 🔧 Troubleshooting

### Issue: Documentation Not Generated

**Error:** No output, no console messages

**Solution:** You're missing the bridge file! See Installation Step 2 above.

**Why?** Reqnroll only discovers hooks in your test assembly, not from referenced DLLs. The bridge file is required to call the package hooks.

### Issue: Features Directory Not Found

**Error:**
```
⚠️ Features directory not found: /path/to/Features
```

**Solution:**
- Create a `Features` folder in your project root, OR
- Specify custom path in `livingdocgen.json`:
  ```json
  { "featurePath": "Specifications" }
  ```

### Issue: No .feature Files Found

**Error:**
```
⚠️ No .feature files found in: /path/to/Features
```

**Solution:**
- Ensure you have `.feature` files in the Features folder
- Verify files are included in your project

### Issue: No Test Results Found

**Warning:**
```
⚠️ No test results found in: /path/to/TestResults
   Generating documentation without test execution data...
```

**This is normal if:**
- Tests haven't run yet
- You're generating documentation before running tests

**Documentation will still be generated** without pass/fail indicators.

### Issue: Documentation Not Generated

**Checklist:**
- ✅ Package installed: `dotnet list package | grep LivingDocGen.Reqnroll.Integration`
- ✅ **Bridge file created:** `Hooks/LivingDocGenBridge.cs` (see Installation Step 2)
- ✅ `Features/` folder exists with .feature files
- ✅ Tests ran successfully (not skipped)
- ✅ Check console output for error messages

## ⚙️ Technical Details

**Target Framework:** .NET 6.0+  
**Compatible With:**
- Reqnroll 2.0.0+
- .NET 6, 7, 8, 9+
- Windows, macOS, Linux

**Important:** Due to Reqnroll's hook discovery mechanism, a bridge file is required in your test project (see Installation). This is a Reqnroll limitation, not a package issue.


## 📚 Links

- [GitHub Repository](https://github.com/suban5/LivingDocGen)
- [Main Documentation](../../README.md)
- [Changelog](../../CHANGELOG.md)
- [Report Issues](https://github.com/suban5/LivingDocGen/issues)

## 📄 License

MIT License - Copyright (c) 2024-2026 Suban Dhyako. See [LICENSE](../../LICENSE) for details.

---

**Questions?** Open an issue on [GitHub](https://github.com/suban5/LivingDocGen/issues)

## ✨ Features

- 🚀 **Automatic Generation**: Documentation created after all tests complete
- 📦 **Self-Contained**: No global tool installation - everything in one package!
- 🎯 **Zero Configuration**: Works out of the box with sensible defaults
- ⚙️ **Fully Configurable**: Optional `livingdocgen.json` for customization
- 🎨 **Multiple Test Runners**: Visual Studio Test Explorer, dotnet test, ReSharper, Rider, CI/CD
- 📊 **Test Results Integration**: Automatically includes execution results (pass/fail/skip)
- 🔍 **Smart Detection**: Auto-discovers features and test results
- 🎭 **6 Built-in Themes**: Purple, Blue, Green, Dark, Light, Pickles
- 💡 **Developer Friendly**: Clear console output with progress indicators
- ⚡ **Fast**: In-process generation (no external process spawning)

## 📦 Installation

**One command - that's it!**

```bash
dotnet add package LivingDocGen.Reqnroll.Integration
```

✅ **Self-contained** - No global tool installation required!  
✅ **Simple setup** - Package + one bridge file (Reqnroll requirement)  
✅ **No external dependencies** - Everything bundled in one package

Documentation will now generate automatically after tests run.

## 🚀 Quick Start

### Installation

**Step 1:** Add the package
```bash
dotnet add package LivingDocGen.Reqnroll.Integration
```

**Step 2:** Create bridge file `Hooks/LivingDocGenBridge.cs`:
```csharp
using System;
using Reqnroll;
using LivingDocGen.Reqnroll.Integration.Bootstrap;

namespace YourTestProject.Hooks
{
    [Binding]
    public class LivingDocGenBridge
    {
        private static bool _testRunStarted = false;
        private static bool _testRunEnded = false;
        private static readonly object _lock = new object();

        [BeforeScenario(Order = int.MinValue)]
        public static void BeforeFirstScenario()
        {
            lock (_lock)
            {
                if (!_testRunStarted)
                {
                    _testRunStarted = true;
                    LivingDocBootstrap.BeforeTestRun();
                }
            }
        }
        
        [AfterTestRun(Order = int.MaxValue)]
        public static void AfterAllTests()
        {
            lock (_lock)
            {
                if (!_testRunEnded)
                {
                    _testRunEnded = true;
                    LivingDocBootstrap.AfterTestRun();
                }
            }
        }
    }
}
```

**Why the bridge file?** Reqnroll only discovers hooks from your test assembly, not from NuGet packages.

### Zero Configuration Usage

After installation, simply run your tests using any method:

**Visual Studio Test Explorer:**
- Right-click on test project → Run Tests
- Documentation generates automatically ✅

**Command Line:**
```bash
dotnet test
# 🚀 LivingDocGen - Test run starting
# 📊 Generating Living Documentation...
# ✅ Living documentation generated successfully!
```

**Default Behavior:**
- **Features**: Reads from `Features/` folder
- **Test Results**: Latest file from `TestResults/` folder  
- **Output**: `living-documentation.html` in project root
- **Theme**: Purple
- **Title**: `{ProjectName} - Living Documentation`

### Custom Configuration (Optional)

Create `livingdocgen.json` in your test project root:

```json
{
  "featurePath": "Features",
  "testResultsPath": "TestResults",
  "outputPath": "docs/living-documentation.html",
  "title": "My Amazing Project - Living Documentation",
  "theme": "blue",
  "includeTestResults": true
}
```

**Available Themes:** `purple` | `blue` | `green` | `dark` | `light` | `pickles`

## ⚙️ How It Works

```
Test Execution Flow
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
┌─────────────────────────────────────────────────┐
│  1. Test runner starts (VS/CLI/CI)             │
│     [BeforeTestRun] hook initializes            │
└──────────────────┬──────────────────────────────┘
                   │
┌──────────────────▼──────────────────────────────┐
│  2. Reqnroll executes all scenarios             │
│     Given/When/Then steps run                   │
│     Test results collected                      │
└──────────────────┬──────────────────────────────┘
                   │
┌──────────────────▼──────────────────────────────┐
│  3. All tests complete                          │
│     Test results written to TestResults/        │
│     [AfterTestRun] hook triggers                │
└──────────────────┬──────────────────────────────┘
                   │
┌──────────────────▼──────────────────────────────┐
│  4. LivingDocGen generates documentation        │
│     ✓ Parses .feature files                     │
│     ✓ Integrates test results                   │
│     ✓ Generates styled HTML                     │
│     ✓ Opens in browser (optional)               │
└─────────────────────────────────────────────────┘
```

### Console Output Example

```
🚀 LivingDocGen - Test run starting
   Project Root: /Users/dev/MyProject.Tests
   Test Runner: Visual Studio Test Explorer / dotnet test

[... tests execute ...]

📊 Generating Living Documentation...
   ✓ Using config file: /Users/dev/MyProject.Tests/livingdocgen.json
   ✓ Using test results: TestResult_20260101_123456.xml
   ✅ Living documentation generated successfully!
   📄 file:///Users/dev/MyProject.Tests/living-documentation.html
```

## 📋 Configuration Reference

### livingdocgen.json Schema

```json
{
  "featurePath": "Features",           // Path to .feature files (required)
  "testResultsPath": "TestResults",    // Test results folder (optional)
  "outputPath": "living-doc.html",     // Output HTML path (optional)
  "title": "Living Documentation",     // Document title (optional)
  "theme": "purple",                   // Theme name (optional)
  "includeTestResults": true           // Include test data (optional)
}
```

### Default Values

| Property | Default Value | Description |
|----------|---------------|-------------|
| `featurePath` | `Features/` | Folder containing .feature files |
| `testResultsPath` | `TestResults/` | Folder for test result files |
| `outputPath` | `living-documentation.html` | Output HTML file path |
| `title` | `{ProjectName} - Living Documentation` | Document title |
| `theme` | `purple` | Visual theme |
| `includeTestResults` | `true` | Include execution results |

### Supported Test Result Formats

- ✅ **NUnit** 2/3/4 (XML format)
- ✅ **xUnit** (XML format)
- ✅ **MSTest** (TRX format)
- ✅ **SpecFlow** (JSON format)

The integration automatically detects the latest result file and format.

## 🌍 Supported Environments

### Test Runners
- ✅ **Visual Studio** 2019/2022 Test Explorer
- ✅ **dotnet test** CLI
- ✅ **ReSharper** Test Runner
- ✅ **JetBrains Rider** Test Runner
- ✅ **CI/CD**: GitHub Actions, Azure Pipelines, Jenkins, GitLab CI, etc.

### Platforms
- ✅ **Windows** (x64, ARM64)
- ✅ **macOS** (Intel, Apple Silicon)
- ✅ **Linux** (Ubuntu, Debian, RHEL, Alpine)

### .NET Versions
- ✅ **.NET 6.0** (LTS)
- ✅ **.NET 7.0**
- ✅ **.NET 8.0** (LTS)
- ✅ **.NET 9.0+**

## 💼 Real-World Examples

### Example 1: E-commerce Test Suite

```bash
MyEcommerce.Tests/
├── Features/
│   ├── Checkout.feature
│   ├── ProductCatalog.feature
│   └── UserAccount.feature
├── StepDefinitions/
├── livingdocgen.json          # ← Custom config
└── living-documentation.html  # ← Generated output
```

**livingdocgen.json:**
```json
{
  "featurePath": "Features",
  "outputPath": "living-documentation.html",
  "title": "E-commerce Platform - BDD Specifications",
  "theme": "blue"
}
```

### Example 2: CI/CD Pipeline Integration

```yaml
# .github/workflows/test.yml
name: Tests and Documentation

on: [push, pull_request]

jobs:
  test-and-document:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
      
      - name: Run Tests (Auto-generates documentation)
        run: dotnet test --logger trx
      
      - name: Upload Living Documentation
        uses: actions/upload-artifact@v4
        with:
          name: living-documentation
          path: '**/living-documentation.html'
      
      - name: Publish to GitHub Pages (optional)
        uses: peaceiris/actions-gh-pages@v3
        if: github.ref == 'refs/heads/main'
        with:
          github_token: ${{ secrets.GITHUB_TOKEN }}
          publish_dir: ./tests/
          publish_branch: gh-pages
```

### Example 3: Multiple Test Projects

```
Solution/
├── Tests.API/
│   ├── Features/
│   ├── livingdocgen.json      # API-specific config
│   └── living-documentation.html
├── Tests.Integration/
│   ├── Features/
│   ├── livingdocgen.json      # Integration-specific config
│   └── living-documentation.html
└── Tests.E2E/
    ├── Features/
    ├── livingdocgen.json      # E2E-specific config
    └── living-documentation.html
```

Each project generates its own documentation independently.

## 🔧 Troubleshooting

### ⚠️ Issue: Features Directory Not Found

**Console Output:**
```
⚠️ Features directory not found: /path/to/project/Features
   Create a 'Features' folder or use livingdocgen.json to specify custom path
```

**Solutions:**

**Option 1:** Create Features folder
```bash
mkdir Features
```

**Option 2:** Use custom path in `livingdocgen.json`
```json
{
  "featurePath": "Specifications"
}
```

### ⚠️ Issue: No Test Results Found

**Console Output:**
```
⚠️ No test results found in: /path/to/project/TestResults
   Generating documentation without test execution data...
```

**This is usually fine!** Documentation will be generated without pass/fail indicators.

**To include test results:**
1. Ensure tests are actually running (not all skipped)
2. Check TestResults folder exists after test run
3. Verify test logger is enabled (it should be by default)

### ⚠️ Issue: Documentation Not Generated

**Checklist:**
- ✅ Integration package added to test project (`dotnet list package`)
- ✅ `Features/` folder exists with .feature files
- ✅ Tests actually ran (check test output)
- ✅ Check console for error messages
- ✅ Verify package restore completed successfully

**Common Causes:**
- Tests skipped or not executed
- Features folder empty or missing .feature files
- Exception during generation (check console output)

### ⚠️ Issue: Wrong Test Results Used

The integration uses the **latest modified** file from TestResults folder.

**To ensure correct results:**
- Clean TestResults folder before running: `rm -rf TestResults/*`
- Or specify exact path in `livingdocgen.json`:
  ```json
  {
    "testResultsPath": "TestResults/latest"
  }
  ```

## 🎯 Advanced Usage

### Custom Output Locations

The `outputPath` property is fully configurable - you can change the file name, directory, or use absolute paths:

**Different file names:**
```json
{
  "outputPath": "specs.html"
}
```
```json
{
  "outputPath": "bdd-report.html"
}
```

**Different directories (relative paths):**
```json
{
  "outputPath": "artifacts/reports/living-doc.html"
}
```
```json
{
  "outputPath": "docs/test-results/documentation.html"
}
```

**Absolute paths:**
```json
{
  "outputPath": "/var/reports/living-documentation.html"
}
```

**Complete example:**
```json
{
  "featurePath": "Features",
  "outputPath": "artifacts/reports/documentation/living-doc.html",
  "title": "v1.0.4 Release - BDD Specifications"
}
```

### Documentation Without Test Results

```json
{
  "featurePath": "Features",
  "outputPath": "specs.html",
  "includeTestResults": false
}
```

Useful for generating specification documentation before tests are implemented.

### Environment-Specific Configurations

Create multiple config files:

```bash
livingdocgen.dev.json
livingdocgen.staging.json
livingdocgen.prod.json
```

Rename the appropriate one to `livingdocgen.json` during CI/CD.

## 🚀 Future Enhancements

The following features are planned for future releases:

### Planned Features

- [ ] **Environment Variable Configuration**
  - Override settings via `LIVINGDOC_THEME`, `LIVINGDOC_OUTPUT_PATH`, etc.
  - Useful for CI/CD without config files

- [ ] **Multi-Language Support**
  - Documentation in multiple languages (English, Spanish, German, French)
  - Configurable via `"language": "es"` in config

- [ ] **PDF Export**
  - Generate PDF alongside HTML
  - `"outputFormats": ["html", "pdf"]`

- [ ] **Markdown Export**
  - Export as Markdown for GitHub wikis
  - `"outputFormats": ["html", "markdown"]`

- [ ] **Custom Templates**
  - User-provided Razor templates
  - `"templatePath": "custom-template.cshtml"`

- [ ] **Screenshot Embedding**
  - Automatically embed screenshots from test failures
  - Integration with Selenium/Playwright

- [ ] **Real-time Documentation Server**
  - Live-reload documentation during development
  - `LivingDocGen serve --watch`

- [ ] **Tag Filtering**
  - Generate documentation for specific tags only
  - `"includeTags": ["@smoke", "@regression"]`

- [ ] **Automatic Opening**
  - Open documentation in browser after generation
  - `"openInBrowser": true`

- [ ] **Confluence/SharePoint Integration**
  - Publish directly to Confluence pages
  - `"publishTo": "confluence"`

### Community Contributions Welcome!

Have an idea? [Open an issue](https://github.com/suban5/LivingDocGen/issues) or submit a PR!

## 📚 Additional Resources

### Documentation
- 📖 [Main README](../../README.md) - Project overview and features
- 🏗️ [Architecture Guide](../../docs/ARCHITECTURE.md) - System design and components
- 🛠️ [Development Guide](../../docs/DEVELOPMENT.md) - Contributing and building
- 📘 [API Reference](../../docs/API_REFERENCE.md) - Programmatic usage

### Related Packages
- 🔧 [LivingDocGen.Tool](https://www.nuget.org/packages/LivingDocGen.Tool) - CLI tool (required)
- 📦 [LivingDocGen.Core](https://www.nuget.org/packages/LivingDocGen.Core) - Core library
- 🏗️ [LivingDocGen.MSBuild](https://www.nuget.org/packages/LivingDocGen.MSBuild) - MSBuild integration

### Community
- 🏠 [GitHub Repository](https://github.com/suban5/LivingDocGen)
- 🐛 [Issue Tracker](https://github.com/suban5/LivingDocGen/issues)
- 📝 [Changelog](../../CHANGELOG.md)
- 🔒 [Security Policy](../../SECURITY.md)

## ⚙️ Technical Details

### Target Framework
- **.NET 6.0** - Requires .NET 6.0 runtime or higher
  - Compatible with Reqnroll 2.0.0+
  - Works on Windows, macOS, and Linux

### Dependencies
- **Reqnroll** ≥ 2.0.0
- All generation libraries bundled internally (self-contained)

### Hook Lifecycle
```csharp
[BeforeTestRun]  // Initialize paths, display banner
↓
[Test Execution]  // Scenarios run, results collected
↓
[AfterTestRun]   // Generate documentation with results
```

## 🤝 Contributing

Contributions are welcome! See [CONTRIBUTING.md](../../CONTRIBUTING.md) for guidelines.

### Quick Start for Contributors
```bash
git clone https://github.com/suban5/LivingDocGen.git
cd LivingDocGen
dotnet build
dotnet test
```

## 📄 License

MIT License - Copyright (c) 2024-2026 Suban Dhyako

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

See [LICENSE](../../LICENSE) file for full details.

## 👤 Author

**Suban Dhyako**
- GitHub: [@suban5](https://github.com/suban5)
- LinkedIn: [Suban Dhyako](https://www.linkedin.com/in/suban-dhyako/)

## 🙏 Acknowledgments

- [Reqnroll Team](https://reqnroll.net/) - For the excellent BDD framework
- [SpecFlow Community](https://specflow.org/) - For BDD best practices and inspiration
- [Pickles](http://www.picklesdoc.com/) - For living documentation concepts

---

<div align="center">

**Made with ❤️ for the BDD community**

[⬆ Back to Top](#livingdocgenreqnrollintegration)

</div>

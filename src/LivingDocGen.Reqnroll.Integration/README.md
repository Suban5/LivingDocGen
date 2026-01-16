# LivingDocGen.Reqnroll.Integration

[![NuGet](https://img.shields.io/nuget/v/LivingDocGen.Reqnroll.Integration.svg)](https://www.nuget.org/packages/LivingDocGen.Reqnroll.Integration/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](../../LICENSE)

Automatic living documentation generation for [Reqnroll](https://reqnroll.net/) BDD test projects.

## Overview

Seamlessly integrate living documentation generation into your Reqnroll test execution. This package automatically generates beautiful, interactive HTML reports after your tests complete.

**What You Get:**
- 📊 **Automatic Generation** - Documentation created after test execution
- 🎨 **6 Built-in Themes** - Purple, Blue, Green, Dark, Light, Pickles
- ✅ **Test Results Integration** - Shows pass/fail/skip status
- 📱 **Responsive Design** - Works on all devices
- 🔍 **Interactive Features** - Search, filter by status, collapsible sections
- ⚡ **Fast** - In-process generation (~500ms for 100 scenarios)

---

## 📦 Installation & Setup

### Step 1: Install Package

```bash
dotnet add package LivingDocGen.Reqnroll.Integration
```

Or via Package Manager:
```powershell
Install-Package LivingDocGen.Reqnroll.Integration
```

### Step 2: Create Bridge File (Required)

> **Why?** Reqnroll only discovers hooks from your test assembly, not from NuGet packages. This bridge file connects the integration to your tests.

Create `Hooks/LivingDocGenBridge.cs` in your test project:

```csharp
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

### Step 3: Run Your Tests

```bash
dotnet test
```

**Output:**
```
🚀 LivingDocGen - Test run starting
📊 Generating Living Documentation...
   ✓ Found 5 feature file(s)
   ✓ Using test results: TestResult_20260115_143022.xml
✅ Living documentation generated successfully!
📄 file:///path/to/your/project/living-documentation.html
```

That's it! Documentation will be generated automatically as `living-documentation.html` in your project root.

---

## ⚙️ Configuration

### Default Behavior (Zero Configuration)

Works out of the box with these defaults:
- **Features**: `Features/` folder
- **Test Results**: Latest file from `TestResults/`
- **Output**: `living-documentation.html` in project root
- **Theme**: Purple
- **Title**: `{ProjectName} - Living Documentation`

### Custom Configuration (Optional)

Create `livingdocgen.json` in your test project root:

```json
{
  "featurePath": "Features",
  "testResultsPath": "TestResults",
  "outputPath": "docs/living-doc.html",
  "title": "My Project Documentation",
  "theme": "blue",
  "includeComments": true,
  "includeTestResults": true
}
```

**All Configuration Options:**

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `featurePath` | string | `"Features/"` | Path to .feature files |
| `testResultsPath` | string | `"TestResults/"` | Test results folder |
| `outputPath` | string | `"living-documentation.html"` | Output HTML path |
| `title` | string | `"{ProjectName} - Living Documentation"` | Document title |
| `theme` | string | `"purple"` | Theme: `purple`, `blue`, `green`, `dark`, `light`, `pickles` |
| `includeComments` | bool | `true` | Include Gherkin comments |
| `includeTestResults` | bool | `true` | Include test execution results |

---

## 🧪 Supported Test Frameworks

The integration auto-detects and parses:
- ✅ **NUnit** 2/3 (XML format), NUnit 4 (XML and TRX format)
- ✅ **xUnit** (XML format)
- ✅ **MSTest** (TRX format)
- ✅ **SpecFlow** (JSON format)

---

## 🔧 Troubleshooting

### Documentation Not Generated

**Checklist:**
1. ✅ Package installed: `dotnet list package | grep LivingDocGen`
2. ✅ **Bridge file created** in `Hooks/LivingDocGenBridge.cs`
3. ✅ `Features/` folder exists with `.feature` files
4. ✅ Tests ran successfully (not skipped)
5. ✅ Check console for error messages

### Features Directory Not Found

**Error:**
```
⚠️ Features directory not found: /path/to/Features
```

**Solutions:**
- Create a `Features` folder in your project root, OR
- Specify custom path in `livingdocgen.json`:
  ```json
  { "featurePath": "Specifications" }
  ```

### No Test Results Found

**Warning:**
```
⚠️ No test results found in: /path/to/TestResults
   Generating documentation without test execution data...
```

**This is normal if:**
- Tests haven't run yet
- Running without test execution

**Documentation will still be generated** without pass/fail indicators.

---

## 💡 Examples

### CI/CD Integration (GitHub Actions)

```yaml
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
```

### Multiple Test Projects

```
Solution/
├── Tests.API/
│   ├── Features/
│   ├── livingdocgen.json
│   └── living-documentation.html
├── Tests.Integration/
│   ├── Features/
│   ├── livingdocgen.json
│   └── living-documentation.html
└── Tests.E2E/
    ├── Features/
    ├── livingdocgen.json
    └── living-documentation.html
```

Each project generates its own documentation independently.

---

## 🌍 Compatibility

**Target Framework:** .NET 6.0+

**Compatible With:**
- Reqnroll 2.0.0+
- .NET 6, 7, 8, 9+
- Windows, macOS, Linux
- Visual Studio, VS Code, Rider, ReSharper
- All CI/CD platforms

---

## 📚 Additional Resources

- 📖 [Main Documentation](../../README.md) - Complete project overview
- 📝 [Changelog](CHANGELOG.md) - Version history
- 🏗️ [Architecture Guide](../../docs/ARCHITECTURE.md) - Technical details
- 🐛 [Report Issues](https://github.com/suban5/LivingDocGen/issues)
- 💬 [GitHub Discussions](https://github.com/suban5/LivingDocGen/discussions)

---

## 📄 License

MIT License - Copyright (c) 2024-2026 Suban Dhyako  
See [LICENSE](../../LICENSE) for full details.

---

## 🙏 Acknowledgments

- [Reqnroll Team](https://reqnroll.net/) - Excellent BDD framework
- [SpecFlow Community](https://specflow.org/) - BDD best practices
- [Pickles](http://www.picklesdoc.com/) - Living documentation inspiration

---

**Made with ❤️ for the BDD community**

[⬆ Back to Top](#livingdocgenreqnrollintegration)


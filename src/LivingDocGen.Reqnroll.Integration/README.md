# LivingDocGen.Reqnroll.Integration

[![NuGet](https://img.shields.io/nuget/v/LivingDocGen.Reqnroll.Integration.svg)](https://www.nuget.org/packages/LivingDocGen.Reqnroll.Integration/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](../../LICENSE)

Automatic living documentation generation for [Reqnroll](https://reqnroll.net/) BDD test projects.

## Overview

This package automatically generates living documentation for your Reqnroll BDD test projects. After your tests run, it creates an interactive HTML report showing all your features, scenarios, and test results.

### 🔄 Integration vs CLI

**This is the Reqnroll Integration package** - designed to work directly inside your test project.

- **No CLI installation needed** - Works automatically when you run your tests
- **No command-line needed** - Everything happens during test execution
- **Separate from LivingDocGen.CLI** - The CLI tool is for manual report generation via command prompt

**Use this package when:**
- ✅ You want automatic documentation after every test run
- ✅ You're working within a Reqnroll test project
- ✅ You want zero-configuration setup

**Use the CLI instead when:**
- You need to generate reports manually
- You're working outside a Reqnroll project
- You need command-line control

---

**What You Get:**
- 📊 **Automatic Generation** - Documentation created every time tests complete
- 🎨 **6 Built-in Themes** - Purple, Blue, Green, Dark, Light, Pickles
- ✅ **Test Results** - Shows which scenarios passed, failed, or were skipped
- 🔍 **Interactive Features** - Search scenarios, filter by status, collapse/expand sections
- ⚡ **Fast** - Generates in about 500ms for 100 scenarios

## ⚠️ Version Compatibility

**Reqnroll 3.3.2+ Required**

This package requires **Reqnroll 3.3.2 or higher**. If you're using an older version of Reqnroll, please upgrade:

```bash
dotnet add package Reqnroll --version 3.3.2
dotnet add package Reqnroll.NUnit --version 3.3.2
```

**Dependencies:**
- Reqnroll: 3.3.2+
- Gherkin: 35.0.0
- .NET: 6.0+

---

## ✨ What's New in v2.0.5 🎉

**Release Date:** January 26, 2026

**Tag Filtering & UX Improvements:**
- ✅ **Tag Filtering** - Filter scenarios by tags with dropdown selector
  - Feature-level and scenario-level tag support
  - Case-insensitive tag matching
  - Integrated with unified filter system
- ✅ **Improved Controls Layout** - Better UX organization
  - New logical order: Status filters → Tag filter → Search → Clear All → Theme
  - Enhanced visual grouping and filtering workflow
- ✅ **Search Navigation** - Fixed prev/next button functionality
- ✅ **Accurate Counts** - Formula-based untested scenario calculation
- ✅ **Simplified Navigation** - Removed redundant sidebar search

**Impact:** Enhanced filtering capabilities and more intuitive controls layout for better user experience with generated reports.

See [CHANGELOG.md](CHANGELOG.md) for complete release notes.

---

## 📦 Installation & Setup

### Prerequisites

Before you start, make sure you have:
- A Reqnroll test project (not a regular .NET project)
- Reqnroll 3.3.2 or higher installed
- .NET 6.0 or higher
- At least one `.feature` file in your project

---

### Step 1: Install the Package

**Option A: Using .NET CLI (Recommended)**

Open a terminal in your test project folder and run:

```bash
dotnet add package LivingDocGen.Reqnroll.Integration
```

**Option B: Using Visual Studio Package Manager**

In Visual Studio:
1. Right-click your test project
2. Select "Manage NuGet Packages"
3. Search for "LivingDocGen.Reqnroll.Integration"
4. Click "Install"

**Option C: Using Package Manager Console**

In Visual Studio, open Package Manager Console and run:

```powershell
Install-Package LivingDocGen.Reqnroll.Integration
```

---

### Step 2: Create the Bridge File

**Why is this needed?**  
Reqnroll can only find hooks that are in your test project code, not in NuGet packages. This bridge file connects the package to your tests.

**How to create it:**

1. In your test project, create a folder named `Hooks` (if it doesn't exist)
2. Inside the `Hooks` folder, create a new file called `LivingDocGenBridge.cs`
3. Copy and paste this code:

```csharp
using Reqnroll;
using LivingDocGen.Reqnroll.Integration.Bootstrap;

namespace YourTestProject.Hooks
{
    [Binding]
    public class LivingDocGenBridge
    {
        [BeforeTestRun(Order = int.MinValue)]
        public static void BeforeAllTests()
        {
            LivingDocBootstrap.BeforeTestRun();
        }
        
        [AfterTestRun(Order = int.MaxValue)]
        public static void AfterAllTests()
        {
            LivingDocBootstrap.AfterTestRun();
        }
    }
}
```

4. Change `YourTestProject.Hooks` to match your actual test project namespace

**Example project structure:**
```
YourTestProject/
├── Features/
│   ├── Login.feature
│   └── ShoppingCart.feature
├── Hooks/
│   └── LivingDocGenBridge.cs    ← Create this file
└── YourTestProject.csproj
```

> **Note:** This code runs once before all tests start and once after all tests finish.

---

### Step 3: Run Your Tests

```bash
dotnet test
```

**Output:**
```
========================================
🚀 LivingDocGen - Test run starting
   Project Root: /path/to/your/project
   Test Results Path: /path/to/your/project/TestResults
   Test Runner: Visual Studio Test Explorer / dotnet test
========================================

📊 Generating Living Documentation...
   ✓ Config File: /path/to/your/project/livingdocgen.json
      Feature Path: /path/to/Features
      Test Results: /path/to/TestResults/xml
      Output: living-documentation.html
      Theme: blue

   ✓ Found 115 feature file(s)
      First file: login.feature
      Last file: shopping_cart.feature

   ✓ Using test results:
      File: /path/to/TestResults/xml/test-results.xml
      Size: 123,456 bytes (120.56 KB)
      Modified: 2026-01-26 21:56:00

========================================
   ✅ Living documentation generated successfully!
   📄 Output File: /path/to/living-documentation.html
   📊 File Size: 1,726,198 bytes (1685.74 KB)
   🔗 Open in browser: file:///path/to/living-documentation.html
========================================
```

**Files Created:**
- `living-documentation.html` - Interactive report
- `LIVINGDOC_DEBUG.txt` - Detailed debug log

**What happens:**
- `living-documentation.html` is created in your project root folder
- `LIVINGDOC_DEBUG.txt` is created with detailed generation information

**Where to find the report:**
Look for `living-documentation.html` in the same folder as your `.csproj` file.

That's it! The documentation generates automatically every time you run your tests.

---

## ⚙️ Configuration

### Default Settings (Works Without Configuration)

The package works immediately after installation with these default settings:

| Setting | Default Value | What It Means |
|---------|---------------|---------------|
| **Features folder** | `Features/` | Where your `.feature` files are located |
| **Test results folder** | `TestResults/` | Where test runner saves results |
| **Output file** | `living-documentation.html` | Name of the generated report |
| **Theme** | Purple | Color scheme of the report |
| **Title** | `{YourProjectName} - Living Documentation` | Title shown in the report |

**You don't need to configure anything** - it will work with these defaults.

---

### Custom Configuration (Optional)

If you want to change any default settings, create a file named `livingdocgen.json` in the same folder as your `.csproj` file.

**Example configuration file:**

```json
{
  "featurePath": "path/to/Feature/files",
  "testResultsPath": "path/where/testresult/will/generate/after/executing/test",
  "outputPath": "path/where/to/generate/html/report_name.html",
  "title": "My Project Test Report",
  "theme": "blue",
  "includeComments": true,
  "includeTestResults": true
}
```

**Configuration Options Explained:**

| Setting | What You Can Enter | What It Does |
|---------|-------------------|---------------|
| `featurePath` | Folder path (e.g., `"Features"`, `"Specs"`) | Tells the tool where to find your `.feature` files |
| `testResultsPath` | Folder path (e.g., `"TestResults"`, `"TestResults/xml"`) | Where to look for test result files |
| `outputPath` | File path (e.g., `"report.html"`, `"docs/living-doc.html"`) | Where to save the generated HTML report |
| `title` | Any text (e.g., `"My Project Documentation"`) | Title displayed at the top of the report |
| `theme` | `purple`, `blue`, `green`, `dark`, `light`, or `pickles` | Color scheme for the report |
| `includeComments` | `true` or `false` | Whether to show comments from your `.feature` files |
| `includeTestResults` | `true` or `false` | Whether to include pass/fail/skip indicators |

**Tips:**
- Start without a config file - only create one if you need to change something
- Paths can be relative (e.g., `"Features"`) or absolute (e.g., `"C:/Projects/MyTests/Features"`)
- If a folder doesn't exist, you'll see an error message telling you what to do

---

## 🧪 Supported Test Frameworks

This package works with these test runners:

| Test Framework | Supported Versions | Result File Format |
|----------------|-------------------|--------------------|
| **NUnit** | 2, 3, 4 | XML files (`.xml`) and TRX files (`.trx`) for NUnit 4 |
| **xUnit** | All versions | XML files (`.xml`) |
| **MSTest** | All versions | TRX files (`.trx`) |
| **SpecFlow** | All versions | JSON files (`.json`) |

**The package automatically finds and reads test result files** - you don't need to specify which framework you're using.

---

## 🔧 Troubleshooting Guide

### Using the Debug Log File

**What is it?**  
Every time documentation is generated, a file called `LIVINGDOC_DEBUG.txt` is created in your test project folder. This file contains detailed information about what happened.

**Where to find it:**  
Look in the same folder as your `.csproj` file.

**What's inside:**
- When generation started (timestamp)
- Where the tool looked for files (full paths)
- What configuration was used
- How many feature files were found
- Which test result file was used (including file size and date)
- Whether HTML generation succeeded
- Any errors that occurred (with full details)

**When to check it:**
- ❌ Report wasn't created
- ⚠️ Features or test results are missing from the report
- 🐛 Something went wrong but you're not sure what

**Example of what you'll see:**
```
Living Documentation Generation Debug Log
Timestamp: 2026-01-26 21:56:47
Project Root: C:/Projects/MyTests
Test Results Path: C:/Projects/MyTests/TestResults

Config loaded - Feature Path: C:/Projects/MyTests/Features
Config loaded - Output: living-documentation.html
Config loaded - Theme: blue

Feature files found: 10
Test Result File: C:/Projects/MyTests/TestResults/test-results.xml
File Size: 123456 bytes

Output file created: C:/Projects/MyTests/living-documentation.html
File size: 1726198 bytes
```

> **💡 Best Practice:** Always check `LIVINGDOC_DEBUG.txt` first when something goes wrong. It usually tells you exactly what the problem is.

---

### Common Problems and Solutions

#### Problem 1: No HTML Report Was Created

**How to diagnose:**
1. Check if the package is installed:
   ```bash
   dotnet list package
   ```
   Look for "LivingDocGen.Reqnroll.Integration" in the list.

2. Verify the bridge file exists:
   - Open your project
   - Check if `Hooks/LivingDocGenBridge.cs` exists
   - If not, go back to Step 2 in Installation & Setup

3. Make sure you have feature files:
   - Look for a `Features` folder in your project
   - Make sure it contains `.feature` files
   - If the folder is named something else, create a `livingdocgen.json` config file

4. Check if tests actually ran:
   - If tests were skipped or cancelled, documentation won't generate
   - Run tests again: `dotnet test`

5. Look at the console output:
   - Scroll through the test output
   - Look for messages starting with "🚀 LivingDocGen" or "❌ ERROR"

6. Open `LIVINGDOC_DEBUG.txt`:
   - This file shows exactly what happened
   - Look for error messages at the bottom

---

#### Problem 2: "Features Directory Not Found" Error

**What you'll see:**
```
========================================
   ❌ ERROR: Features directory not found
      Expected path: C:/Projects/MyTests/Features
      Project root: C:/Projects/MyTests
   Solution: Create a 'Features' folder or specify custom path
========================================
```

**Why this happens:**  
The tool is looking for a `Features` folder but can't find it.

**How to fix it:**

**Option 1: Create the Features folder**
1. Create a folder named `Features` in your test project
2. Make sure your `.feature` files are inside it
3. Run tests again

**Option 2: Tell the tool where your features are**
1. Create a file named `livingdocgen.json` in your project folder
2. Add this content (change the path to match your folder name):
   ```json
   {
     "featurePath": "YourFolderName"
   }
   ```
3. Save the file
4. Run tests again

**Example:** If your features are in a folder called `Specifications`:
```json
{
  "featurePath": "Specifications"
}
```

---

#### Problem 3: Report Shows No Test Results (All Gray)

**What you'll see:**
```
   ⚠️ No test results found
      Searched in: C:/Projects/MyTests/TestResults
   Generating documentation without test execution data...
```

**Why this happens:**  
The tool couldn't find any test result files, so it generates the report without showing pass/fail status.

**This is normal if:**
- You just added the package and haven't run tests yet
- Tests were cancelled or didn't complete
- Test runner is still saving results (happens very quickly, usually not a problem)

**How to get test results in your report:**

1. **Make sure tests actually run:**
   ```bash
   dotnet test
   ```
   Wait for all tests to finish.

2. **Check if result files were created:**
   - Look in your project folder for a `TestResults` folder
   - Open it and look for `.xml` or `.trx` files
   - If empty, your test runner might not be configured to save results

3. **For NUnit users:** Make sure NUnit3TestAdapter is installed:
   ```bash
   dotnet add package NUnit3TestAdapter
   ```

4. **For xUnit users:** xUnit automatically creates result files.

5. **For MSTest users:** Results are saved in `.trx` format automatically.

6. **Check the debug log:**
   - Open `LIVINGDOC_DEBUG.txt`
   - Look for "Test Result File:" to see what file was found
   - If it says "No test results found", check the "Searched in:" path

**Note:** The report will still be created and show all your features and scenarios. They just won't have green (pass), red (fail), or yellow (skip) indicators.

---

#### Problem 4: Bridge File Not Working

**Symptoms:**
- Tests run but no documentation is generated
- No console output about LivingDocGen
- No `LIVINGDOC_DEBUG.txt` file created

**How to fix it:**

1. **Check the bridge file exists:**
   - Look for `Hooks/LivingDocGenBridge.cs` in your project

2. **Check the namespace:**
   - Open `LivingDocGenBridge.cs`
   - Make sure the namespace matches your project
   - Example: If your project is "MyCompany.Tests", the namespace should be "MyCompany.Tests.Hooks"

3. **Check the using statements:**
   - Make sure these lines are at the top:
     ```csharp
     using Reqnroll;
     using LivingDocGen.Reqnroll.Integration.Bootstrap;
     ```

4. **Rebuild your project:**
   ```bash
   dotnet build
   ```

5. **Make sure the file is included in your project:**
   - In Visual Studio, the file should appear in Solution Explorer
   - Check if it has a red icon (means build error)

---

### Still Having Problems?

If none of these solutions work:

1. **Check the debug log first:**
   - Open `LIVINGDOC_DEBUG.txt`
   - Read any error messages carefully
   - They usually tell you exactly what's wrong

2. **Verify your setup:**
   - Package installed? (`dotnet list package`)
   - Bridge file created? (Check `Hooks` folder)
   - Features folder exists? (Check project root)
   - Correct Reqnroll version? (Need 3.3.2+)

3. **Try the basics:**
   - Clean and rebuild: `dotnet clean && dotnet build`
   - Run tests again: `dotnet test`
   - Check console output for error messages

4. **Get help:**
   - Check [GitHub Issues](https://github.com/suban5/LivingDocGen/issues) for similar problems
   - Create a new issue with:
     - Your `LIVINGDOC_DEBUG.txt` content
     - Console output from test run
     - Description of what you expected vs. what happened

---

## 💡 Usage Examples

### Example 1: Multiple Test Projects in One Solution

If you have multiple test projects, each one can generate its own report independently.

**Project structure:**
```
MySolution/
├── Tests.API/                        ← First test project
│   ├── Features/
│   │   └── API.feature
│   ├── Hooks/
│   │   └── LivingDocGenBridge.cs
│   ├── livingdocgen.json             (optional)
│   └── living-documentation.html     ← Generated here
│
├── Tests.Integration/                ← Second test project
│   ├── Features/
│   │   └── Database.feature
│   ├── Hooks/
│   │   └── LivingDocGenBridge.cs
│   ├── livingdocgen.json             (optional)
│   └── living-documentation.html     ← Generated here
│
└── Tests.E2E/                        ← Third test project
    ├── Features/
    │   └── UserJourney.feature
    ├── Hooks/
    │   └── LivingDocGenBridge.cs
    ├── livingdocgen.json             (optional)
    └── living-documentation.html     ← Generated here
```

**What happens:**
- Each test project gets its own bridge file
- Each project generates its own HTML report
- Reports are independent and can use different themes/settings

---

### Example 2: Custom Output Location

If you want the report in a specific folder:

**Create `livingdocgen.json`:**
```json
{
  "outputPath": "Documentation/test-report.html"
}
```

**Result:**
```
YourTestProject/
├── Documentation/
│   └── test-report.html      ← Report generated here
├── Features/
└── livingdocgen.json
```

---

### Example 3: Features in a Different Folder

If your `.feature` files are not in a `Features` folder:

**Create `livingdocgen.json`:**
```json
{
  "featurePath": "Specifications"
}
```

**Or for a nested folder:**
```json
{
  "featurePath": "Tests/BDD/Scenarios"
}
```

---

### Example 4: Customizing the Report Appearance

**Create `livingdocgen.json`:**
```json
{
  "title": "My Awesome Project - Test Report",
  "theme": "dark",
  "outputPath": "docs/report.html"
}
```

**Available themes:**
- `purple` (default)
- `blue`
- `green`
- `dark`
- `light`
- `pickles`

---

## 🌍 System Requirements

**What you need:**
- .NET 6.0 or higher
- Reqnroll 3.3.2 or higher (see Version Compatibility section above)

**Works on:**
- Windows 10/11
- macOS (Intel and Apple Silicon)
- Linux (any distribution)

**Works with these IDEs:**
- Visual Studio 2022 or later
- Visual Studio Code (with C# extension)
- JetBrains Rider
- ReSharper (Visual Studio extension)
- Command line (`dotnet test`)

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


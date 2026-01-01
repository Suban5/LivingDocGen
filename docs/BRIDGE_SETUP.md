# Reqnroll Integration Bridge Setup

## Why Do I Need a Bridge File?

Reqnroll only discovers hooks (`[BeforeTestRun]`, `[AfterTestRun]`) from **your test assembly**, not from referenced NuGet packages. This is a Reqnroll architectural limitation, not a package issue.

The bridge file is a simple class in your test project that calls the package's bootstrap API.

## Bridge Pattern Diagram

```
Your Test Project                    NuGet Package
┌────────────────────┐              ┌──────────────────────┐
│ LivingDocGenBridge │──────────────│ LivingDocBootstrap   │
│  [Binding]         │   calls      │  (public API)        │
│  [BeforeScenario]  │──────────────│  BeforeTestRun()     │
│  [AfterTestRun]    │──────────────│  AfterTestRun()      │
└────────────────────┘              └──────────────────────┘
         ↑                                      ↓
         │                                      │
    Reqnroll discovers                  Generates documentation
```

## Complete Bridge File Code

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

## Configuration (Optional)

Create `livingdocgen.json` in your project root:

```json
{
  "featurePath": "Features",
  "testResultsPath": "TestResults",
  "outputPath": "living-documentation.html",
  "title": "My Project - Living Documentation",
  "theme": "blue",
  "includeTestResults": true
}
```

### Available Configuration Options

| Option | Default | Description |
|--------|---------|-------------|
| `featurePath` | `Features` | Path to feature files |
| `testResultsPath` | `TestResults` | Path to test results |
| `outputPath` | `living-documentation.html` | Output file name |
| `title` | `{ProjectName} - Living Documentation` | Document title |
| `theme` | `purple` | Theme: `purple`, `blue`, `green`, `dark`, `light`, `pickles` |
| `includeTestResults` | `true` | Include pass/fail indicators |

## Test Results Setup (NUnit)

Create `test.runsettings` in your test project:

```xml
<?xml version="1.0" encoding="utf-8"?>
<RunSettings>
  <RunConfiguration>
    <ResultsDirectory>TestResults</ResultsDirectory>
  </RunConfiguration>
  <NUnit>
    <OutputXmlFolderMode>UseResultDirectory</OutputXmlFolderMode>
    <TestOutputXml>nunit-results.xml</TestOutputXml>
    <NewOutputXmlFileForEachRun>true</NewOutputXmlFileForEachRun>
  </NUnit>
</RunSettings>
```

Run tests with:
```bash
dotnet test --settings test.runsettings
```

## Troubleshooting

### Documentation not generated?

✅ **Checklist:**
1. Package installed: `dotnet list package | grep LivingDocGen.Reqnroll.Integration`
2. Bridge file created in `Hooks/` folder
3. Bridge file uses correct namespace: `LivingDocGen.Reqnroll.Integration.Bootstrap`
4. Features folder exists with `.feature` files
5. Tests actually ran (not all skipped)

### Test results not included?

✅ **Solutions:**
1. Create `test.runsettings` file (see above)
2. Run with: `dotnet test --settings test.runsettings`
3. Verify `TestResults/` folder created with XML/TRX files
4. Check console output for "⚠️ No test results found" message

### VS Code Testing tab doesn't generate XML files?

✅ **Workaround:**
- VS Code's Testing UI has limited runsettings support
- Use integrated terminal: `dotnet test --settings test.runsettings`
- Or create VS Code task in `.vscode/tasks.json`:

```json
{
  "version": "2.0.0",
  "tasks": [{
    "label": "test-with-results",
    "command": "dotnet",
    "type": "process",
    "args": ["test", "--settings", "test.runsettings"],
    "group": { "kind": "test", "isDefault": true }
  }]
}
```

Then run via **Terminal > Run Task**.

## Supported Test Frameworks

The Reqnroll integration works with **all test frameworks**:
- ✅ NUnit 2, 3, 4
- ✅ xUnit
- ✅ MSTest

The package is test-framework agnostic.

## Example Projects

See the integration test projects for working examples:
- [IntegrationTest.Net7](../../tests/IntegrationTest.Net7/)
- [IntegrationTest.Net6](../../tests/IntegrationTest.Net6/)

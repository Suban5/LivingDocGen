# LivingDocGen.Reqnroll.Integration

[![NuGet](https://img.shields.io/badge/NuGet-v3.0.0-blue)](https://www.nuget.org/packages/LivingDocGen.Reqnroll.Integration/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://github.com/suban5/LivingDocGen/blob/main/LICENSE)

`LivingDocGen.Reqnroll.Integration` automatically generates living documentation for Reqnroll test projects. After `dotnet test` completes, it creates an interactive HTML report from your feature files and test results.

## Installation

### .NET CLI

```bash
dotnet add package LivingDocGen.Reqnroll.Integration
```

### Package Manager Console

```powershell
Install-Package LivingDocGen.Reqnroll.Integration
```

### PackageReference (optional)

```xml
<PackageReference Include="LivingDocGen.Reqnroll.Integration" Version="3.0.0" />
```

## Quick Start

1) Add a bridge file in your test project at `Hooks/LivingDocGenBridge.cs`:

```csharp
using Reqnroll;
using LivingDocGen.Reqnroll.Integration.Bootstrap;

namespace YourTestProject.Hooks
{
    [Binding]
    public class LivingDocGenBridge
    {
        [BeforeTestRun(Order = int.MinValue)]
        public static void BeforeAllTests() => LivingDocBootstrap.BeforeTestRun();

        [AfterTestRun(Order = int.MaxValue)]
        public static void AfterAllTests() => LivingDocBootstrap.AfterTestRun();
    }
}
```

2) Run tests:

```bash
dotnet test
```

The package generates `living-documentation.html` in the test project root.

## Features

- Automatic report generation after test runs
- Detached Worker process for reliable post-test generation
- Scalable chunked output (default) for large test suites
- Support for TRX, NUnit XML, xUnit XML, and SpecFlow JSON test results
- Interactive HTML report with search, status filtering, and themes
- Works with `dotnet test`, Visual Studio Test Explorer, and CI pipelines

## Configuration

Configuration is optional. If needed, add `livingdocgen.json` next to your `.csproj`:

```json
{
  "featurePath": "Features",
  "testResultsPath": "TestResults",
  "outputPath": "living-documentation.html",
  "title": "My Project - Living Documentation",
  "theme": "blue"
}
```

Essential options:
- `featurePath`: feature directory path
- `testResultsPath`: test result directory path
- `outputPath`: output file path
- `title`: report title
- `theme`: `purple`, `blue`, `green`, `dark`, `light`, `pickles`

Optional test format override:

```json
{
  "testResults": {
    "format": "nunit"
  }
}
```

Supported `format` values: `trx`, `nunit`, `xunit`, `junit`, `specflow`, `all`.

## Compatibility

- .NET: 8.0+
- Reqnroll: 3.3.2+
- Gherkin: 35.0.0
- Platforms: Windows, macOS, Linux

Supported frameworks:
- NUnit 2/3: `.xml`
- NUnit 4 / MSTest: `.trx`
- xUnit: `.xml`
- SpecFlow: `.json`

## Documentation

- Full documentation: https://github.com/suban5/LivingDocGen
- Bridge setup guide: https://github.com/suban5/LivingDocGen/blob/main/docs/BRIDGE_SETUP.md
- Package changelog: https://github.com/suban5/LivingDocGen/blob/main/src/LivingDocGen.Reqnroll.Integration/CHANGELOG.md

## License

MIT: https://github.com/suban5/LivingDocGen/blob/main/LICENSE

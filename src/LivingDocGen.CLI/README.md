# LivingDocGen CLI

[![NuGet](https://img.shields.io/nuget/v/LivingDocGen.Tool.svg)](https://www.nuget.org/packages/LivingDocGen.Tool/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://github.com/suban5/LivingDocGen/blob/main/LICENSE)

`LivingDocGen` is a cross-platform .NET tool that generates interactive living documentation from Gherkin feature files and test results. It correlates scenarios with execution outcomes and produces an HTML report with search, filtering, and theme support.

## Installation

### .NET CLI

```bash
dotnet tool install --global LivingDocGen.Tool
```

### Package Manager Console

```powershell
Install-Package LivingDocGen.Tool
```

### PackageReference (optional)

```xml
<PackageReference Include="LivingDocGen.Tool" Version="3.0.0" />
```

## Quick Start

```bash
# Generate HTML report from features + test results
LivingDocGen generate ./Features ./TestResults -o living-documentation.html
```

The command creates an HTML report you can open directly in a browser.

## Features

- Generate interactive HTML living documentation from `.feature` files and test results
- Use scalable `chunked` output mode (default) for very large suites
- Use `legacy` output mode for single-file HTML output
- Parse TRX, NUnit XML, xUnit XML, and SpecFlow JSON test result formats
- Apply built-in themes (`purple`, `blue`, `green`, `dark`, `light`, `pickles`)
- Use `generate`, `parse`, and `test-results` commands

## Configuration

Use `livingdocgen.json` in the working directory:

```json
{
  "featuresPath": "./Features",
  "testResultsPath": "./TestResults",
  "output": "./living-documentation.html",
  "title": "Project Living Documentation",
  "theme": "blue"
}
```

Essential options:
- `featuresPath`: path to feature files
- `testResultsPath`: path to test results
- `output`: output HTML file path
- `title`: report title
- `theme`: report theme

## Compatibility

- .NET runtime: 6.0+
- Platforms: Windows, macOS, Linux

Supported result formats:
- NUnit 2/3: `.xml`
- NUnit 4 / MSTest: `.trx`
- xUnit: `.xml`
- SpecFlow: `.json`

## Documentation

- Full documentation: https://github.com/suban5/LivingDocGen
- CLI changelog: https://github.com/suban5/LivingDocGen/blob/main/src/LivingDocGen.CLI/CHANGELOG.md

## License

MIT: https://github.com/suban5/LivingDocGen/blob/main/LICENSE

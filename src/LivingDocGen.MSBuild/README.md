# LivingDocGen.MSBuild

**LivingDocGen.MSBuild** is the MSBuild integration layer for LivingDocGen. It is distributed as the NuGet package **`LivingDocGen.MSBuild`**.

## 🎯 Purpose

This project provides the automatic integration with the .NET build and test process. When installed in a test project, it ensures that living documentation is generated automatically whenever tests are run.

## 📦 NuGet Package

*   **Package ID**: `LivingDocGen.MSBuild`
*   **Description**: Automatic living documentation generator for BDD test projects.

## ⚙️ How It Works

1.  **Hooks into Build/Test**: The package includes MSBuild `.targets` files (`LivingDocGen.targets`) that inject tasks into the build pipeline.
2.  **Executes CLI**: It bundles the `LivingDocGen.CLI` tool and executes it against the project's output.
3.  **Zero Configuration**: By default, it scans the project output for feature files and test results and generates `living-documentation.html`.

## 🔧 Configuration

You can configure the behavior using MSBuild properties in your `.csproj` file or by using a `livingdocgen.json` file in your project root.

### Option 1: Configuration File (Recommended)

Create a `livingdocgen.json` file in your project root:

```json
{
  "enabled": true,
  "autoGenerate": "AfterTest",
  "paths": {
    "features": "./Features",
    "testResults": "./TestResults",
    "output": "./living-documentation.html"
  },
  "documentation": {
    "title": "My Project - Living Documentation",
    "theme": "purple",
    "primaryColor": "#9333ea"
  },
  "advanced": {
    "verbose": false,
    "includeSkipped": true,
    "includePending": true
  }
}
```

The MSBuild integration will automatically detect and use this file if it exists.

### Option 2: MSBuild Properties

If no `livingdocgen.json` file is found, you can configure using MSBuild properties in your `.csproj`:

```xml
<PropertyGroup>
  <!-- Path to config file (optional, defaults to livingdocgen.json) -->
  <LivingDocConfigFile>$(ProjectDir)custom-config.json</LivingDocConfigFile>
  
  <!-- Enable/Disable generation (default: true) -->
  <LivingDocEnabled>true</LivingDocEnabled>
  
  <!-- Custom output path -->
  <LivingDocOutput>$(OutputPath)docs/index.html</LivingDocOutput>
  
  <!-- Documentation Title -->
  <LivingDocTitle>My Project Specs</LivingDocTitle>
  
  <!-- Theme (purple, blue, green, dark, light, pickles) -->
  <LivingDocTheme>blue</LivingDocTheme>
</PropertyGroup>
```

**Priority Order:**
1. If `LivingDocConfigFile` exists → use config file
2. If `livingdocgen.json` exists in project root → use it
3. Otherwise → use MSBuild properties

## 🏗 Development

This project is responsible for:
1.  Packaging the `LivingDocGen.CLI` binaries into the `tools/` folder of the NuGet package.
2.  Defining the MSBuild targets (`LivingDocGen.targets`) that run the tool.

### Build Process
The `.csproj` contains a custom target `CopyBDDCliToTools` that copies the CLI build artifacts before packing.

```xml
<Target Name="CopyBDDCliToTools" BeforeTargets="_GetPackageFiles">
    <!-- Copies CLI binaries to tools/ folder -->
</Target>
```

## 📝 Todo List

- [ ] Add conditional execution based on test run success/failure.
- [ ] Improve error reporting when CLI execution fails.

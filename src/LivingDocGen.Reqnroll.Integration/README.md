# LivingDocGen.Reqnroll.Integration

**LivingDocGen.Reqnroll.Integration** is a specialized library that provides seamless integration with the [Reqnroll](https://reqnroll.net/) BDD framework.

## 🎯 Purpose

This library allows Reqnroll test projects to automatically trigger the generation of living documentation immediately after the test run completes. It uses Reqnroll's `[AfterTestRun]` hook to execute the generation process without requiring any external build scripts or CI/CD configuration.

## 📦 NuGet Package

*   **Package ID**: `LivingDocGen.Reqnroll.Integration`
*   **Description**: Reqnroll integration for automatic living documentation generation.

## ⚙️ How It Works

1.  **Hooks**: The library contains a `[Binding]` class with an `[AfterTestRun]` hook.
2.  **Execution**: When the test suite finishes, the hook triggers.
3.  **Path Detection**: It automatically detects the project root, `Features` folder, and `TestResults` folder.
4.  **Generation**: It invokes the `LivingDocGen.CLI` logic to generate the HTML report.

## 💻 Usage

Simply add the NuGet package to your Reqnroll test project. No additional code is required.

```bash
dotnet add package LivingDocGen.Reqnroll.Integration
```

The documentation will be generated at `living-documentation.html` in your project root after `dotnet test` completes.

## 🔧 Configuration

Currently, the integration uses sensible defaults:
*   **Features Path**: `./Features`
*   **Test Results Path**: `./TestResults`
*   **Output**: `./living-documentation.html`
*   **Theme**: `purple`

*Note: Future versions will support reading configuration from `reqnroll.json` or `livingdocgen.json`.*

## ⚠️ Requirements

*   **Reqnroll**: Version 2.0.0 or later.
*   **Project Structure**: Assumes a standard folder structure with `Features/` at the project root.

## 📝 Todo List

- [ ] Add support for reading configuration from `reqnroll.json` or `livingdocgen.json`.
- [ ] Allow customization of paths and themes via environment variables.
- [ ] Add integration tests with sample Reqnroll projects.

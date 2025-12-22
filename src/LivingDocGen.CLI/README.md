# LivingDocGen CLI

The **LivingDocGen CLI** is a cross-platform .NET Global Tool that generates beautiful, interactive living documentation from your Gherkin feature files and test results.

## 📦 Installation

### As a Global Tool
Install the tool globally to use it from any directory:

```bash
dotnet tool install --global LivingDocGen.Tool
```

### As a Local Tool
Install it locally in your project manifest:

```bash
dotnet new tool-manifest # if you haven't created one yet
dotnet tool install LivingDocGen.Tool
```

## 🚀 Usage

The main command is `generate`, which produces the HTML documentation.

### Basic Generation
```bash
LivingDocGen generate ./Features ./TestResults
```

### Specify Output File
```bash
LivingDocGen generate ./Features ./TestResults -o ./docs/index.html
```

### Customize Title and Theme
```bash
LivingDocGen generate ./Features ./TestResults --title "My Project Docs" --theme dark
```

## 🛠 Commands

### `generate`
Generates the HTML living documentation.

**Arguments:**
- `features`: Path to feature files or directory (optional if using config file).
- `test-results`: Path to test results or directory (optional).

**Options:**
- `-o, --output <path>`: Output HTML file path (default: `living-documentation.html`).
- `-t, --title <text>`: Documentation title.
- `-th, --theme <name>`: Theme name (`purple`, `blue`, `green`, `dark`, `light`, `pickles`).
- `-c, --color <hex>`: Primary color (e.g., `#FF0000`).
- `--config <path>`: Path to `livingdocgen.json` configuration file.
- `-v, --verbose`: Show detailed output.

### `parse`
Parses feature files and outputs the structure as JSON. Useful for debugging or custom integrations.

```bash
LivingDocGen parse ./Features -o features.json
```

**Options:**
- `-o, --output <path>`: Output JSON file path.
- `-f, --framework <name>`: Force specific framework parser (`cucumber`, `specflow`, `reqnroll`, `jbehave`).
- `-v, --verbose`: Show detailed output.

### `test-results`
Parses test result files (Trx, NUnit, xUnit, etc.) and outputs the standardized execution data as JSON.

```bash
LivingDocGen test-results ./TestResults -o results.json
```

**Options:**
- `-o, --output <path>`: Output JSON file path.
- `-v, --verbose`: Show detailed output.

## ⚙️ Configuration

You can use a `livingdocgen.json` file instead of passing CLI arguments. The tool automatically looks for this file in the current directory.

**Example `livingdocgen.json`:**
```json
{
  "enabled": true,
  "featuresPath": "./Features",
  "testResultsPath": "./TestResults",
  "output": "./docs/documentation.html",
  "title": "My Awesome Project",
  "theme": "dark",
  "primaryColor": "#3b82f6"
}
```

## 🏗 Development

To build and run the CLI locally from source:

```bash
# Navigate to the CLI project
cd src/LivingDocGen.CLI

# Run directly
dotnet run -- generate ../../samples/features ../../samples/test-results -o ../../docs/dev-output.html
```

## 📝 Todo List

- [ ] Add interactive mode for guided documentation generation.
- [ ] Support for custom template engines (beyond embedded HTML).
- [ ] Add `--watch` mode to regenerate docs on file changes.

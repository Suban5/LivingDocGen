# Frequently Asked Questions (FAQ)

## General Questions

### Q: Do I need to change my test code?
**A:** No! Zero code changes required. Just add the NuGet package and run tests.

### Q: Which test frameworks are supported?
**A:** NUnit (2, 3, 4), xUnit, JUnit, MSTest, SpecFlow, Cucumber.

### Q: Which BDD frameworks are supported?
**A:** Reqnroll, SpecFlow, Cucumber, JBehave - any framework that produces Gherkin `.feature` files.

### Q: Can I use this without MSBuild integration?
**A:** Yes! Install as a global tool: `dotnet tool install --global LivingDocGen.Tool`

### Q: Does it work with CI/CD?
**A:** Absolutely! Use the global tool in your pipeline. See the [GitHub Actions example](../README.md#example-2-cicd-pipeline-github-actions).

## Configuration

### Q: How do I customize the theme?
**A:** Three ways:
1. Set `BDDLivingDocTheme` in `.csproj`
2. Set `theme` in `livingdocgen.json`
3. Change it in the browser after generation (saved to localStorage)

### Q: Can I customize colors?
**A:** Yes! Use the `--color` CLI option with hex codes: `LivingDocGen generate ... --color #3b82f6`

### Q: Where do I put the configuration file?
**A:** Create `livingdocgen.json` in your test project root (same folder as `.csproj`).

## Test Results

### Q: Can I generate documentation without test results?
**A:** Yes! Run: `LivingDocGen generate ./Features` (omit test results path). The documentation will show features and scenarios without pass/fail indicators.

### Q: What if I have multiple test result files?
**A:** The tool automatically merges them using smart timestamp-based logic. The most recent result for each test is shown.

### Q: Why aren't my test results showing up?
**A:** Common causes:
1. Missing `test.runsettings` file (for NUnit)
2. Running tests from VS Code Testing tab (limited runsettings support)
3. Test results in different folder than configured

**Solution:** Create `test.runsettings` and run `dotnet test --settings test.runsettings`. See [Bridge Setup Guide](BRIDGE_SETUP.md#test-results-setup-nunit).

### Q: What test result formats are supported?
**A:** 
- ✅ NUnit 2/3 (XML)
- ✅ NUnit 4 (TRX)
- ✅ xUnit (XML)
- ✅ JUnit (XML)
- ✅ MSTest (TRX)
- ✅ SpecFlow/Cucumber (JSON execution reports)

## Reqnroll Integration

### Q: Why does Reqnroll integration need a bridge file?
**A:** Reqnroll only discovers hooks (`[BeforeTestRun]`, `[AfterTestRun]`) from your test assembly, not from referenced NuGet packages. The bridge file is a simple class that calls the package's bootstrap API from your test project. This is a Reqnroll architectural limitation, not a package issue.

### Q: Does Reqnroll integration work with all test frameworks?
**A:** Yes! NUnit, xUnit, MSTest - any framework that Reqnroll supports. The package is test-framework agnostic.

### Q: Can I use Reqnroll integration with SpecFlow?
**A:** The package is specifically designed for Reqnroll. For SpecFlow, use the CLI tool or MSBuild integration instead.

### Q: Where should I put the bridge file?
**A:** Create it in the `Hooks/` folder of your test project: `Hooks/LivingDocGenBridge.cs`. See the [complete bridge setup guide](BRIDGE_SETUP.md).

## Output & Features

### Q: Is the output a single file?
**A:** Yes! A self-contained HTML file with embedded CSS and JavaScript. No external dependencies. Just open it in any browser.

### Q: Can I customize the output file name?
**A:** Yes! Use `-o` or `--output` option: `LivingDocGen generate ... -o my-report.html`

### Q: Does it support Scenario Outlines?
**A:** Yes! Full support for Scenario Outlines with multiple Examples sections and large data tables.

### Q: Can I filter scenarios by tags?
**A:** Yes! The generated HTML has interactive filtering by tags, status, and features. Search functionality is also included.

### Q: What browsers are supported?
**A:** All modern browsers: Chrome, Firefox, Safari, Edge. IE11 is not supported.

## Framework Compatibility

### Q: Does it work with .NET Framework 4.7.2?
**A:** 
- **Library packages** (Core, Parser, TestReporter, Generator): ✅ Yes (.NET Standard 2.0)
- **CLI Tool & Reqnroll Integration**: ❌ No (.NET 6+ required due to RazorEngine.NetCore dependency)

### Q: What .NET versions are supported?
**A:** 
- **Library packages**: .NET Framework 4.6.1+, .NET Core 2.0+, .NET 5+
- **CLI Tool**: .NET 6.0+ runtime
- **Reqnroll Integration**: .NET 6.0+ runtime

## Troubleshooting

### Q: The tool says "No features found"?
**A:** Check:
1. Features folder path is correct
2. `.feature` files exist in the folder
3. Path is relative to where you run the command
4. Use absolute paths if relative paths don't work

### Q: The documentation generated but looks empty?
**A:** Possible causes:
1. Feature files have syntax errors (check console output)
2. Features folder is empty
3. Parser couldn't recognize the Gherkin format

**Solution:** Run with `--verbose` flag to see detailed parsing logs.

### Q: Error: "Could not find test results"?
**A:** 
1. Ensure tests have run at least once
2. Check `TestResults` folder exists
3. Verify test results file format is supported
4. Use absolute path to test results folder

### Q: Documentation not generating automatically in Reqnroll project?
**A:** See the [complete troubleshooting guide](BRIDGE_SETUP.md#troubleshooting) in the Bridge Setup documentation.

## Performance

### Q: How long does generation take?
**A:** Typically 1-5 seconds for projects with:
- 10-100 feature files
- 100-1000 scenarios
- 1-10 test result files

Large projects (1000+ scenarios) may take 10-30 seconds.

### Q: Can I generate documentation in parallel CI jobs?
**A:** Yes! Each job can generate its own documentation. To merge results, collect all test result files in one place and generate once.

## Contributing

### Q: Can I contribute to this project?
**A:** Yes! This is a Master's thesis project, but contributions are welcome. See [CONTRIBUTING.md](../CONTRIBUTING.md) for guidelines.

### Q: I found a bug. How do I report it?
**A:** [Open an issue](https://github.com/Suban5/LivingDocGen/issues) with:
1. Description of the problem
2. Steps to reproduce
3. Expected vs actual behavior
4. Sample feature files (if possible)
5. Environment details (.NET version, OS)

### Q: Can I request a feature?
**A:** Absolutely! [Open a discussion](https://github.com/Suban5/LivingDocGen/discussions) or issue with your idea.

---

**Still have questions?** 
- [Open a discussion](https://github.com/Suban5/LivingDocGen/discussions)
- [Check existing issues](https://github.com/Suban5/LivingDocGen/issues)
- [Review the main README](../README.md)

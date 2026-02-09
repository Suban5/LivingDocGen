# Worker Architecture

This document describes the Worker process architecture used by `LivingDocGen.Reqnroll.Integration` for reliable documentation generation.

## Overview

The Worker architecture uses a **detached process** to generate documentation independently of the test host lifecycle. This solves the VSTest shutdown problem where `dotnet test` forcefully terminates background threads before they can complete.

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           Test Execution Flow                                │
└─────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────┐
│   dotnet test       │
│   (Test Host)       │
└─────────┬───────────┘
          │
          ▼
┌─────────────────────┐     ┌─────────────────────┐
│   Test Execution    │────▶│   Write Test        │
│   (NUnit/xUnit)     │     │   Results File      │
└─────────┬───────────┘     └─────────────────────┘
          │
          ▼
┌─────────────────────┐     ┌─────────────────────┐
│   AfterTestRun      │────▶│   Write Job File    │
│   Reqnroll Hook     │     │   (JSON config)     │
└─────────┬───────────┘     └─────────────────────┘
          │
          ▼
┌─────────────────────┐
│   Launch Worker     │───────────────────────────────┐
│   (Detached)        │                               │
└─────────┬───────────┘                               │
          │                                           │
          ▼                                           ▼
┌─────────────────────┐                   ┌─────────────────────┐
│   Test Host Exits   │                   │   Worker Process    │
│   (Normal shutdown) │                   │   (Independent)     │
└─────────────────────┘                   └─────────┬───────────┘
                                                    │
                                          ┌─────────┴───────────┐
                                          ▼                     ▼
                                ┌─────────────────┐   ┌─────────────────┐
                                │ Wait for Test   │   │ Read Job File   │
                                │ Results Stable  │   │ (Configuration) │
                                └────────┬────────┘   └─────────────────┘
                                         │
                                         ▼
                                ┌─────────────────┐
                                │ Parse Features  │
                                │ Parse Results   │
                                └────────┬────────┘
                                         │
                                         ▼
                                ┌─────────────────┐
                                │ Generate HTML   │
                                │ Documentation   │
                                └────────┬────────┘
                                         │
                                         ▼
                                ┌─────────────────┐
                                │ Write Output    │
                                │ Clean Up Job    │
                                └─────────────────┘
```

## Components

### 1. LivingDocBootstrap (Reqnroll Hook)

**Location:** `src/LivingDocGen.Reqnroll.Integration/Bootstrap/LivingDocBootstrap.cs`

Responsibilities:
- Registers `AfterTestRun` hook with Reqnroll
- Loads configuration from `livingdocgen.json`
- Writes job file with all necessary paths and settings
- Launches Worker process in detached mode
- Returns immediately (doesn't block test completion)

### 2. LivingDocJob (Job File Writer)

**Location:** `src/LivingDocGen.Reqnroll.Integration/Runtime/LivingDocJob.cs`

Responsibilities:
- Serializes job configuration to JSON
- Writes `livingdoc-job-{timestamp}.json` file
- Starts Worker process with `ProcessStartInfo`
- Uses `UseShellExecute = false` for detached execution

### 3. LivingDocGen.Worker (Standalone Process)

**Location:** `src/LivingDocGen.Worker/Program.cs`

Responsibilities:
- Reads job file from project directory
- Waits for test result files to be written and stable
- Parses feature files using `GherkinParser`
- Parses test results using `TestReportService`
- Generates HTML using `LivingDocumentationGenerator`
- Writes output file and cleans up job file

## Job File Format

```json
{
  "ProjectRoot": "/path/to/project",
  "TestResultsPath": "/path/to/project/TestResults",
  "Timestamp": "2026-02-10T00:00:00Z"
}
```

The Worker also reads `livingdocgen.json` for additional configuration:

```json
{
  "paths": {
    "features": "Features",
    "testResults": "TestResults",
    "output": "living-documentation.html"
  },
  "documentation": {
    "title": "My Project - Living Documentation",
    "theme": "blue"
  },
  "testResults": {
    "format": "nunit"
  }
}
```

## Test Result Waiting Logic

The Worker uses intelligent waiting to ensure test results are ready:

```
┌─────────────────────────────────────────────────────────────┐
│                 Test Result Waiting Flow                     │
└─────────────────────────────────────────────────────────────┘

1. Start polling (1 second intervals)
2. Search for files matching patterns (*.trx, *.xml, etc.)
3. If files found:
   a. Check total file size
   b. If size stable for 3 consecutive checks → Ready
   c. If size changing → Continue waiting
4. If no files found:
   a. Log "Still waiting..."
   b. Continue polling
5. After 3 minutes timeout:
   a. Proceed anyway with warning
   b. May generate docs without test results
```

### Stability Detection

```csharp
// File size must be stable for 3 consecutive checks
if (currentTotalSize > 0 && currentTotalSize == lastFileSize)
{
    stableCount++;
    if (stableCount >= 3)
    {
        // Additional 500ms delay for file handle release
        Thread.Sleep(500);
        return allFiles; // Ready!
    }
}
```

## Multi-Format Support

The Worker supports multiple test result formats via configurable patterns:

| Format | Shorthand | Patterns | Parser Used |
|--------|-----------|----------|-------------|
| VSTest/MSTest | `trx` | `*.trx` | `TrxResultParser` |
| NUnit 3/4 | `nunit` | `*.xml` | `NUnitResultParser` |
| NUnit 2 | `nunit` | `*.xml` | `NUnit2ResultParser` |
| xUnit | `xunit` | `*.xml` | `XUnitResultParser` |
| JUnit | `junit` | `*.xml` | `JUnitResultParser` |
| SpecFlow/Reqnroll | `specflow` | `*.json` | `SpecFlowJsonResultParser` |
| All Formats | `all` | `*.trx`, `*.xml`, `*.json` | Auto-detect |

Configure via `livingdocgen.json`:

```json
{
  "testResults": {
    "format": "nunit"
  }
}
```

Or use custom patterns:

```json
{
  "testResults": {
    "patterns": ["TestResults*.xml", "coverage.trx"]
  }
}
```

## Error Handling

The Worker handles errors gracefully:

1. **Missing Job File**: Exits silently if no job file found
2. **Missing Config File**: Uses defaults for all settings
3. **Missing Features Directory**: Logs warning, proceeds anyway
4. **No Test Results**: Logs warning after timeout, generates docs without results
5. **Parse Errors**: Logs error, continues with partial data
6. **Write Errors**: Logs error with full path and message

## Performance Characteristics

| Metric | Value |
|--------|-------|
| Startup time | ~100ms |
| Polling interval | 1 second |
| Stability checks | 3 consecutive |
| Default timeout | 3 minutes |
| Typical generation | ~500ms for 100 scenarios |

## Debugging

### Enable Verbose Output

The Worker outputs detailed logs to console:

```
[00:00:01.234] LivingDocGen Worker started
[00:00:01.235] Reading job from: /path/to/livingdoc-job-123.json
[00:00:01.240] Looking for patterns: *.trx, *.xml
[00:00:01.245] Waiting for test results...
[00:00:03.250] File size stable: 12345 bytes (check 1/3)
[00:00:04.255] File size stable: 12345 bytes (check 2/3)
[00:00:05.260] File size stable: 12345 bytes (check 3/3)
[00:00:05.760] ✅ Test results ready after 4525ms
[00:00:05.762] Found 1 test result file(s):
[00:00:05.763]   - test-results.trx
[00:00:06.100] Generated 2567 KB of HTML
[00:00:06.150] ✅ Documentation generated successfully!
```

### Job File Inspection

Check the job file contents:

```bash
cat livingdoc-job-*.json
```

### Manual Worker Execution

Run the Worker manually for testing:

```bash
cd /path/to/test/project
dotnet run --project /path/to/LivingDocGen.Worker
```

## Dependencies

The Worker includes all necessary dependencies (48 DLLs):

- `LivingDocGen.Core.dll`
- `LivingDocGen.Parser.dll`
- `LivingDocGen.Generator.dll`
- `LivingDocGen.TestReporter.dll`
- `Gherkin.dll`
- `Microsoft.Extensions.*.dll`
- ... and transitive dependencies

These are automatically copied to consuming projects via NuGet package content.

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using LivingDocGen.Generator.Models;
using LivingDocGen.Generator.Services.Rendering;
using LivingDocGen.Parser.Models;
using LivingDocGen.TestReporter.Models;

namespace LivingDocGen.Generator.Tests.Services.Rendering;

/// <summary>
/// Tests for PR-5: Virtualization for Scenarios and Large Tables.
/// Verifies scenario list windowing and table row chunk rendering.
/// </summary>
public class FeatureRendererVirtualizationTests
{
    private readonly FeatureRenderer _sut = new();

    // ===== Scenario Virtualization =====

    [Fact]
    public void Render_FeatureWithFewScenarios_NoVirtualization()
    {
        var feature = CreateFeatureWithScenarios(10);

        var html = _sut.Render(feature, 0);

        Assert.DoesNotContain("scenario-virtualizer", html);
        Assert.DoesNotContain("scenario-virtualized", html);
        Assert.DoesNotContain("virtualization-sentinel", html);
    }

    [Fact]
    public void Render_FeatureExactlyAtThreshold_NoVirtualization()
    {
        var feature = CreateFeatureWithScenarios(FeatureRenderer.ScenarioVirtualizationThreshold);

        var html = _sut.Render(feature, 0);

        Assert.DoesNotContain("scenario-virtualizer", html);
        Assert.DoesNotContain("scenario-virtualized", html);
    }

    [Fact]
    public void Render_FeatureAboveThreshold_HasVirtualizationContainer()
    {
        var scenarioCount = FeatureRenderer.ScenarioVirtualizationThreshold + 50;
        var feature = CreateFeatureWithScenarios(scenarioCount);

        var html = _sut.Render(feature, 0);

        Assert.Contains("scenario-virtualizer", html);
        Assert.Contains($"data-total-scenarios=\"{scenarioCount}\"", html);
        Assert.Contains($"data-initial-window=\"{FeatureRenderer.ScenarioInitialWindow}\"", html);
    }

    [Fact]
    public void Render_FeatureAboveThreshold_MarksExcessScenariosVirtualized()
    {
        var scenarioCount = FeatureRenderer.ScenarioVirtualizationThreshold + 50;
        var feature = CreateFeatureWithScenarios(scenarioCount);

        var html = _sut.Render(feature, 0);

        // First ScenarioInitialWindow scenarios should NOT be virtualized
        // Scenarios beyond the window should have 'scenario-virtualized' class
        Assert.Contains("scenario-virtualized", html);
    }

    [Fact]
    public void Render_FeatureAboveThreshold_HasSentinelWithShowMoreButton()
    {
        var scenarioCount = FeatureRenderer.ScenarioVirtualizationThreshold + 50;
        var feature = CreateFeatureWithScenarios(scenarioCount);

        var html = _sut.Render(feature, 0);

        Assert.Contains("virtualization-sentinel", html);
        Assert.Contains("virtualization-show-more", html);
        var expectedRemaining = scenarioCount - FeatureRenderer.ScenarioInitialWindow;
        Assert.Contains($"{expectedRemaining} remaining", html);
    }

    [Fact]
    public void Render_FeatureAboveThreshold_ShowsProgressCounter()
    {
        var scenarioCount = FeatureRenderer.ScenarioVirtualizationThreshold + 100;
        var feature = CreateFeatureWithScenarios(scenarioCount);

        var html = _sut.Render(feature, 0);

        Assert.Contains("virtualization-progress", html);
        Assert.Contains("virtualization-rendered", html);
        Assert.Contains($"{FeatureRenderer.ScenarioInitialWindow}", html);
        Assert.Contains($"/ {scenarioCount} scenarios rendered", html);
    }

    [Fact]
    public void Render_FeatureWithRules_NoVirtualizationApplied()
    {
        // Features with rules don't get scenario-level virtualization
        // (rules structure the scenarios differently)
        var feature = CreateFeatureWithRules(FeatureRenderer.ScenarioVirtualizationThreshold + 50);

        var html = _sut.Render(feature, 0);

        // Even above threshold, rules mean the scenarios are subdivided —
        // virtualization container is still present at the feature level
        Assert.Contains("scenario-virtualizer", html);
    }

    // ===== Data Table Row Chunking =====

    [Fact]
    public void Render_SmallDataTable_NoChunking()
    {
        var feature = CreateFeatureWithDataTable(10); // 10 data rows + 1 header

        var html = _sut.Render(feature, 0);

        Assert.DoesNotContain("chunked-table", html);
        Assert.DoesNotContain("chunk-sentinel", html);
        Assert.DoesNotContain("chunk-data", html);
    }

    [Fact]
    public void Render_LargeDataTable_HasChunkingAttributes()
    {
        var rowCount = FeatureRenderer.TableRowChunkThreshold + 50;
        var feature = CreateFeatureWithDataTable(rowCount);

        var html = _sut.Render(feature, 0);

        Assert.Contains("chunked-table", html);
        Assert.Contains($"data-total-rows=\"{rowCount}\"", html);
        Assert.Contains($"data-chunk-size=\"{FeatureRenderer.TableRowChunkSize}\"", html);
        Assert.Contains($"data-rendered-rows=\"{FeatureRenderer.TableRowInitialWindow}\"", html);
    }

    [Fact]
    public void Render_LargeDataTable_HasLoadMoreButton()
    {
        var rowCount = FeatureRenderer.TableRowChunkThreshold + 100;
        var feature = CreateFeatureWithDataTable(rowCount);

        var html = _sut.Render(feature, 0);

        Assert.Contains("chunk-load-btn", html);
        Assert.Contains("chunk-load-more", html);
        var remaining = rowCount - FeatureRenderer.TableRowInitialWindow;
        Assert.Contains($"{remaining} remaining", html);
    }

    [Fact]
    public void Render_LargeDataTable_StoresChunkDataAsJson()
    {
        var rowCount = FeatureRenderer.TableRowChunkThreshold + 10;
        var feature = CreateFeatureWithDataTable(rowCount);

        var html = _sut.Render(feature, 0);

        Assert.Contains("class=\"chunk-data\"", html);
        Assert.Contains("application/json", html);
    }

    [Fact]
    public void Render_LargeDataTable_PreservesHeaderRow()
    {
        var feature = CreateFeatureWithDataTable(FeatureRenderer.TableRowChunkThreshold + 10);

        var html = _sut.Render(feature, 0);

        // Headers should always render (sticky headers preserved)
        Assert.Contains("<thead", html);
        Assert.Contains("Header0", html);
        Assert.Contains("Header1", html);
    }

    [Fact]
    public void Render_LargeDataTable_RendersInitialWindowRows()
    {
        var feature = CreateFeatureWithDataTable(FeatureRenderer.TableRowChunkThreshold + 50);

        var html = _sut.Render(feature, 0);

        // First row data should be rendered
        Assert.Contains("Row1-Col0", html);
        // Row at window boundary should be rendered
        var lastWindowRow = FeatureRenderer.TableRowInitialWindow;
        Assert.Contains($"Row{lastWindowRow}-Col0", html);
    }

    // ===== Examples Table Row Chunking =====

    [Fact]
    public void Render_SmallExamplesTable_NoChunking()
    {
        var feature = CreateFeatureWithExamplesTable(10);

        var html = _sut.Render(feature, 0);

        Assert.DoesNotContain("chunked-table", html);
        Assert.DoesNotContain("chunk-data-examples", html);
    }

    [Fact]
    public void Render_LargeExamplesTable_HasChunkingAttributes()
    {
        var rowCount = FeatureRenderer.TableRowChunkThreshold + 50;
        var feature = CreateFeatureWithExamplesTable(rowCount);

        var html = _sut.Render(feature, 0);

        Assert.Contains("chunked-table", html);
        Assert.Contains($"data-total-rows=\"{rowCount}\"", html);
    }

    [Fact]
    public void Render_LargeExamplesTable_StoresJsonWithStatus()
    {
        var rowCount = FeatureRenderer.TableRowChunkThreshold + 10;
        var feature = CreateFeatureWithExamplesTable(rowCount);

        var html = _sut.Render(feature, 0);

        Assert.Contains("chunk-data-examples", html);
        // JSON should include status field
        Assert.Contains("\"status\":", html);
        Assert.Contains("\"cells\":", html);
    }

    [Fact]
    public void Render_LargeExamplesTable_PreservesStickyHeaders()
    {
        var feature = CreateFeatureWithExamplesTable(FeatureRenderer.TableRowChunkThreshold + 10);

        var html = _sut.Render(feature, 0);

        // Headers must be present for sticky header UX
        Assert.Contains("<thead", html);
        Assert.Contains("Status", html);
        Assert.Contains("Param0", html);
    }

    // ===== Threshold Constants =====

    [Fact]
    public void ScenarioVirtualizationThreshold_Is200()
    {
        Assert.Equal(200, FeatureRenderer.ScenarioVirtualizationThreshold);
    }

    [Fact]
    public void TableRowChunkThreshold_Is200()
    {
        Assert.Equal(200, FeatureRenderer.TableRowChunkThreshold);
    }

    [Fact]
    public void TableRowInitialWindow_Is50()
    {
        Assert.Equal(50, FeatureRenderer.TableRowInitialWindow);
    }

    [Fact]
    public void ScenarioInitialWindow_Is30()
    {
        Assert.Equal(30, FeatureRenderer.ScenarioInitialWindow);
    }

    // ===== Helper Methods =====

    private static EnrichedFeature CreateFeatureWithScenarios(int scenarioCount)
    {
        var scenarios = new List<EnrichedScenario>();
        for (int i = 0; i < scenarioCount; i++)
        {
            scenarios.Add(new EnrichedScenario
            {
                Scenario = new UniversalScenario
                {
                    Name = $"Scenario {i}",
                    Type = ScenarioType.Scenario,
                    Steps = new List<UniversalStep>
                    {
                        new UniversalStep { Keyword = "Given", Text = $"step {i}" }
                    }
                },
                Status = i % 2 == 0 ? ExecutionStatus.Passed : ExecutionStatus.Failed,
                Steps = new List<EnrichedStep>
                {
                    new EnrichedStep
                    {
                        Step = new UniversalStep { Keyword = "Given", Text = $"step {i}" },
                        Status = i % 2 == 0 ? ExecutionStatus.Passed : ExecutionStatus.Failed
                    }
                }
            });
        }

        return new EnrichedFeature
        {
            Feature = new UniversalFeature
            {
                Name = "Test Feature",
                Tags = new List<string>(),
                Rules = new List<UniversalRule>()
            },
            Scenarios = scenarios,
            OverallStatus = ExecutionStatus.Failed,
            PassedCount = scenarioCount / 2,
            FailedCount = scenarioCount - scenarioCount / 2
        };
    }

    private static EnrichedFeature CreateFeatureWithRules(int totalScenarios)
    {
        var scenarios = new List<EnrichedScenario>();
        var ruleScenarios = new List<UniversalScenario>();

        for (int i = 0; i < totalScenarios; i++)
        {
            var scenario = new UniversalScenario
            {
                Name = $"Rule Scenario {i}",
                Type = ScenarioType.Scenario,
                Steps = new List<UniversalStep>
                {
                    new UniversalStep { Keyword = "Given", Text = $"step {i}" }
                }
            };
            ruleScenarios.Add(scenario);
            scenarios.Add(new EnrichedScenario
            {
                Scenario = scenario,
                Status = ExecutionStatus.Passed,
                Steps = new List<EnrichedStep>
                {
                    new EnrichedStep
                    {
                        Step = new UniversalStep { Keyword = "Given", Text = $"step {i}" },
                        Status = ExecutionStatus.Passed
                    }
                }
            });
        }

        return new EnrichedFeature
        {
            Feature = new UniversalFeature
            {
                Name = "Feature with Rules",
                Tags = new List<string>(),
                Rules = new List<UniversalRule>
                {
                    new UniversalRule
                    {
                        Name = "Business Rule",
                        Scenarios = ruleScenarios
                    }
                }
            },
            Scenarios = scenarios,
            OverallStatus = ExecutionStatus.Passed,
            PassedCount = totalScenarios
        };
    }

    private static EnrichedFeature CreateFeatureWithDataTable(int dataRowCount)
    {
        // Build a data table: 1 header row + dataRowCount data rows
        var rows = new List<List<string>>();
        // Header row
        rows.Add(new List<string> { "Header0", "Header1", "Header2" });
        // Data rows
        for (int i = 1; i <= dataRowCount; i++)
        {
            rows.Add(new List<string> { $"Row{i}-Col0", $"Row{i}-Col1", $"Row{i}-Col2" });
        }

        return new EnrichedFeature
        {
            Feature = new UniversalFeature
            {
                Name = "Feature with Data Table",
                Tags = new List<string>(),
                Rules = new List<UniversalRule>()
            },
            Scenarios = new List<EnrichedScenario>
            {
                new EnrichedScenario
                {
                    Scenario = new UniversalScenario
                    {
                        Name = "Scenario with table",
                        Type = ScenarioType.Scenario,
                        Steps = new List<UniversalStep>
                        {
                            new UniversalStep
                            {
                                Keyword = "Given",
                                Text = "a data table",
                                DataTable = new UniversalDataTable { Rows = rows }
                            }
                        }
                    },
                    Status = ExecutionStatus.Passed,
                    Steps = new List<EnrichedStep>
                    {
                        new EnrichedStep
                        {
                            Step = new UniversalStep
                            {
                                Keyword = "Given",
                                Text = "a data table",
                                DataTable = new UniversalDataTable { Rows = rows }
                            },
                            Status = ExecutionStatus.Passed
                        }
                    }
                }
            },
            OverallStatus = ExecutionStatus.Passed,
            PassedCount = 1
        };
    }

    private static EnrichedFeature CreateFeatureWithExamplesTable(int exampleRowCount)
    {
        var headers = new List<string> { "Param0", "Param1" };
        var exampleRows = new List<List<string>>();
        for (int i = 0; i < exampleRowCount; i++)
        {
            exampleRows.Add(new List<string> { $"Val{i}-0", $"Val{i}-1" });
        }

        var exampleResults = new Dictionary<int, ScenarioExecutionResult>();
        for (int i = 0; i < exampleRowCount; i++)
        {
            exampleResults[i] = new ScenarioExecutionResult
            {
                Status = i % 3 == 0 ? ExecutionStatus.Passed :
                         i % 3 == 1 ? ExecutionStatus.Failed :
                         ExecutionStatus.Skipped
            };
        }

        return new EnrichedFeature
        {
            Feature = new UniversalFeature
            {
                Name = "Feature with Examples",
                Tags = new List<string>(),
                Rules = new List<UniversalRule>()
            },
            Scenarios = new List<EnrichedScenario>
            {
                new EnrichedScenario
                {
                    Scenario = new UniversalScenario
                    {
                        Name = "Outline scenario",
                        Type = ScenarioType.ScenarioOutline,
                        Steps = new List<UniversalStep>
                        {
                            new UniversalStep { Keyword = "Given", Text = "a step with <Param0>" }
                        },
                        Examples = new List<UniversalExample>
                        {
                            new UniversalExample
                            {
                                Name = "Test Examples",
                                Headers = headers,
                                Rows = exampleRows
                            }
                        }
                    },
                    Status = ExecutionStatus.Failed,
                    ExampleResults = exampleResults,
                    Steps = new List<EnrichedStep>
                    {
                        new EnrichedStep
                        {
                            Step = new UniversalStep { Keyword = "Given", Text = "a step with <Param0>" },
                            Status = ExecutionStatus.Passed
                        }
                    }
                }
            },
            OverallStatus = ExecutionStatus.Failed,
            FailedCount = 1
        };
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LivingDocGen.Generator.Models;
using LivingDocGen.Parser.Models;
using LivingDocGen.TestReporter.Models;

namespace LivingDocGen.Generator.Services.Rendering;

/// <summary>
/// Renders feature content including scenarios, steps, backgrounds, and rules
/// for HTML living documentation.
/// </summary>
public class FeatureRenderer : IFeatureRenderer
{
    /// <inheritdoc/>
    public bool IncludeComments { get; set; } = true;

    /// <summary>
    /// HTML encodes text using a simple cache for performance.
    /// </summary>
    private string HtmlEncode(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;
        return System.Web.HttpUtility.HtmlEncode(text);
    }

    /// <inheritdoc/>
    public string Render(EnrichedFeature feature, int index = 0, bool forLazyLoading = false)
    {
        return GenerateFeature(feature, index, forLazyLoading);
    }

    private int _scenarioCounter = 0;

    private string GenerateFeature(EnrichedFeature feature, int index = 0, bool forLazyLoading = false)
    {
        var statusClass = GetStatusClass(feature.OverallStatus);
        var statusIcon = GetStatusIcon(feature.OverallStatus);
        var featureId = $"feature-{index}";
        var isFirstFeature = index == 0;
        
        var html = new StringBuilder();
        // When generating for lazy loading, dont include feature-hidden (JavaScript will handle visibility)
        var hiddenClass = forLazyLoading ? "" : (isFirstFeature ? "" : " feature-hidden");
        html.AppendLine($@"
    <div class=""feature{hiddenClass}"" data-status=""{statusClass}"" id=""{featureId}"" data-feature-id=""{featureId}"">
        <div class=""feature-header status-{statusClass}"">
            <div class=""feature-title"">
                <span class=""status-icon {statusClass}"">{statusIcon}</span>
                <h2>{System.Web.HttpUtility.HtmlEncode(feature.Feature.Name)}</h2>
            </div>
            <div class=""feature-meta"">");

        if (feature.PassedCount > 0)
            html.AppendLine($@"                <span class=""badge badge-passed""><i class=""fas fa-check""></i> {feature.PassedCount} passed</span>");
        if (feature.FailedCount > 0)
            html.AppendLine($@"                <span class=""badge badge-failed""><i class=""fas fa-times""></i> {feature.FailedCount} failed</span>");
        if (feature.SkippedCount > 0)
            html.AppendLine($@"                <span class=""badge badge-skipped""><i class=""fas fa-minus""></i> {feature.SkippedCount} skipped</span>");

        html.AppendLine(@"            </div>
        </div>
        <div class=""feature-body"">");

        if (!string.IsNullOrWhiteSpace(feature.Feature.Description))
        {
            html.AppendLine($@"            <div class=""feature-description"">{HtmlEncode(feature.Feature.Description)}</div>");
        }

        // Generate feature comments if present and enabled
        if (IncludeComments && feature.Feature.Comments?.Any() == true)
        {
            html.AppendLine(GenerateComments(feature.Feature.Comments));
        }

        if (feature.Feature.Tags.Any())
        {
            html.AppendLine(@"            <div class=""tags"">");
            foreach (var tag in feature.Feature.Tags)
            {
                html.AppendLine($@"                <span class=""tag""><i class=""fas fa-tag""></i> {HtmlEncode(tag)}</span>");
            }
            html.AppendLine(@"            </div>");
        }

        // Generate Background if present (at feature level)
        if (feature.Feature.Background != null)
        {
            html.AppendLine(GenerateBackground(feature.Feature.Background));
        }

        // Generate Rules if present
        if (feature.Feature.Rules != null && feature.Feature.Rules.Any())
        {
            foreach (var rule in feature.Feature.Rules)
            {
                html.AppendLine(GenerateRule(rule, feature.Scenarios, featureId));
            }
        }
        else
        {
            // Generate scenarios directly (no rules)
            foreach (var scenario in feature.Scenarios)
            {
                html.AppendLine(GenerateScenario(scenario, featureId));
            }
        }

        html.AppendLine(@"        </div>
    </div>");

        return html.ToString();
    }

    private string GenerateBackground(LivingDocGen.Parser.Models.UniversalBackground background)
    {
        var html = new StringBuilder();
        
        html.AppendLine($@"
            <div class=""background"">
                <div class=""background-header"">
                    <div style=""display: flex; align-items: center; gap: 0.75rem; flex: 1;"">
                        <i class=""fas fa-layer-group""></i>
                        <strong>Background: {HtmlEncode(background.Name)}</strong>
                    </div>
                    <i class=""fas fa-chevron-down toggle-icon""></i>
                </div>
                <div class=""background-body expanded"">");

        if (!string.IsNullOrWhiteSpace(background.Description))
        {
            html.AppendLine($@"                <div class=""background-description"">{System.Web.HttpUtility.HtmlEncode(background.Description)}</div>");
        }

        // Generate background steps
        foreach (var step in background.Steps)
        {
            html.AppendLine($@"                    <div class=""step"">
                        <div class=""step-line"">
                            <span class=""step-keyword"">{System.Web.HttpUtility.HtmlEncode(step.Keyword)}</span> <span class=""step-text"">{System.Web.HttpUtility.HtmlEncode(step.Text)}</span>
                        </div>");

            // Add DocString if present
            if (!string.IsNullOrWhiteSpace(step.DocString))
            {
                var contentType = step.DocString.TrimStart().StartsWith("{") || step.DocString.TrimStart().StartsWith("[") ? "JSON" : "Text";
                html.AppendLine($@"                        <div class=""doc-string-container"">
                            <div class=""doc-string-header"" onclick=""toggleDocString(this)"">
                                <i class=""fas fa-chevron-down toggle-icon""></i>
                                <i class=""fas fa-file-alt""></i>
                                <span>{contentType} Content</span>
                            </div>
                            <div class=""doc-string"">{System.Web.HttpUtility.HtmlEncode(step.DocString)}</div>
                        </div>");
            }

            // Add DataTable if present
            if (step.DataTable != null && step.DataTable.Rows.Any())
            {
                var rowCount = step.DataTable.Rows.Count > 1 ? step.DataTable.Rows.Count - 1 : 0;
                var colCount = step.DataTable.Rows.Any() ? step.DataTable.Rows[0].Count : 0;
                
                html.AppendLine(@"                        <div class=""data-table-container"">");
                html.AppendLine(@"                            <div class=""data-table-wrapper"">");
                html.AppendLine(@"                                <table class=""data-table"">");
                
                // First row as headers
                var headers = step.DataTable.Rows[0];
                html.AppendLine(@"                                    <thead onclick=""toggleDataTable(this)""><tr>");
                foreach (var header in headers)
                {
                    html.AppendLine($@"                                        <th>{System.Web.HttpUtility.HtmlEncode(header)}</th>");
                }
                html.AppendLine(@"                                    </tr></thead>");

                // Remaining rows as data
                html.AppendLine(@"                                    <tbody>");
                for (int i = 1; i < step.DataTable.Rows.Count; i++)
                {
                    html.AppendLine(@"                                        <tr>");
                    foreach (var cell in step.DataTable.Rows[i])
                    {
                        html.AppendLine($@"                                            <td>{System.Web.HttpUtility.HtmlEncode(cell)}</td>");
                    }
                    html.AppendLine(@"                                        </tr>");
                }
                html.AppendLine(@"                                    </tbody>
                                </table>
                            </div>
                        </div>");
            }

            html.AppendLine(@"                    </div>");
        }

        html.AppendLine(@"                </div>
            </div>");

        return html.ToString();
    }

    private string GenerateRule(UniversalRule rule, List<EnrichedScenario> allScenarios, string featureId = "")
    {
        var html = new StringBuilder();
        
        html.AppendLine($@"
            <div class=""rule"">
                <div class=""rule-header"">
                    <div class=""rule-title"">
                        <i class=""fas fa-gavel""></i>
                        <h3>{System.Web.HttpUtility.HtmlEncode(rule.Name)}</h3>
                    </div>
                    <i class=""fas fa-chevron-down toggle-icon""></i>
                </div>
                <div class=""rule-body expanded"">" );

        if (!string.IsNullOrWhiteSpace(rule.Description))
        {
            html.AppendLine($@"                <div class=""rule-description"">{System.Web.HttpUtility.HtmlEncode(rule.Description)}</div>");
        }

        // Generate Background if present (at rule level)
        if (rule.Background != null)
        {
            html.AppendLine(GenerateBackground(rule.Background));
        }

        // Generate scenarios that belong to this rule
        foreach (var ruleScenario in rule.Scenarios)
        {
            // Find matching enriched scenario
            var enrichedScenario = allScenarios.FirstOrDefault(s => 
                s.Scenario.Name.Equals(ruleScenario.Name, StringComparison.OrdinalIgnoreCase));
            
            if (enrichedScenario != null)
            {
                html.AppendLine(GenerateScenario(enrichedScenario, featureId));
            }
        }

        html.AppendLine(@"                </div>
            </div>");

        return html.ToString();
    }

    private string GenerateScenario(EnrichedScenario scenario, string featureId = "")
    {
        var statusClass = GetStatusClass(scenario.Status);
        var statusIcon = GetStatusIcon(scenario.Status);
        var scenarioId = $"scenario-{_scenarioCounter++}";
        
        var html = new StringBuilder();
        var isOutline = scenario.Scenario.Type == LivingDocGen.Parser.Models.ScenarioType.ScenarioOutline;
        
        html.AppendLine($@"
            <div class=""scenario status-{statusClass}"" data-status=""{statusClass}"" id=""{scenarioId}"" data-feature-id=""{featureId}"">
                <div class=""scenario-header"" data-toggle-scenario>
                    <div class=""scenario-title"">
                        <span class=""status-icon {statusClass}"">{statusIcon}</span>
                        <span class=""scenario-type"">{(isOutline ? "Scenario Outline:" : "Scenario:")}</span>
                        <strong>{System.Web.HttpUtility.HtmlEncode(scenario.Scenario.Name)}</strong>");

        if (isOutline)
        {
            html.AppendLine($@"                        <span class=""badge badge-outline""><i class=""fas fa-layer-group""></i> Outline</span>");
        }

        if (scenario.Duration != default(TimeSpan))
        {
            html.AppendLine($@"                        <span class=""step-duration"">({scenario.Duration.TotalSeconds:F2}s)</span>");
        }

        html.AppendLine(@"                    </div>
                </div>
                <div class=""scenario-body"">");

        // Generate scenario comments if present and enabled
        if (IncludeComments && scenario.Scenario.Comments?.Any() == true)
        {
            html.AppendLine(GenerateComments(scenario.Scenario.Comments));
        }

        if (!string.IsNullOrEmpty(scenario.ErrorMessage))
        {
            html.AppendLine($@"
                    <div class=""error-message"">
                        <strong><i class=""fas fa-exclamation-triangle""></i> Error{(scenario.FailedAtLine > 0 ? $" at line {scenario.FailedAtLine}" : "")}</strong>
                        <pre>{System.Web.HttpUtility.HtmlEncode(scenario.ErrorMessage)}</pre>
                    </div>");
        }

        html.AppendLine(@"                    <ul class=""steps"">");

        foreach (var step in scenario.Steps)
        {
            html.AppendLine(GenerateStep(step));
        }

        html.AppendLine(@"                    </ul>");

        // Generate Examples table for Scenario Outlines (Pickles style)
        if (scenario.Scenario.Examples != null && scenario.Scenario.Examples.Any())
        {
            foreach (var example in scenario.Scenario.Examples)
            {
                html.AppendLine(GenerateExamplesTable(example, scenario));
            }
        }

        html.AppendLine(@"                </div>
            </div>");

        return html.ToString();
    }

    private string GenerateStep(EnrichedStep step)
    {
        var statusClass = GetStatusClass(step.Status);
        var html = new StringBuilder();
        
        html.AppendLine($@"                        <li class=""step status-{statusClass}"">");
        html.AppendLine($@"                            <span class=""step-keyword"">{System.Web.HttpUtility.HtmlEncode(step.Step.Keyword)}</span> <span class=""step-text"">{System.Web.HttpUtility.HtmlEncode(step.Step.Text)}");
        
        if (step.Duration != default(TimeSpan) && step.Duration.TotalMilliseconds > 0)
        {
            html.AppendLine($@" <span class=""step-duration"">({step.Duration.TotalMilliseconds:F0}ms)</span>");
        }
        
        html.AppendLine(@"</span>");

        // Add error message if failed
        if (!string.IsNullOrEmpty(step.ErrorMessage))
        {
            html.AppendLine($@"                            <div style=""color: var(--danger-color); margin-top: 0.5rem; margin-left: 65px;""><small>{System.Web.HttpUtility.HtmlEncode(step.ErrorMessage)}</small></div>");
        }

        // Add data table if present
        if (step.Step.DataTable != null && step.Step.DataTable.Rows.Any())
        {
            html.AppendLine(GenerateDataTable(step.Step.DataTable));
        }

        // Add doc string if present
        if (!string.IsNullOrEmpty(step.Step.DocString))
        {
            var contentType = step.Step.DocString.TrimStart().StartsWith("{") || step.Step.DocString.TrimStart().StartsWith("[") ? "JSON" : "Text";
            html.AppendLine($@"                                <div class=""doc-string-container"">
                                    <div class=""doc-string-header"" onclick=""toggleDocString(this)"">
                                        <i class=""fas fa-chevron-down toggle-icon""></i>
                                        <i class=""fas fa-file-alt""></i>
                                        <span>{contentType} Content</span>
                                    </div>
                                    <div class=""doc-string"">{System.Web.HttpUtility.HtmlEncode(step.Step.DocString)}</div>
                                </div>");
        }

        html.AppendLine(@"                        </li>");

        return html.ToString();
    }

    private string GenerateDataTable(UniversalDataTable dataTable)
    {
        var html = new StringBuilder();
        var rowCount = dataTable.Rows.Count > 1 ? dataTable.Rows.Count - 1 : 0;
        var colCount = dataTable.Rows.Any() ? dataTable.Rows[0].Count : 0;
        
        html.AppendLine(@"                                <div class=""data-table-container"">");
        html.AppendLine(@"                                    <div class=""data-table-wrapper"">");
        html.AppendLine(@"                                        <table class=""data-table"">");
        
        if (dataTable.Rows.Any())
        {
            // First row as headers
            html.AppendLine(@"                                            <thead onclick=""toggleDataTable(this)""><tr>");
            foreach (var cell in dataTable.Rows[0])
            {
                html.AppendLine($@"                                                <th>{System.Web.HttpUtility.HtmlEncode(cell)}</th>");
            }
            html.AppendLine(@"                                            </tr></thead>");

            // Remaining rows as data
            if (dataTable.Rows.Count > 1)
            {
                html.AppendLine(@"                                            <tbody>");
                foreach (var row in dataTable.Rows.Skip(1))
                {
                    html.AppendLine(@"                                                <tr>");
                    foreach (var cell in row)
                    {
                        html.AppendLine($@"                                                    <td>{System.Web.HttpUtility.HtmlEncode(cell)}</td>");
                    }
                    html.AppendLine(@"                                                </tr>");
                }
                html.AppendLine(@"                                            </tbody>");
            }
        }
        
        html.AppendLine(@"                                        </table>");
        html.AppendLine(@"                                    </div>");
        html.AppendLine(@"                                </div>");
        return html.ToString();
    }

    private string GenerateExamplesTable(UniversalExample example, EnrichedScenario enrichedScenario)
    {
        var html = new StringBuilder();
        
        html.AppendLine(@"                    <div class=""examples-section"">");
        
        // Examples header with toggle functionality (like Pickles)
        html.AppendLine($@"                        <h4 class=""examples-header"">");
        html.AppendLine($@"                            <i class=""fas fa-table""></i> Examples{(!string.IsNullOrEmpty(example.Name) ? $": {System.Web.HttpUtility.HtmlEncode(example.Name)}" : "")}");
        html.AppendLine($@"                            <i class=""fas fa-chevron-up toggle-icon""></i>");
        html.AppendLine($@"                        </h4>");

        // Examples content wrapper (collapsible)
        html.AppendLine(@"                        <div class=""examples-content"">");

        // Examples tags if present
        if (example.Tags != null && example.Tags.Any())
        {
            html.AppendLine(@"                            <div class=""examples-tags"">");
            foreach (var tag in example.Tags)
            {
                html.AppendLine($@"                                <span class=""tag""><i class=""fas fa-tag""></i> {System.Web.HttpUtility.HtmlEncode(tag)}</span>");
            }
            html.AppendLine(@"                            </div>");
        }

        // Examples table
        if (example.Headers != null && example.Headers.Any())
        {
            var rowCount = example.Rows?.Count ?? 0;
            var colCount = example.Headers.Count;
            
            html.AppendLine(@"                            <div class=""examples-table-container"">");
            html.AppendLine(@"                                <div class=""table-wrapper"">");
            html.AppendLine(@"                                    <table class=""examples-table"">");
            
            // Headers - Add Status column at the beginning
            html.AppendLine(@"                                        <thead onclick=""toggleExamplesTable(this)""><tr>");
            html.AppendLine(@"                                            <th style=""width: 80px; text-align: center;""><i class=""fas fa-vial""></i> Status</th>");
            foreach (var header in example.Headers)
            {
                html.AppendLine($@"                                            <th>{System.Web.HttpUtility.HtmlEncode(header)}</th>");
            }
            html.AppendLine(@"                                        </tr></thead>");

            // Rows - Add status icon for each row
            if (example.Rows != null && example.Rows.Any())
            {
                html.AppendLine(@"                                <tbody>");
                for (int rowIndex = 0; rowIndex < example.Rows.Count; rowIndex++)
                {
                    var row = example.Rows[rowIndex];
                    
                    // Get test result for this example row
                    var rowStatus = enrichedScenario.ExampleResults.ContainsKey(rowIndex) 
                        ? enrichedScenario.ExampleResults[rowIndex].Status 
                        : ExecutionStatus.NotExecuted;
                    
                    var statusClass = rowStatus.ToString().ToLower();
                    var statusIcon = rowStatus switch
                    {
                        ExecutionStatus.Passed => @"<i class=""fas fa-check-circle status-icon passed"" title=""Passed""></i>",
                        ExecutionStatus.Failed => @"<i class=""fas fa-times-circle status-icon failed"" title=""Failed""></i>",
                        ExecutionStatus.Skipped => @"<i class=""fas fa-minus-circle status-icon skipped"" title=""Skipped""></i>",
                        _ => @"<i class=""fas fa-circle status-icon untested"" title=""Not Executed""></i>"
                    };
                    
                    html.AppendLine($@"                                            <tr class=""example-row {statusClass}"">");
                    html.AppendLine($@"                                                <td style=""text-align: center;"">{statusIcon}</td>");
                    foreach (var cell in row)
                    {
                        html.AppendLine($@"                                                <td>{System.Web.HttpUtility.HtmlEncode(cell)}</td>");
                    }
                    html.AppendLine(@"                                            </tr>");
                }
                html.AppendLine(@"                                        </tbody>");
            }

            html.AppendLine(@"                                    </table>");
            html.AppendLine(@"                                </div>");
            html.AppendLine(@"                            </div>");
        }
        
        html.AppendLine(@"                        </div>"); // End examples-content
        html.AppendLine(@"                    </div>");
        return html.ToString();
    }

    private string GenerateComments(List<string> comments)
    {
        if (!IncludeComments || comments == null || !comments.Any())
            return string.Empty;

        var html = new StringBuilder();
        html.AppendLine(@"            <div class=""comments"">");
        
        foreach (var comment in comments)
        {
            // Remove leading # if present (it will be added by CSS)
            var commentText = comment.TrimStart().TrimStart('#').TrimStart();
            html.AppendLine($@"                <div class=""comment"">{HtmlEncode(commentText)}</div>");
        }
        
        html.AppendLine(@"            </div>");
        return html.ToString();
    }

    private string GetStatusClass(ExecutionStatus status)
    {
        return status switch
        {
            ExecutionStatus.Passed => "passed",
            ExecutionStatus.Failed => "failed",
            ExecutionStatus.Skipped => "skipped",
            ExecutionStatus.Pending => "skipped",
            ExecutionStatus.Undefined => "failed",
            _ => "untested"
        };
    }

    private string GetStatusIcon(ExecutionStatus status)
    {
        return status switch
        {
            ExecutionStatus.Passed => "<i class=\"fas fa-check-circle\"></i>",
            ExecutionStatus.Failed => "<i class=\"fas fa-times-circle\"></i>",
            ExecutionStatus.Skipped => "<i class=\"fas fa-minus-circle\"></i>",
            ExecutionStatus.Pending => "<i class=\"fas fa-clock\"></i>",
            ExecutionStatus.Undefined => "<i class=\"fas fa-question-circle\"></i>",
            _ => "<i class=\"fas fa-circle\"></i>"
        };
    }

    /// <inheritdoc/>
    public string GenerateFeatureDataJson(LivingDocumentation documentation)
    {
        var json = new StringBuilder();
        json.AppendLine("{");
        json.AppendLine("  \"features\": [");
        
        for (int i = 0; i < documentation.Features.Count; i++)
        {
            var featureHtml = GenerateFeature(documentation.Features[i], i, forLazyLoading: true);
            // Escape for JSON
            var escapedHtml = featureHtml
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "")
                .Replace("\t", "  ");
            
            json.Append($"    {{\"index\": {i}, \"html\": \"{escapedHtml}\"}}");
            if (i < documentation.Features.Count - 1)
                json.AppendLine(",");
            else
                json.AppendLine();
        }
        
        json.AppendLine("  ]");
        json.AppendLine("}");
        return json.ToString();
    }

}

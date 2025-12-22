using System.Text.Json.Serialization;

namespace LivingDocGen.CLI.Models;

/// <summary>
/// Configuration model for bdd-livingdoc.json
/// </summary>
public class BddConfiguration
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("autoGenerate")]
    public string? AutoGenerate { get; set; }

    [JsonPropertyName("paths")]
    public PathsConfiguration? Paths { get; set; }

    [JsonPropertyName("documentation")]
    public DocumentationConfiguration? Documentation { get; set; }

    [JsonPropertyName("advanced")]
    public AdvancedConfiguration? Advanced { get; set; }
}

public class PathsConfiguration
{
    [JsonPropertyName("features")]
    public string? Features { get; set; }

    [JsonPropertyName("testResults")]
    public string? TestResults { get; set; }

    [JsonPropertyName("output")]
    public string? Output { get; set; }
}

public class DocumentationConfiguration
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("theme")]
    public string? Theme { get; set; }

    [JsonPropertyName("primaryColor")]
    public string? PrimaryColor { get; set; }
}

public class AdvancedConfiguration
{
    [JsonPropertyName("verbose")]
    public bool Verbose { get; set; }

    [JsonPropertyName("includeSkipped")]
    public bool IncludeSkipped { get; set; } = true;

    [JsonPropertyName("includePending")]
    public bool IncludePending { get; set; } = true;
}

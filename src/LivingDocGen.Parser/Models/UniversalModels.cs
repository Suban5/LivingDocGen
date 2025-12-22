namespace LivingDocGen.Parser.Models;

/// <summary>
/// Universal representation of a BDD Feature (normalized across all frameworks)
/// </summary>
public class UniversalFeature
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Language { get; set; } = "en";
    public string FilePath { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public List<string> Comments { get; set; } = new();
    public List<UniversalScenario> Scenarios { get; set; } = new();
    public List<UniversalRule> Rules { get; set; } = new();
    public UniversalBackground? Background { get; set; }
    
    /// <summary>
    /// Metadata about the source framework
    /// </summary>
    public FrameworkMetadata Metadata { get; set; } = new();
}

public class UniversalRule
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public UniversalBackground? Background { get; set; }
    public List<UniversalScenario> Scenarios { get; set; } = new();
    public int LineNumber { get; set; }
}

public class UniversalScenario
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public List<string> Comments { get; set; } = new();
    public List<UniversalStep> Steps { get; set; } = new();
    public List<UniversalExample>? Examples { get; set; }
    public ScenarioType Type { get; set; } = ScenarioType.Scenario;
    
    /// <summary>
    /// Line number in the original feature file
    /// </summary>
    public int LineNumber { get; set; }
}

public class UniversalStep
{
    public string Keyword { get; set; } = string.Empty; // Given, When, Then, And, But
    public string Text { get; set; } = string.Empty;
    public string? DocString { get; set; }
    public UniversalDataTable? DataTable { get; set; }
    public int LineNumber { get; set; }
}

public class UniversalDataTable
{
    public List<List<string>> Rows { get; set; } = new();
}

public class UniversalExample
{
    public List<string> Tags { get; set; } = new();
    public string Name { get; set; } = string.Empty;
    public List<string> Headers { get; set; } = new();
    public List<List<string>> Rows { get; set; } = new();
}

public class UniversalBackground
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<UniversalStep> Steps { get; set; } = new();
}

public class FrameworkMetadata
{
    public BDDFramework Framework { get; set; }
    public string Version { get; set; } = string.Empty;
    public DateTime ParsedAt { get; set; } = DateTime.UtcNow;
}

public enum BDDFramework
{
    Unknown,
    Cucumber,
    SpecFlow,
    ReqnRoll,
    JBehave
}

public enum ScenarioType
{
    Scenario,
    ScenarioOutline,
    ScenarioTemplate // JBehave specific
}

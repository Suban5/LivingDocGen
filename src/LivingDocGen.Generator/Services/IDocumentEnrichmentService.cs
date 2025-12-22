namespace LivingDocGen.Generator.Services;

using LivingDocGen.Generator.Models;
using LivingDocGen.Parser.Models;
using LivingDocGen.TestReporter.Models;

/// <summary>
/// Interface for enriching features with test execution data
/// </summary>
public interface IDocumentEnrichmentService
{
    /// <summary>
    /// Enrich features with test execution results
    /// </summary>
    LivingDocumentation EnrichDocumentation(
        List<UniversalFeature> features,
        TestExecutionReport? testReport = null);
}

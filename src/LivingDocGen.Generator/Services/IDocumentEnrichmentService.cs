using System.Collections.Generic;
using System.Threading;

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
    /// <param name="features">List of parsed features to enrich</param>
    /// <param name="testReport">Test execution report containing test results</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>Enriched living documentation</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when features is null</exception>
    /// <exception cref="System.OperationCanceledException">Thrown when operation is cancelled</exception>
    LivingDocumentation EnrichDocumentation(
        List<UniversalFeature> features,
        TestExecutionReport testReport = null,
        CancellationToken cancellationToken = default);
}

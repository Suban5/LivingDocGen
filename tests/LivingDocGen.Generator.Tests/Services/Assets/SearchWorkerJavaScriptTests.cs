using Xunit;
using LivingDocGen.Generator.Services.Assets;

namespace LivingDocGen.Generator.Tests.Services.Assets;

/// <summary>
/// Tests for SearchWorkerJavaScript (PR-4: Web Worker for search/filter).
/// Verifies the generated worker JavaScript contains all required components.
/// </summary>
public class SearchWorkerJavaScriptTests
{
    // ===== Basic Structure =====

    [Fact]
    public void Generate_ReturnsNonEmptyString()
    {
        var result = SearchWorkerJavaScript.Generate();

        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public void Generate_DoesNotContainScriptTags()
    {
        // Worker code is not wrapped in <script> tags — it's loaded via Blob URL
        var result = SearchWorkerJavaScript.Generate();

        Assert.DoesNotContain("<script>", result);
        Assert.DoesNotContain("</script>", result);
    }

    [Fact]
    public void Generate_ContainsProtocolVersion()
    {
        var result = SearchWorkerJavaScript.Generate();

        Assert.Contains($"const PROTOCOL_VERSION = '{SearchWorkerJavaScript.ProtocolVersion}'", result);
    }

    [Fact]
    public void Generate_ContainsUseStrict()
    {
        var result = SearchWorkerJavaScript.Generate();

        Assert.Contains("'use strict'", result);
    }

    // ===== Message Handler =====

    [Fact]
    public void Generate_ContainsOnMessageHandler()
    {
        var result = SearchWorkerJavaScript.Generate();

        Assert.Contains("self.onmessage", result);
    }

    [Fact]
    public void Generate_HandlesInitIndexMessage()
    {
        var result = SearchWorkerJavaScript.Generate();

        Assert.Contains("case 'INIT_INDEX':", result);
        Assert.Contains("handleInitIndex(msg)", result);
    }

    [Fact]
    public void Generate_HandlesQueryMessage()
    {
        var result = SearchWorkerJavaScript.Generate();

        Assert.Contains("case 'QUERY':", result);
        Assert.Contains("handleQuery(msg)", result);
    }

    [Fact]
    public void Generate_HandlesQueryCancelMessage()
    {
        var result = SearchWorkerJavaScript.Generate();

        Assert.Contains("case 'QUERY_CANCEL':", result);
        Assert.Contains("handleQueryCancel(msg)", result);
    }

    // ===== Index Initialization =====

    [Fact]
    public void Generate_InitIndex_BuildsTokenIndex()
    {
        var result = SearchWorkerJavaScript.Generate();

        Assert.Contains("tokenIndex = new Map()", result);
        Assert.Contains("invertedTokenIndex", result);
    }

    [Fact]
    public void Generate_InitIndex_BuildsStatusSets()
    {
        var result = SearchWorkerJavaScript.Generate();

        Assert.Contains("statusSets = new Map()", result);
        Assert.Contains("statusIndex", result);
    }

    [Fact]
    public void Generate_InitIndex_BuildsTagIndex()
    {
        var result = SearchWorkerJavaScript.Generate();

        Assert.Contains("tagIndex = new Map()", result);
    }

    [Fact]
    public void Generate_InitIndex_UsesInt32ArrayForCompactStorage()
    {
        var result = SearchWorkerJavaScript.Generate();

        Assert.Contains("new Int32Array(postings)", result);
    }

    [Fact]
    public void Generate_InitIndex_BuildsScenarioToFeatureLookup()
    {
        var result = SearchWorkerJavaScript.Generate();

        Assert.Contains("scenarioToFeature", result);
        Assert.Contains("new Int32Array(totalScenarios)", result);
    }

    [Fact]
    public void Generate_InitIndex_SendsReadyMessage()
    {
        var result = SearchWorkerJavaScript.Generate();

        Assert.Contains("type: 'READY'", result);
    }

    // ===== Query Processing =====

    [Fact]
    public void Generate_Query_PerformsStatusFiltering()
    {
        var result = SearchWorkerJavaScript.Generate();

        Assert.Contains("statusSets.has(statusFilter)", result);
    }

    [Fact]
    public void Generate_Query_PerformsTagFiltering()
    {
        var result = SearchWorkerJavaScript.Generate();

        Assert.Contains("tagIndex.has(tag)", result);
    }

    [Fact]
    public void Generate_Query_PerformsTextTokenLookup()
    {
        var result = SearchWorkerJavaScript.Generate();

        Assert.Contains("findTokenPostings(token)", result);
    }

    [Fact]
    public void Generate_Query_UsesSetIntersection()
    {
        var result = SearchWorkerJavaScript.Generate();

        Assert.Contains("intersectSorted(resultOrdinals, candidateSets[i])", result);
    }

    [Fact]
    public void Generate_Query_SortsCandidateSetsBySize()
    {
        var result = SearchWorkerJavaScript.Generate();

        // Smallest-first sorting for efficient intersection
        Assert.Contains("candidateSets.sort(function(a, b)", result);
        Assert.Contains("return a.length - b.length", result);
    }

    [Fact]
    public void Generate_Query_DerivesFeatureOrdinals()
    {
        var result = SearchWorkerJavaScript.Generate();

        Assert.Contains("featureOrdinalSet", result);
        Assert.Contains("scenarioToFeature[resultOrdinals[i]]", result);
    }

    [Fact]
    public void Generate_Query_SendsResultMessage()
    {
        var result = SearchWorkerJavaScript.Generate();

        Assert.Contains("type: 'RESULT'", result);
        Assert.Contains("scenarioOrdinals:", result);
        Assert.Contains("featureOrdinals:", result);
    }

    [Fact]
    public void Generate_Query_IncludesDiagnosticStats()
    {
        var result = SearchWorkerJavaScript.Generate();

        Assert.Contains("evalMs:", result);
        Assert.Contains("candidateCount:", result);
        Assert.Contains("indexLookups:", result);
    }

    // ===== Stale Query Prevention =====

    [Fact]
    public void Generate_Query_TracksLatestQuerySeq()
    {
        var result = SearchWorkerJavaScript.Generate();

        Assert.Contains("latestQuerySeq", result);
    }

    [Fact]
    public void Generate_Query_DiscardsStaleResults()
    {
        var result = SearchWorkerJavaScript.Generate();

        Assert.Contains("querySeq < latestQuerySeq", result);
    }

    [Fact]
    public void Generate_QueryCancel_TracksCancelledRequests()
    {
        var result = SearchWorkerJavaScript.Generate();

        Assert.Contains("cancelledQueries.add(msg.requestId)", result);
    }

    [Fact]
    public void Generate_Query_ChecksCancellationDuringIntersection()
    {
        var result = SearchWorkerJavaScript.Generate();

        Assert.Contains("cancelledQueries.has(requestId)", result);
    }

    // ===== Set Operations =====

    [Fact]
    public void Generate_ContainsIntersectSortedFunction()
    {
        var result = SearchWorkerJavaScript.Generate();

        Assert.Contains("function intersectSorted(a, b)", result);
    }

    [Fact]
    public void Generate_IntersectSorted_UsesGallopingForLargeSizeRatio()
    {
        var result = SearchWorkerJavaScript.Generate();

        Assert.Contains("gallopSearch(b, target, j)", result);
        Assert.Contains("b.length > a.length * 4", result);
    }

    [Fact]
    public void Generate_ContainsGallopSearchFunction()
    {
        var result = SearchWorkerJavaScript.Generate();

        Assert.Contains("function gallopSearch(arr, target, startIdx)", result);
    }

    [Fact]
    public void Generate_ContainsUnionSortedFunction()
    {
        var result = SearchWorkerJavaScript.Generate();

        Assert.Contains("function unionSorted(sets)", result);
    }

    // ===== Tokenizer =====

    [Fact]
    public void Generate_ContainsTokenizeFunction()
    {
        var result = SearchWorkerJavaScript.Generate();

        Assert.Contains("function tokenize(text)", result);
    }

    [Fact]
    public void Generate_Tokenize_MinLengthFilterIs2()
    {
        var result = SearchWorkerJavaScript.Generate();

        Assert.Contains("t.length >= 2", result);
    }

    [Fact]
    public void Generate_ContainsPrefixMatchLogic()
    {
        var result = SearchWorkerJavaScript.Generate();

        Assert.Contains("function findTokenPostings(queryToken)", result);
        Assert.Contains("key.startsWith(queryToken)", result);
    }

    // ===== Error Handling =====

    [Fact]
    public void Generate_ContainsSendErrorFunction()
    {
        var result = SearchWorkerJavaScript.Generate();

        Assert.Contains("function sendError(requestId, querySeq, message)", result);
        Assert.Contains("type: 'ERROR'", result);
    }

    [Fact]
    public void Generate_InitIndex_HandlesInvalidData()
    {
        var result = SearchWorkerJavaScript.Generate();

        Assert.Contains("Invalid index data: missing scenarioRecords", result);
    }

    [Fact]
    public void Generate_Query_HandlesUninitializedIndex()
    {
        var result = SearchWorkerJavaScript.Generate();

        Assert.Contains("Index not initialized", result);
    }
}

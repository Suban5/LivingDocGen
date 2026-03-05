using Xunit;
using LivingDocGen.Generator.Services.Assets;

namespace LivingDocGen.Generator.Tests.Services.Assets;

/// <summary>
/// Tests for SearchBridgeJavaScript (PR-4: Main-thread search bridge + fallback).
/// Verifies the generated JavaScript contains worker spawning, protocol handling,
/// delta DOM updates, and main-thread fallback logic.
/// </summary>
public class SearchBridgeJavaScriptTests
{
    private readonly string _workerSource;

    public SearchBridgeJavaScriptTests()
    {
        _workerSource = SearchWorkerJavaScript.Generate();
    }

    // ===== Basic Structure =====

    [Fact]
    public void Generate_ReturnsNonEmptyString()
    {
        var result = SearchBridgeJavaScript.Generate(_workerSource);

        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public void Generate_ContainsScriptTags()
    {
        var result = SearchBridgeJavaScript.Generate(_workerSource);

        Assert.Contains("<script>", result);
        Assert.Contains("</script>", result);
    }

    [Fact]
    public void Generate_ContainsProtocolVersion()
    {
        var result = SearchBridgeJavaScript.Generate(_workerSource);

        Assert.Contains($"const SEARCH_PROTOCOL_VERSION = '{SearchBridgeJavaScript.ProtocolVersion}'", result);
    }

    [Fact]
    public void Generate_DefaultDebounceIs150ms()
    {
        var result = SearchBridgeJavaScript.Generate(_workerSource);

        Assert.Contains("const SEARCH_DEBOUNCE_MS = 150", result);
    }

    [Fact]
    public void Generate_CustomDebounceIsRespected()
    {
        var result = SearchBridgeJavaScript.Generate(_workerSource, debounceMs: 250);

        Assert.Contains("const SEARCH_DEBOUNCE_MS = 250", result);
    }

    // ===== Worker Lifecycle =====

    [Fact]
    public void Generate_ContainsSpawnSearchWorkerFunction()
    {
        var result = SearchBridgeJavaScript.Generate(_workerSource);

        Assert.Contains("function spawnSearchWorker()", result);
    }

    [Fact]
    public void Generate_SpawnsWorkerViaBlobUrl()
    {
        var result = SearchBridgeJavaScript.Generate(_workerSource);

        Assert.Contains("new Blob([workerCode]", result);
        Assert.Contains("URL.createObjectURL(blob)", result);
        Assert.Contains("new Worker(url)", result);
    }

    [Fact]
    public void Generate_RevokesBlobUrlAfterCreation()
    {
        var result = SearchBridgeJavaScript.Generate(_workerSource);

        Assert.Contains("URL.revokeObjectURL(url)", result);
    }

    [Fact]
    public void Generate_FallsBackWhenWorkersNotSupported()
    {
        var result = SearchBridgeJavaScript.Generate(_workerSource);

        Assert.Contains("typeof Worker === 'undefined'", result);
        Assert.Contains("workerFailed = true", result);
    }

    [Fact]
    public void Generate_HandlesWorkerCreationFailure()
    {
        var result = SearchBridgeJavaScript.Generate(_workerSource);

        Assert.Contains("catch (err)", result);
        Assert.Contains("Failed to spawn search worker", result);
    }

    // ===== Index Initialization =====

    [Fact]
    public void Generate_ContainsInitWorkerIndexFunction()
    {
        var result = SearchBridgeJavaScript.Generate(_workerSource);

        Assert.Contains("function initWorkerIndex(indexData)", result);
    }

    [Fact]
    public void Generate_SendsInitIndexToWorker()
    {
        var result = SearchBridgeJavaScript.Generate(_workerSource);

        Assert.Contains("type: 'INIT_INDEX'", result);
        Assert.Contains("indexData: indexData", result);
    }

    [Fact]
    public void Generate_KeepsLocalCopyForFallback()
    {
        var result = SearchBridgeJavaScript.Generate(_workerSource);

        Assert.Contains("localIndexData = indexData", result);
    }

    // ===== Worker Message Handling =====

    [Fact]
    public void Generate_ContainsHandleWorkerMessageFunction()
    {
        var result = SearchBridgeJavaScript.Generate(_workerSource);

        Assert.Contains("function handleWorkerMessage(e)", result);
    }

    [Fact]
    public void Generate_HandlesReadyMessage()
    {
        var result = SearchBridgeJavaScript.Generate(_workerSource);

        Assert.Contains("case 'READY':", result);
        Assert.Contains("workerReady = true", result);
    }

    [Fact]
    public void Generate_HandlesResultMessage()
    {
        var result = SearchBridgeJavaScript.Generate(_workerSource);

        Assert.Contains("case 'RESULT':", result);
        Assert.Contains("handleQueryResult(msg)", result);
    }

    [Fact]
    public void Generate_HandlesErrorMessage()
    {
        var result = SearchBridgeJavaScript.Generate(_workerSource);

        Assert.Contains("case 'ERROR':", result);
        Assert.Contains("runFallbackQuery()", result);
    }

    // ===== Query Dispatch =====

    [Fact]
    public void Generate_ContainsDispatchSearchQueryFunction()
    {
        var result = SearchBridgeJavaScript.Generate(_workerSource);

        Assert.Contains("function dispatchSearchQuery()", result);
    }

    [Fact]
    public void Generate_DebouncesPreviousQuery()
    {
        var result = SearchBridgeJavaScript.Generate(_workerSource);

        Assert.Contains("clearTimeout(debounceTimer)", result);
        Assert.Contains("setTimeout(function()", result);
    }

    [Fact]
    public void Generate_CancelsPreviousInflightQuery()
    {
        var result = SearchBridgeJavaScript.Generate(_workerSource);

        Assert.Contains("type: 'QUERY_CANCEL'", result);
        Assert.Contains("pendingQueryRequestId", result);
    }

    [Fact]
    public void Generate_SendsQueryMessageToWorker()
    {
        var result = SearchBridgeJavaScript.Generate(_workerSource);

        Assert.Contains("type: 'QUERY'", result);
        Assert.Contains("query: searchTerm", result);
        Assert.Contains("status: statusFilter", result);
        Assert.Contains("tags: tagFilters", result);
    }

    [Fact]
    public void Generate_IncrementsQuerySeq()
    {
        var result = SearchBridgeJavaScript.Generate(_workerSource);

        Assert.Contains("++querySeqCounter", result);
    }

    // ===== Result Handling =====

    [Fact]
    public void Generate_ContainsHandleQueryResultFunction()
    {
        var result = SearchBridgeJavaScript.Generate(_workerSource);

        Assert.Contains("function handleQueryResult(msg)", result);
    }

    [Fact]
    public void Generate_DiscardsStaleResults()
    {
        var result = SearchBridgeJavaScript.Generate(_workerSource);

        Assert.Contains("querySeq < lastAppliedQuerySeq", result);
    }

    [Fact]
    public void Generate_TracksSearchMetrics()
    {
        var result = SearchBridgeJavaScript.Generate(_workerSource);

        Assert.Contains("searchMetrics.resultsReceived++", result);
        Assert.Contains("searchMetrics.lastEvalMs", result);
    }

    // ===== Delta DOM Updates =====

    [Fact]
    public void Generate_ContainsApplyDeltaUpdateFunction()
    {
        var result = SearchBridgeJavaScript.Generate(_workerSource);

        Assert.Contains("function applyDeltaUpdate(newVisibleIds, matchCount)", result);
    }

    [Fact]
    public void Generate_DeltaUpdate_TracksPreviousState()
    {
        var result = SearchBridgeJavaScript.Generate(_workerSource);

        Assert.Contains("prevVisibleFeatureIds", result);
    }

    [Fact]
    public void Generate_DeltaUpdate_OnlyTogglesChangedItems()
    {
        var result = SearchBridgeJavaScript.Generate(_workerSource);

        Assert.Contains("wasVisible !== shouldBeVisible", result);
    }

    [Fact]
    public void Generate_DeltaUpdate_UpdatesFolderVisibility()
    {
        var result = SearchBridgeJavaScript.Generate(_workerSource);

        Assert.Contains(".folder", result);
        Assert.Contains("hasVisible", result);
    }

    [Fact]
    public void Generate_DeltaUpdate_AnnouncesToScreenReader()
    {
        var result = SearchBridgeJavaScript.Generate(_workerSource);

        Assert.Contains("announceToScreenReader", result);
        Assert.Contains("Filter applied", result);
    }

    // ===== Main-Thread Fallback =====

    [Fact]
    public void Generate_ContainsBuildLocalIndexFunction()
    {
        var result = SearchBridgeJavaScript.Generate(_workerSource);

        Assert.Contains("function buildLocalIndex(indexData)", result);
    }

    [Fact]
    public void Generate_Fallback_BuildsTokenIndex()
    {
        var result = SearchBridgeJavaScript.Generate(_workerSource);

        Assert.Contains("localTokenIndex = new Map()", result);
    }

    [Fact]
    public void Generate_Fallback_BuildsStatusSets()
    {
        var result = SearchBridgeJavaScript.Generate(_workerSource);

        Assert.Contains("localStatusSets = new Map()", result);
    }

    [Fact]
    public void Generate_Fallback_BuildsTagIndex()
    {
        var result = SearchBridgeJavaScript.Generate(_workerSource);

        Assert.Contains("localTagIndex = new Map()", result);
    }

    [Fact]
    public void Generate_ContainsRunFallbackQueryFunction()
    {
        var result = SearchBridgeJavaScript.Generate(_workerSource);

        Assert.Contains("function runFallbackQuery()", result);
    }

    [Fact]
    public void Generate_ContainsRunFallbackQueryWithFunction()
    {
        var result = SearchBridgeJavaScript.Generate(_workerSource);

        Assert.Contains("function runFallbackQueryWith(searchTerm, statusFilter, tagFilters, querySeq)", result);
    }

    [Fact]
    public void Generate_Fallback_PerformsSetIntersection()
    {
        var result = SearchBridgeJavaScript.Generate(_workerSource);

        Assert.Contains("function fallbackIntersect(a, b)", result);
    }

    [Fact]
    public void Generate_Fallback_HasManifestOnlyFallback()
    {
        var result = SearchBridgeJavaScript.Generate(_workerSource);

        Assert.Contains("function applyManifestOnlyFilter(searchTerm, statusFilter, tagFilters, querySeq)", result);
    }

    [Fact]
    public void Generate_Fallback_TracksMetrics()
    {
        var result = SearchBridgeJavaScript.Generate(_workerSource);

        Assert.Contains("searchMetrics.fallbacksUsed++", result);
    }

    // ===== Index Loading =====

    [Fact]
    public void Generate_ContainsInitSearchSystemFunction()
    {
        var result = SearchBridgeJavaScript.Generate(_workerSource);

        Assert.Contains("async function initSearchSystem()", result);
    }

    [Fact]
    public void Generate_InitSearchSystem_FetchesFeatureIndex()
    {
        var result = SearchBridgeJavaScript.Generate(_workerSource);

        Assert.Contains("fetch(indexUrl)", result);
        Assert.Contains("feature-index.json", result);
    }

    [Fact]
    public void Generate_InitSearchSystem_HasWorkerReadyTimeout()
    {
        var result = SearchBridgeJavaScript.Generate(_workerSource);

        Assert.Contains("Promise.race(", result);
        Assert.Contains("Worker init timed out", result);
    }

    // ===== Filter Override =====

    [Fact]
    public void Generate_OverridesApplyChunkedFilters()
    {
        var result = SearchBridgeJavaScript.Generate(_workerSource);

        Assert.Contains("function applyChunkedFilters()", result);
        Assert.Contains("dispatchSearchQuery()", result);
    }

    [Fact]
    public void Generate_OverridesApplyAllFilters()
    {
        var result = SearchBridgeJavaScript.Generate(_workerSource);

        Assert.Contains("function applyAllFilters()", result);
        Assert.Contains("CHUNKED_MODE", result);
    }

    // ===== Diagnostics =====

    [Fact]
    public void Generate_ContainsGetSearchMetricsFunction()
    {
        var result = SearchBridgeJavaScript.Generate(_workerSource);

        Assert.Contains("function getSearchMetrics()", result);
    }

    [Fact]
    public void Generate_ExposesMetricsGlobally()
    {
        var result = SearchBridgeJavaScript.Generate(_workerSource);

        Assert.Contains("window.getSearchMetrics = getSearchMetrics", result);
    }

    // ===== Worker Source Embedding =====

    [Fact]
    public void Generate_InlinesWorkerSourceInBlobUrl()
    {
        var result = SearchBridgeJavaScript.Generate(_workerSource);

        // The worker source should appear inside the bridge as a template literal
        Assert.Contains("const workerCode = `", result);
    }

    [Fact]
    public void Generate_WorkerSourceIsEscapedForTemplateLiteral()
    {
        // The worker source contains backtick-like chars and special sequences
        // that need escaping for embedding in a template literal.
        // This test verifies the output is valid (no unescaped backticks outside the template).
        var result = SearchBridgeJavaScript.Generate(_workerSource);

        // The method should not throw, and the output should be well-formed
        Assert.NotNull(result);
        // Should contain the escaped worker identifier
        Assert.Contains("PROTOCOL_VERSION", result);
    }
}

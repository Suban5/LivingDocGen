using Xunit;
using LivingDocGen.Generator.Services.Assets;

namespace LivingDocGen.Generator.Tests.Services.Assets;

/// <summary>
/// Tests for ChunkedRuntimeJavaScript (PR-3: Runtime Loader + Chunk Rendering).
/// Verifies the generated JavaScript contains all required runtime components.
/// </summary>
public class ChunkedRuntimeJavaScriptTests
{
    [Fact]
    public void Generate_ReturnsNonEmptyString()
    {
        var result = ChunkedRuntimeJavaScript.Generate();

        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public void Generate_ContainsScriptTags()
    {
        var result = ChunkedRuntimeJavaScript.Generate();

        Assert.Contains("<script>", result);
        Assert.Contains("</script>", result);
    }

    [Fact]
    public void Generate_ContainsChunkedModeFlag()
    {
        var result = ChunkedRuntimeJavaScript.Generate();

        Assert.Contains("const CHUNKED_MODE = true", result);
    }

    // ===== LRU Cache =====

    [Fact]
    public void Generate_ContainsLRUCacheClass()
    {
        var result = ChunkedRuntimeJavaScript.Generate();

        Assert.Contains("class LRUCache", result);
    }

    [Fact]
    public void Generate_LRUCache_HasGetSetHasMethods()
    {
        var result = ChunkedRuntimeJavaScript.Generate();

        Assert.Contains("get(key)", result);
        Assert.Contains("set(key, value)", result);
        Assert.Contains("has(key)", result);
    }

    [Fact]
    public void Generate_LRUCache_EvictsOldestOnOverflow()
    {
        var result = ChunkedRuntimeJavaScript.Generate();

        // Verify the eviction logic exists
        Assert.Contains("this._map.size >= this._capacity", result);
        Assert.Contains("this._map.keys().next().value", result);
        Assert.Contains("unmountFeature(oldestKey)", result);
    }

    [Fact]
    public void Generate_DefaultCacheSize_Is10()
    {
        var result = ChunkedRuntimeJavaScript.Generate();

        Assert.Contains("const LRU_CACHE_SIZE = 10", result);
    }

    [Fact]
    public void Generate_CustomCacheSize_IsRespected()
    {
        var result = ChunkedRuntimeJavaScript.Generate(lruCacheSize: 25);

        Assert.Contains("const LRU_CACHE_SIZE = 25", result);
    }

    [Fact]
    public void Generate_UnmountFeature_ClearsDom()
    {
        var result = ChunkedRuntimeJavaScript.Generate();

        Assert.Contains("function unmountFeature(featureId)", result);
        Assert.Contains("data-loaded", result);
    }

    // ===== Manifest Loader =====

    [Fact]
    public void Generate_ContainsManifestLoader()
    {
        var result = ChunkedRuntimeJavaScript.Generate();

        Assert.Contains("async function loadManifest()", result);
    }

    [Fact]
    public void Generate_ManifestLoader_DelegatesToContractLoader()
    {
        var result = ChunkedRuntimeJavaScript.Generate();

        // PR-2.5: loadManifest() now delegates to ContractLoader for
        // schema validation, retry, and buildId tracking
        Assert.Contains("ContractLoader.loadManifest()", result);
    }

    [Fact]
    public void Generate_ManifestLoader_HasReadyCallbackMechanism()
    {
        var result = ChunkedRuntimeJavaScript.Generate();

        Assert.Contains("function onManifestReady(cb)", result);
        Assert.Contains("manifestReadyCallbacks", result);
    }

    [Fact]
    public void Generate_ManifestLoader_HandlesFetchErrors()
    {
        var result = ChunkedRuntimeJavaScript.Generate();

        Assert.Contains("manifestError", result);
        Assert.Contains("showDegradedBanner", result);
    }

    // ===== Chunk Fetcher =====

    [Fact]
    public void Generate_ContainsChunkFetcher()
    {
        var result = ChunkedRuntimeJavaScript.Generate();

        Assert.Contains("async function fetchChunk(featureId)", result);
    }

    [Fact]
    public void Generate_ChunkFetcher_UsesCacheFirst()
    {
        var result = ChunkedRuntimeJavaScript.Generate();

        Assert.Contains("chunkCache.get(featureId)", result);
        Assert.Contains("Cache hit", result);
    }

    [Fact]
    public void Generate_ChunkFetcher_DeduplicatesInFlightRequests()
    {
        var result = ChunkedRuntimeJavaScript.Generate();

        Assert.Contains("inflightFetches", result);
        Assert.Contains("inflightFetches.has(featureId)", result);
    }

    [Fact]
    public void Generate_ChunkFetcher_DelegatesToContractLoader()
    {
        var result = ChunkedRuntimeJavaScript.Generate();

        // PR-2.5: chunk fetching now delegates to ContractLoader for
        // hash validation, buildId consistency, and retry logic
        Assert.Contains("ContractLoader.loadChunk(entry)", result);
    }

    // ===== Chunk Renderer =====

    [Fact]
    public void Generate_ContainsChunkRenderer()
    {
        var result = ChunkedRuntimeJavaScript.Generate();

        Assert.Contains("function renderChunk(featureId, chunk)", result);
    }

    [Fact]
    public void Generate_ChunkRenderer_HidesOtherContainers()
    {
        var result = ChunkedRuntimeJavaScript.Generate();

        Assert.Contains("chunk-container", result);
        Assert.Contains("feature-hidden", result);
    }

    [Fact]
    public void Generate_ChunkRenderer_InjectsChunkHtml()
    {
        var result = ChunkedRuntimeJavaScript.Generate();

        Assert.Contains("container.innerHTML = chunk.html", result);
    }

    [Fact]
    public void Generate_ChunkRenderer_TracksActiveFeature()
    {
        var result = ChunkedRuntimeJavaScript.Generate();

        Assert.Contains("activeFeatureId = featureId", result);
    }

    // ===== Sidebar Builder =====

    [Fact]
    public void Generate_ContainsSidebarBuilder()
    {
        var result = ChunkedRuntimeJavaScript.Generate();

        Assert.Contains("function buildSidebarFromManifest(m)", result);
    }

    [Fact]
    public void Generate_SidebarBuilder_UsesFolderTree()
    {
        var result = ChunkedRuntimeJavaScript.Generate();

        Assert.Contains("function renderFolderNode", result);
        Assert.Contains("function renderFolder", result);
        Assert.Contains("function renderFeatureItem", result);
    }

    [Fact]
    public void Generate_SidebarBuilder_PopulatesSidebarNav()
    {
        var result = ChunkedRuntimeJavaScript.Generate();

        Assert.Contains("document.getElementById('sidebar-nav')", result);
    }

    [Fact]
    public void Generate_SidebarBuilder_AutoSelectsFirstFeature()
    {
        var result = ChunkedRuntimeJavaScript.Generate();

        Assert.Contains("selectChunkedFeature(m.featureMap[0].featureId)", result);
    }

    // ===== Feature Selection =====

    [Fact]
    public void Generate_ContainsFeatureSelection()
    {
        var result = ChunkedRuntimeJavaScript.Generate();

        Assert.Contains("async function selectChunkedFeature(featureId)", result);
    }

    [Fact]
    public void Generate_FeatureSelection_UpdatesActiveState()
    {
        var result = ChunkedRuntimeJavaScript.Generate();

        // Updates sidebar active class
        Assert.Contains("item.classList.remove('active')", result);
        Assert.Contains("activeItem.classList.add('active')", result);
    }

    [Fact]
    public void Generate_FeatureSelection_SavesLastViewed()
    {
        var result = ChunkedRuntimeJavaScript.Generate();

        Assert.Contains("localStorage.setItem('bdd-last-feature'", result);
    }

    // ===== Idle Prefetch =====

    [Fact]
    public void Generate_ContainsIdlePrefetch()
    {
        var result = ChunkedRuntimeJavaScript.Generate();

        Assert.Contains("function schedulePrefetch()", result);
    }

    [Fact]
    public void Generate_IdlePrefetch_PrefetchesAdjacentFeatures()
    {
        var result = ChunkedRuntimeJavaScript.Generate();

        Assert.Contains("currentIdx + 1", result);
        Assert.Contains("currentIdx - 1", result);
    }

    [Fact]
    public void Generate_IdlePrefetch_UsesRequestIdleCallback()
    {
        var result = ChunkedRuntimeJavaScript.Generate();

        Assert.Contains("requestIdleCallback", result);
    }

    // ===== Degraded Mode =====

    [Fact]
    public void Generate_ContainsDegradedModeBanner()
    {
        var result = ChunkedRuntimeJavaScript.Generate();

        Assert.Contains("function showDegradedBanner(message)", result);
        Assert.Contains("degraded-banner", result);
    }

    // ===== Search & Filter Delegation (PR-4) =====

    [Fact]
    public void Generate_SearchDelegatedToSearchBridge()
    {
        var result = ChunkedRuntimeJavaScript.Generate();

        // applyChunkedFilters is now in SearchBridge, not here
        Assert.Contains("DELEGATED TO SEARCH BRIDGE", result);
        Assert.Contains("SearchBridgeJavaScript", result);
    }

    [Fact]
    public void Generate_InitializesSearchSystem()
    {
        var result = ChunkedRuntimeJavaScript.Generate();

        Assert.Contains("initSearchSystem()", result);
    }

    // ===== Tag Population =====

    [Fact]
    public void Generate_ContainsTagPopulationFromManifest()
    {
        var result = ChunkedRuntimeJavaScript.Generate();

        Assert.Contains("function populateTagsFromManifest(m)", result);
    }

    // ===== Initialization =====

    [Fact]
    public void Generate_ContainsDomContentLoadedInit()
    {
        var result = ChunkedRuntimeJavaScript.Generate();

        Assert.Contains("DOMContentLoaded", result);
    }

    [Fact]
    public void Generate_Init_LoadsManifest()
    {
        var result = ChunkedRuntimeJavaScript.Generate();

        Assert.Contains("await loadManifest()", result);
    }

    [Fact]
    public void Generate_Init_BuildsSidebar()
    {
        var result = ChunkedRuntimeJavaScript.Generate();

        Assert.Contains("buildSidebarFromManifest(manifest)", result);
    }

    [Fact]
    public void Generate_Init_PopulatesTags()
    {
        var result = ChunkedRuntimeJavaScript.Generate();

        Assert.Contains("populateTagsFromManifest(manifest)", result);
    }

    [Fact]
    public void Generate_Init_RestoresLastViewedFeature()
    {
        var result = ChunkedRuntimeJavaScript.Generate();

        Assert.Contains("localStorage.getItem('bdd-last-feature')", result);
    }

    [Fact]
    public void Generate_Init_SchedulesPrefetch()
    {
        var result = ChunkedRuntimeJavaScript.Generate();

        Assert.Contains("schedulePrefetch", result);
    }

    // ===== Legacy Override =====

    [Fact]
    public void Generate_OverridesLegacySelectFeature()
    {
        var result = ChunkedRuntimeJavaScript.Generate();

        Assert.Contains("function selectFeature(featureIdOrChunkedId)", result);
        Assert.Contains("_legacySelectFeature", result);
    }

    [Fact]
    public void Generate_ApplyAllFiltersOverrideDelegatedToSearchBridge()
    {
        var result = ChunkedRuntimeJavaScript.Generate();

        // applyAllFilters override is now in SearchBridge, not ChunkedRuntime
        Assert.DoesNotContain("function applyAllFilters()", result);
        Assert.DoesNotContain("_legacyApplyAllFilters", result);
    }

    // ===== Accessibility =====

    [Fact]
    public void Generate_SidebarItems_HaveKeyboardHandlers()
    {
        var result = ChunkedRuntimeJavaScript.Generate();

        Assert.Contains("handleChunkedFeatureKeydown", result);
        Assert.Contains("handleFolderKeydown", result);
    }

    [Fact]
    public void Generate_SidebarItems_HaveAriaAttributes()
    {
        var result = ChunkedRuntimeJavaScript.Generate();

        Assert.Contains("role=\"treeitem\"", result);
        Assert.Contains("aria-label", result);
    }

    [Fact]
    public void Generate_FilterResults_AnnounceToScreenReaderDelegatedToSearchBridge()
    {
        var result = ChunkedRuntimeJavaScript.Generate();

        // announceToScreenReader in filter context is now in SearchBridge
        // ChunkedRuntime still uses aria attributes but screen reader announcements for filter results are delegated
        Assert.DoesNotContain("announceToScreenReader", result);
    }
}

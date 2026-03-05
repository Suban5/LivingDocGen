namespace LivingDocGen.Generator.Services.Assets;

/// <summary>
/// Generates the main-thread search bridge JavaScript (PR-4).
/// This script:
/// - Spawns the search Web Worker via Blob URL (inlined worker source)
/// - Sends INIT_INDEX with the fetched feature-index.json data
/// - Sends QUERY messages on filter/search changes (with debounce + cancellation)
/// - Receives RESULT messages and applies delta DOM updates to sidebar
/// - Provides a synchronous main-thread fallback if Worker is unavailable
/// - Tracks query metrics for diagnostics
/// </summary>
public static class SearchBridgeJavaScript
{
    /// <summary>
    /// Protocol version matching <see cref="Models.Contracts.WorkerMessage.ProtocolVersion"/>.
    /// </summary>
    public const string ProtocolVersion = "1.1";

    /// <summary>
    /// Generates the complete search bridge JavaScript wrapped in script tags.
    /// </summary>
    /// <param name="workerSource">The raw worker JS source code (from SearchWorkerJavaScript.Generate()).</param>
    /// <param name="debounceMs">Debounce delay in ms for search/filter queries (default: 150).</param>
    /// <returns>JavaScript code wrapped in script tags.</returns>
    public static string Generate(string workerSource, int debounceMs = 150)
    {
        // Escape backticks and backslashes in worker source for template literal embedding
        var escapedWorkerSource = workerSource
            .Replace("\\", "\\\\")
            .Replace("`", "\\`")
            .Replace("${", "\\${");

        return $@"
    <script>
    // ============================================
    // SEARCH BRIDGE — PR-4: Worker-Based Search/Filter
    // ============================================
    // Spawns a Web Worker for off-main-thread search evaluation.
    // Falls back to synchronous main-thread search if Worker is unavailable.
    // Replaces the manifest-only applyChunkedFilters with index-backed search.

    const SEARCH_PROTOCOL_VERSION = '{ProtocolVersion}';
    const SEARCH_DEBOUNCE_MS = {debounceMs};

    // ---- Worker State ----
    let searchWorker = null;
    let workerReady = false;
    let workerFailed = false;
    let workerInitResolve = null;
    let workerInitPromise = null;

    // ---- Query State ----
    let querySeqCounter = 0;
    let pendingQueryRequestId = null;
    let debounceTimer = null;
    let lastAppliedQuerySeq = -1;

    // ---- Index State (for fallback) ----
    let localIndexData = null;

    // ---- Metrics ----
    const searchMetrics = {{
        queriesDispatched: 0,
        resultsReceived: 0,
        fallbacksUsed: 0,
        totalEvalMs: 0,
        lastEvalMs: 0
    }};

    // ---- Previous DOM state for delta updates ----
    let prevVisibleFeatureIds = null;

    // ============================================
    // 1. WORKER LIFECYCLE
    // ============================================

    /**
     * Spawns the search worker from an inline Blob URL.
     * Returns a promise that resolves when the worker sends READY.
     */
    function spawnSearchWorker() {{
        if (typeof Worker === 'undefined') {{
            console.warn('⚠ Web Workers not supported — using main-thread fallback');
            workerFailed = true;
            return Promise.resolve(false);
        }}

        try {{
            const workerCode = `{escapedWorkerSource}`;
            const blob = new Blob([workerCode], {{ type: 'application/javascript' }});
            const url = URL.createObjectURL(blob);

            searchWorker = new Worker(url);
            URL.revokeObjectURL(url); // Safe to revoke after Worker is created

            workerInitPromise = new Promise(function(resolve) {{
                workerInitResolve = resolve;
            }});

            searchWorker.onmessage = handleWorkerMessage;
            searchWorker.onerror = function(err) {{
                console.error('✗ Search worker error:', err);
                workerFailed = true;
                workerReady = false;
                // Resolve init promise so callers don't hang
                if (workerInitResolve) {{
                    workerInitResolve(false);
                    workerInitResolve = null;
                }}
            }};

            return workerInitPromise;
        }} catch (err) {{
            console.warn('⚠ Failed to spawn search worker:', err, '— using fallback');
            workerFailed = true;
            return Promise.resolve(false);
        }}
    }}

    /**
     * Sends the index data to the worker for initialization.
     * @param {{object}} indexData - The parsed feature-index.json object.
     */
    function initWorkerIndex(indexData) {{
        localIndexData = indexData; // Keep for fallback

        if (searchWorker && !workerFailed) {{
            searchWorker.postMessage({{
                type: 'INIT_INDEX',
                protocol: SEARCH_PROTOCOL_VERSION,
                requestId: 'init-' + Date.now(),
                querySeq: 0,
                timestamp: Date.now(),
                payload: {{
                    indexData: indexData
                }}
            }});
        }} else {{
            // Fallback: build local index
            buildLocalIndex(indexData);
        }}
    }}

    /**
     * Handles messages from the search worker.
     */
    function handleWorkerMessage(e) {{
        const msg = e.data;
        if (!msg || !msg.type) return;

        switch (msg.type) {{
            case 'READY':
                workerReady = true;
                console.log('✓ Search worker ready: ' + (msg.payload?.count || 0) + ' scenarios indexed in ' +
                    (msg.payload?.stats?.evalMs || 0).toFixed(1) + 'ms (' +
                    (msg.payload?.stats?.tokenCount || 0) + ' tokens, ' +
                    (msg.payload?.stats?.tagCount || 0) + ' tags)');
                if (workerInitResolve) {{
                    workerInitResolve(true);
                    workerInitResolve = null;
                }}
                break;

            case 'RESULT':
                handleQueryResult(msg);
                break;

            case 'ERROR':
                console.error('✗ Search worker error:', msg.payload?.message);
                // Fallback to main-thread for this query
                if (msg.querySeq >= lastAppliedQuerySeq) {{
                    runFallbackQuery();
                }}
                break;

            case 'METRICS':
                console.log('📊 Worker metrics:', msg.payload?.stats);
                break;
        }}
    }}

    // ============================================
    // 2. QUERY DISPATCH
    // ============================================

    /**
     * Dispatches a search/filter query. Called whenever filters change.
     * Debounces rapid changes and cancels in-flight queries.
     */
    function dispatchSearchQuery() {{
        // Clear previous debounce
        if (debounceTimer) {{
            clearTimeout(debounceTimer);
            debounceTimer = null;
        }}

        debounceTimer = setTimeout(function() {{
            debounceTimer = null;
            executeSearchQuery();
        }}, SEARCH_DEBOUNCE_MS);
    }}

    /**
     * Executes the search query immediately (after debounce).
     */
    function executeSearchQuery() {{
        const querySeq = ++querySeqCounter;
        const requestId = 'q-' + querySeq + '-' + Date.now();

        // Cancel previous in-flight query
        if (pendingQueryRequestId && searchWorker && workerReady) {{
            searchWorker.postMessage({{
                type: 'QUERY_CANCEL',
                protocol: SEARCH_PROTOCOL_VERSION,
                requestId: pendingQueryRequestId,
                querySeq: querySeq,
                timestamp: Date.now(),
                payload: {{}}
            }});
        }}

        const searchTerm = (activeFilters.searchTerm || '').trim();
        const statusFilter = activeFilters.status || 'all';
        const tagFilters = activeFilters.tags || [];

        // Use worker if available, else fallback
        if (searchWorker && workerReady && !workerFailed) {{
            pendingQueryRequestId = requestId;
            searchMetrics.queriesDispatched++;

            searchWorker.postMessage({{
                type: 'QUERY',
                protocol: SEARCH_PROTOCOL_VERSION,
                requestId: requestId,
                querySeq: querySeq,
                timestamp: Date.now(),
                payload: {{
                    query: searchTerm,
                    status: statusFilter,
                    tags: tagFilters
                }}
            }});
        }} else {{
            // Main-thread fallback
            runFallbackQueryWith(searchTerm, statusFilter, tagFilters, querySeq);
        }}
    }}

    // ============================================
    // 3. RESULT HANDLING + DELTA DOM UPDATES
    // ============================================

    /**
     * Handles a RESULT message from the worker.
     * Applies delta DOM updates — only changes visibility of nodes that differ.
     */
    function handleQueryResult(msg) {{
        const querySeq = msg.querySeq || 0;

        // Discard stale results
        if (querySeq < lastAppliedQuerySeq) {{
            return;
        }}

        lastAppliedQuerySeq = querySeq;
        pendingQueryRequestId = null;
        searchMetrics.resultsReceived++;

        const payload = msg.payload || {{}};
        const stats = payload.stats || {{}};
        searchMetrics.lastEvalMs = stats.evalMs || 0;
        searchMetrics.totalEvalMs += searchMetrics.lastEvalMs;

        // Convert feature ordinals to feature IDs using manifest
        const matchingFeatureIds = new Set();
        if (manifest && manifest.featureMap) {{
            const featureOrdinals = payload.featureOrdinals || [];
            for (let i = 0; i < featureOrdinals.length; i++) {{
                const ord = featureOrdinals[i];
                if (ord >= 0 && ord < manifest.featureMap.length) {{
                    matchingFeatureIds.add(manifest.featureMap[ord].featureId);
                }}
            }}
        }}

        // Apply delta DOM update
        applyDeltaUpdate(matchingFeatureIds, payload.count || 0);
    }}

    /**
     * Applies delta DOM updates — only toggles sidebar items that changed state.
     * @param {{Set<string>}} newVisibleIds - Feature IDs that should be visible.
     * @param {{number}} matchCount - Total matching scenario count.
     */
    function applyDeltaUpdate(newVisibleIds, matchCount) {{
        const noFiltersActive = (activeFilters.status || 'all') === 'all' &&
                                (activeFilters.tags || []).length === 0 &&
                                !(activeFilters.searchTerm || '').trim();

        // If no filters, show everything
        if (noFiltersActive) {{
            if (prevVisibleFeatureIds !== null) {{
                // Transition from filtered → unfiltered: show all
                document.querySelectorAll('.feature-item').forEach(function(item) {{
                    item.style.display = 'flex';
                }});
                document.querySelectorAll('.folder').forEach(function(folder) {{
                    folder.style.display = 'block';
                }});
            }}
            prevVisibleFeatureIds = null;
            showEmptyStateIfNeeded(manifest ? manifest.totalFeatures : 1, 'all');
            announceFilterResults(manifest ? manifest.totalScenarios : 0, manifest ? manifest.totalFeatures : 0);
            return;
        }}

        const sidebarItems = document.querySelectorAll('.feature-item');

        if (prevVisibleFeatureIds === null) {{
            // First filtered query — full pass
            sidebarItems.forEach(function(item) {{
                const fid = item.getAttribute('data-feature-id');
                item.style.display = newVisibleIds.has(fid) ? 'flex' : 'none';
            }});
        }} else {{
            // Delta update — only toggle items that changed
            sidebarItems.forEach(function(item) {{
                const fid = item.getAttribute('data-feature-id');
                const wasVisible = prevVisibleFeatureIds.has(fid);
                const shouldBeVisible = newVisibleIds.has(fid);

                if (wasVisible !== shouldBeVisible) {{
                    item.style.display = shouldBeVisible ? 'flex' : 'none';
                }}
            }});
        }}

        // Update folder visibility
        document.querySelectorAll('.folder').forEach(function(folder) {{
            const hasVisible = Array.from(folder.querySelectorAll('.feature-item'))
                .some(function(item) {{ return item.style.display !== 'none'; }});
            folder.style.display = hasVisible ? 'block' : 'none';
            if (hasVisible) folder.classList.remove('collapsed');
        }});

        prevVisibleFeatureIds = newVisibleIds;

        // Show empty state if no results
        showEmptyStateIfNeeded(newVisibleIds.size, activeFilters.status || 'all');
        announceFilterResults(matchCount, newVisibleIds.size);
    }}

    /**
     * Announces filter results to screen reader.
     */
    function announceFilterResults(scenarioCount, featureCount) {{
        if (typeof announceToScreenReader === 'function') {{
            announceToScreenReader(
                'Filter applied. Showing ' + scenarioCount + ' scenario' +
                (scenarioCount !== 1 ? 's' : '') + ' in ' + featureCount +
                ' feature' + (featureCount !== 1 ? 's' : '')
            );
        }}
    }}

    // ============================================
    // 4. MAIN-THREAD FALLBACK
    // ============================================
    // Mirrors the worker's set-intersection logic synchronously.
    // Used when Web Workers are not available or worker init fails.

    // Local fallback index structures
    let localTokenIndex = null;   // Map<string, number[]>
    let localStatusSets = null;   // Map<string, number[]>
    let localTagIndex = null;     // Map<string, number[]>
    let localScenarioToFeature = null; // number[]
    let localTotalScenarios = 0;

    /**
     * Builds local in-memory index from feature-index.json (fallback path).
     */
    function buildLocalIndex(indexData) {{
        if (!indexData || !indexData.scenarioRecords) return;

        const records = indexData.scenarioRecords;
        localTotalScenarios = records.length;

        // Scenario → Feature mapping
        localScenarioToFeature = new Array(localTotalScenarios);
        for (let i = 0; i < localTotalScenarios; i++) {{
            localScenarioToFeature[i] = records[i].featureOrdinal;
        }}

        // Token index
        localTokenIndex = new Map();
        if (indexData.invertedTokenIndex) {{
            Object.entries(indexData.invertedTokenIndex).forEach(function(entry) {{
                localTokenIndex.set(entry[0], entry[1]);
            }});
        }}

        // Status index
        localStatusSets = new Map();
        if (indexData.statusIndex) {{
            const si = indexData.statusIndex;
            if (si.passed)   localStatusSets.set('passed',   si.passed);
            if (si.failed)   localStatusSets.set('failed',   si.failed);
            if (si.skipped)  localStatusSets.set('skipped',  si.skipped);
            if (si.untested) localStatusSets.set('untested', si.untested);
        }}

        // Tag index
        localTagIndex = new Map();
        if (indexData.tagIndex) {{
            Object.entries(indexData.tagIndex).forEach(function(entry) {{
                localTagIndex.set(entry[0].toLowerCase(), entry[1]);
            }});
        }}

        console.log('✓ Local fallback index built: ' + localTotalScenarios + ' scenarios');
        searchMetrics.fallbacksUsed++;
    }}

    /**
     * Runs the current active filter query on the main thread (fallback).
     */
    function runFallbackQuery() {{
        const searchTerm = (activeFilters.searchTerm || '').trim();
        const statusFilter = activeFilters.status || 'all';
        const tagFilters = activeFilters.tags || [];
        runFallbackQueryWith(searchTerm, statusFilter, tagFilters, querySeqCounter);
    }}

    /**
     * Runs a specific query on the main thread using the local index.
     */
    function runFallbackQueryWith(searchTerm, statusFilter, tagFilters, querySeq) {{
        searchMetrics.fallbacksUsed++;

        if (!localTokenIndex && localIndexData) {{
            buildLocalIndex(localIndexData);
        }}

        // If still no index, fall back to manifest-only filtering
        if (!localTokenIndex) {{
            applyManifestOnlyFilter(searchTerm, statusFilter, tagFilters, querySeq);
            return;
        }}

        const t0 = performance.now();
        const candidateSets = [];

        // Status filter
        if (statusFilter !== 'all' && localStatusSets.has(statusFilter)) {{
            candidateSets.push(localStatusSets.get(statusFilter));
        }}

        // Tag filters (AND)
        const normalizedTags = tagFilters.map(function(t) {{ return t.toLowerCase().replace(/^@/, ''); }});
        for (let i = 0; i < normalizedTags.length; i++) {{
            const tag = normalizedTags[i];
            candidateSets.push(localTagIndex.has(tag) ? localTagIndex.get(tag) : []);
        }}

        // Text query
        if (searchTerm) {{
            const queryTokens = fallbackTokenize(searchTerm);
            for (let i = 0; i < queryTokens.length; i++) {{
                const postings = fallbackFindTokenPostings(queryTokens[i]);
                candidateSets.push(postings);
            }}
        }}

        // Intersect
        let result;
        if (candidateSets.length === 0) {{
            // No filters — all scenarios
            result = [];
            for (let i = 0; i < localTotalScenarios; i++) result.push(i);
        }} else {{
            candidateSets.sort(function(a, b) {{ return a.length - b.length; }});
            result = candidateSets[0].slice(); // copy
            for (let i = 1; i < candidateSets.length; i++) {{
                result = fallbackIntersect(result, candidateSets[i]);
                if (result.length === 0) break;
            }}
        }}

        // Derive feature ordinals
        const featureOrdinals = new Set();
        for (let i = 0; i < result.length; i++) {{
            featureOrdinals.add(localScenarioToFeature[result[i]]);
        }}

        // Convert to feature IDs
        const matchingFeatureIds = new Set();
        if (manifest && manifest.featureMap) {{
            featureOrdinals.forEach(function(ord) {{
                if (ord >= 0 && ord < manifest.featureMap.length) {{
                    matchingFeatureIds.add(manifest.featureMap[ord].featureId);
                }}
            }});
        }}

        lastAppliedQuerySeq = querySeq;
        const evalMs = performance.now() - t0;
        searchMetrics.lastEvalMs = evalMs;
        searchMetrics.totalEvalMs += evalMs;

        applyDeltaUpdate(matchingFeatureIds, result.length);
    }}

    /**
     * Manifest-only fallback when index is not available at all.
     * Filters on feature-level metadata only (no scenario-level granularity).
     */
    function applyManifestOnlyFilter(searchTerm, statusFilter, tagFilters, querySeq) {{
        if (!manifest) return;

        const matchingFeatureIds = new Set();
        const searchLower = (searchTerm || '').toLowerCase();

        manifest.featureMap.forEach(function(entry) {{
            const matchesStatus = statusFilter === 'all' || entry.status === statusFilter;
            const entryTags = (entry.tags || []).map(function(t) {{ return t.toLowerCase(); }});
            const matchesTags = tagFilters.length === 0 ||
                tagFilters.every(function(ft) {{
                    return entryTags.some(function(et) {{ return et.includes(ft.toLowerCase()); }});
                }});
            const matchesSearch = !searchLower ||
                (entry.name || '').toLowerCase().includes(searchLower) ||
                (entry.filePath || '').toLowerCase().includes(searchLower);

            if (matchesStatus && matchesTags && matchesSearch) {{
                matchingFeatureIds.add(entry.featureId);
            }}
        }});

        lastAppliedQuerySeq = querySeq;
        applyDeltaUpdate(matchingFeatureIds, matchingFeatureIds.size);
    }}

    // ---- Fallback helpers ----

    function fallbackTokenize(text) {{
        if (!text) return [];
        return text.toLowerCase()
            .split(/[\s_\-\/.@#\t\r\n()\[\]{{}}\:,;""']+/)
            .filter(function(t) {{ return t.length >= 2; }});
    }}

    function fallbackFindTokenPostings(queryToken) {{
        if (localTokenIndex.has(queryToken)) {{
            const sets = [localTokenIndex.get(queryToken)];
            if (queryToken.length >= 4) {{
                localTokenIndex.forEach(function(postings, key) {{
                    if (key !== queryToken && key.startsWith(queryToken)) {{
                        sets.push(postings);
                    }}
                }});
            }}
            if (sets.length === 1) return sets[0];
            return fallbackUnion(sets);
        }}
        // Prefix match
        const prefixSets = [];
        localTokenIndex.forEach(function(postings, key) {{
            if (key.startsWith(queryToken)) {{
                prefixSets.push(postings);
            }}
        }});
        if (prefixSets.length === 0) return [];
        if (prefixSets.length === 1) return prefixSets[0];
        return fallbackUnion(prefixSets);
    }}

    function fallbackIntersect(a, b) {{
        if (a.length === 0 || b.length === 0) return [];
        const setB = new Set(b);
        return a.filter(function(x) {{ return setB.has(x); }});
    }}

    function fallbackUnion(sets) {{
        const merged = new Set();
        for (let i = 0; i < sets.length; i++) {{
            const s = sets[i];
            for (let j = 0; j < s.length; j++) {{
                merged.add(s[j]);
            }}
        }}
        return Array.from(merged).sort(function(a, b) {{ return a - b; }});
    }}

    // ============================================
    // 5. INDEX LOADING
    // ============================================

    /**
     * Fetches feature-index.json and initializes the search system.
     * Called during chunked mode initialization after manifest is loaded.
     */
    async function initSearchSystem() {{
        try {{
            // 1. Spawn worker first (non-blocking)
            const workerSpawned = await spawnSearchWorker();

            // 2. PR-2.5: Load index via ContractLoader for schema validation,
            //    hash integrity, and buildId consistency checks.
            let indexData;
            if (typeof ContractLoader !== 'undefined') {{
                console.log('⏳ Loading search index via ContractLoader...');
                indexData = await ContractLoader.loadIndex(manifest);
            }} else {{
                // Fallback for non-chunked mode or if ContractLoader is not available
                const indexUrl = (manifest && manifest.indexFile) || 'feature-index.json';
                console.log('⏳ Fetching search index: ' + indexUrl);
                const response = await fetch(indexUrl);
                if (!response.ok) throw new Error('Index fetch failed: ' + response.status);
                indexData = await response.json();
            }}

            // 3. Send to worker (or build local index)
            initWorkerIndex(indexData);

            // 4. Wait for worker ready (with timeout)
            if (searchWorker && !workerFailed) {{
                const readyTimeout = new Promise(function(resolve) {{
                    setTimeout(function() {{ resolve(false); }}, 5000);
                }});
                const ready = await Promise.race([workerInitPromise, readyTimeout]);
                if (!ready) {{
                    console.warn('⚠ Worker init timed out — using fallback');
                    workerFailed = true;
                    buildLocalIndex(indexData);
                }}
            }}

            console.log('✓ Search system initialized (worker: ' + (workerReady ? 'active' : 'fallback') + ')');

        }} catch (err) {{
            console.error('✗ Failed to init search system:', err);
            workerFailed = true;
            // Manifest-only fallback will be used
        }}
    }}

    // ============================================
    // 6. OVERRIDE applyAllFilters / applyChunkedFilters
    // ============================================
    // Replace the chunked mode filter function with worker-backed search.

    // Store reference to original applyChunkedFilters if it exists
    const _originalApplyChunkedFilters = typeof applyChunkedFilters === 'function' ? applyChunkedFilters : null;

    /**
     * New applyChunkedFilters: delegates to worker-backed search.
     */
    function applyChunkedFilters() {{
        dispatchSearchQuery();
    }}

    // Override applyAllFilters to use the search bridge
    const _prevApplyAllFilters = typeof applyAllFilters === 'function' ? applyAllFilters : null;
    function applyAllFilters() {{
        if (typeof CHUNKED_MODE !== 'undefined' && CHUNKED_MODE && manifest) {{
            dispatchSearchQuery();
            return;
        }}
        if (_prevApplyAllFilters) _prevApplyAllFilters();
    }}

    // ============================================
    // 7. DIAGNOSTICS
    // ============================================

    /**
     * Returns current search metrics for diagnostics.
     * Accessible via console: getSearchMetrics()
     */
    function getSearchMetrics() {{
        return {{
            ...searchMetrics,
            workerActive: workerReady && !workerFailed,
            workerFailed: workerFailed,
            avgEvalMs: searchMetrics.resultsReceived > 0
                ? (searchMetrics.totalEvalMs / searchMetrics.resultsReceived).toFixed(2)
                : 0
        }};
    }}

    // Make diagnostics globally accessible
    if (typeof window !== 'undefined') {{
        window.getSearchMetrics = getSearchMetrics;
    }}

    </script>";
    }
}

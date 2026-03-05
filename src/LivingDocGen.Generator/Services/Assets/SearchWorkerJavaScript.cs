namespace LivingDocGen.Generator.Services.Assets;

/// <summary>
/// Generates the Web Worker JavaScript for search and filter operations (PR-4).
/// This worker runs off the main thread and:
/// - Receives the feature-index.json data via INIT_INDEX message
/// - Builds in-memory inverted index structures (token, status, tag)
/// - Handles QUERY messages with set-intersection filtering
/// - Handles QUERY_CANCEL to abort stale queries
/// - Sends back RESULT messages with matching scenario/feature ordinals
/// - Reports METRICS for diagnostics
/// </summary>
public static class SearchWorkerJavaScript
{
    /// <summary>
    /// Protocol version matching <see cref="Models.Contracts.WorkerMessage.ProtocolVersion"/>.
    /// </summary>
    public const string ProtocolVersion = "1.1";

    /// <summary>
    /// Generates the complete Web Worker JavaScript source code.
    /// This code is designed to be loaded as a Blob URL worker from the main thread.
    /// </summary>
    /// <returns>Raw JavaScript source (no script tags — this is worker code).</returns>
    public static string Generate()
    {
        return $@"
'use strict';

// ============================================
// SEARCH WORKER — PR-4: Worker-based Search/Filter
// ============================================
// Runs off the main thread. Receives feature-index.json,
// builds compact in-memory indexes, and evaluates queries
// using set intersection for O(min(|A|,|B|)) combined filtering.

const PROTOCOL_VERSION = '{ProtocolVersion}';

// ---- State ----
let indexData = null;       // Raw FeatureIndex JSON
let scenarioRecords = null; // Array of scenario records
let totalScenarios = 0;

// Inverted indexes (built on INIT_INDEX)
let tokenIndex = null;      // Map<string, Int32Array> — token → sorted scenario ordinals
let statusSets = null;      // Map<string, Int32Array> — status → sorted scenario ordinals
let tagIndex = null;         // Map<string, Int32Array> — tag → sorted scenario ordinals

// Feature ordinal lookup
let scenarioToFeature = null; // Int32Array — scenarioOrdinal → featureOrdinal

// Cancelled query tracking
const cancelledQueries = new Set();

// Latest query sequence (for stale-result prevention)
let latestQuerySeq = -1;

// ---- Message Handler ----
self.onmessage = function(e) {{
    const msg = e.data;
    if (!msg || !msg.type) return;

    switch (msg.type) {{
        case 'INIT_INDEX':
            handleInitIndex(msg);
            break;
        case 'QUERY':
            handleQuery(msg);
            break;
        case 'QUERY_CANCEL':
            handleQueryCancel(msg);
            break;
        default:
            sendError(msg.requestId, msg.querySeq, 'Unknown message type: ' + msg.type);
    }}
}};

// ============================================
// INIT_INDEX — Build in-memory indexes
// ============================================

function handleInitIndex(msg) {{
    const t0 = performance.now();
    try {{
        indexData = msg.payload.indexData;
        if (!indexData || !indexData.scenarioRecords) {{
            throw new Error('Invalid index data: missing scenarioRecords');
        }}

        scenarioRecords = indexData.scenarioRecords;
        totalScenarios = scenarioRecords.length;

        // Build scenario → feature lookup
        scenarioToFeature = new Int32Array(totalScenarios);
        for (let i = 0; i < totalScenarios; i++) {{
            scenarioToFeature[i] = scenarioRecords[i].featureOrdinal;
        }}

        // Build token inverted index (convert arrays to Int32Array for fast intersection)
        tokenIndex = new Map();
        if (indexData.invertedTokenIndex) {{
            const entries = Object.entries(indexData.invertedTokenIndex);
            for (let i = 0; i < entries.length; i++) {{
                const [token, postings] = entries[i];
                tokenIndex.set(token, new Int32Array(postings));
            }}
        }}

        // Build status index
        statusSets = new Map();
        if (indexData.statusIndex) {{
            const si = indexData.statusIndex;
            if (si.passed)   statusSets.set('passed',   new Int32Array(si.passed));
            if (si.failed)   statusSets.set('failed',   new Int32Array(si.failed));
            if (si.skipped)  statusSets.set('skipped',  new Int32Array(si.skipped));
            if (si.untested) statusSets.set('untested', new Int32Array(si.untested));
        }}

        // Build tag index
        tagIndex = new Map();
        if (indexData.tagIndex) {{
            const entries = Object.entries(indexData.tagIndex);
            for (let i = 0; i < entries.length; i++) {{
                const [tag, postings] = entries[i];
                tagIndex.set(tag.toLowerCase(), new Int32Array(postings));
            }}
        }}

        const initMs = performance.now() - t0;

        // Send READY
        self.postMessage({{
            type: 'READY',
            protocol: PROTOCOL_VERSION,
            requestId: msg.requestId || '',
            querySeq: 0,
            timestamp: Date.now(),
            payload: {{
                message: 'Index initialized',
                count: totalScenarios,
                stats: {{
                    evalMs: initMs,
                    candidateCount: totalScenarios,
                    indexLookups: 0,
                    tokenCount: tokenIndex.size,
                    tagCount: tagIndex.size,
                    statusBuckets: statusSets.size
                }}
            }}
        }});

    }} catch (err) {{
        sendError(msg.requestId, 0, 'INIT_INDEX failed: ' + err.message);
    }}
}}

// ============================================
// QUERY — Set-intersection search/filter
// ============================================

function handleQuery(msg) {{
    const t0 = performance.now();
    const requestId = msg.requestId || '';
    const querySeq = msg.querySeq || 0;

    // Track latest query seq for stale detection
    latestQuerySeq = Math.max(latestQuerySeq, querySeq);

    // Check if already cancelled
    if (cancelledQueries.has(requestId)) {{
        cancelledQueries.delete(requestId);
        return; // Silently drop
    }}

    if (!indexData) {{
        sendError(requestId, querySeq, 'Index not initialized');
        return;
    }}

    try {{
        const payload = msg.payload || {{}};
        const queryText = (payload.query || '').toLowerCase().trim();
        const statusFilter = (payload.status || 'all').toLowerCase();
        const tagFilters = (payload.tags || []).map(function(t) {{ return t.toLowerCase().replace(/^@/, ''); }});

        let indexLookups = 0;
        let candidateSets = [];

        // 1. Status filter → candidate set
        if (statusFilter !== 'all' && statusSets.has(statusFilter)) {{
            candidateSets.push(statusSets.get(statusFilter));
            indexLookups++;
        }}

        // 2. Tag filters → candidate sets (AND logic: intersect all tag sets)
        for (let i = 0; i < tagFilters.length; i++) {{
            const tag = tagFilters[i];
            if (tagIndex.has(tag)) {{
                candidateSets.push(tagIndex.get(tag));
                indexLookups++;
            }} else {{
                // Tag not in index → empty result
                candidateSets.push(new Int32Array(0));
                indexLookups++;
            }}
        }}

        // 3. Text query → token lookup + intersect token postings
        if (queryText) {{
            const queryTokens = tokenize(queryText);
            // For each token, find postings from token index (prefix match)
            for (let i = 0; i < queryTokens.length; i++) {{
                const token = queryTokens[i];
                const postings = findTokenPostings(token);
                candidateSets.push(postings);
                indexLookups++;
            }}
        }}

        // 4. Intersect all candidate sets
        let resultOrdinals;
        if (candidateSets.length === 0) {{
            // No filters → all scenarios
            resultOrdinals = allScenariosSet();
        }} else {{
            // Sort by size (smallest first) for efficient intersection
            candidateSets.sort(function(a, b) {{ return a.length - b.length; }});
            resultOrdinals = candidateSets[0];
            for (let i = 1; i < candidateSets.length; i++) {{
                // Check for cancellation between intersections
                if (cancelledQueries.has(requestId)) {{
                    cancelledQueries.delete(requestId);
                    return;
                }}
                resultOrdinals = intersectSorted(resultOrdinals, candidateSets[i]);
                if (resultOrdinals.length === 0) break;
            }}
        }}

        // 5. Derive feature ordinals from scenario ordinals
        const featureOrdinalSet = new Set();
        for (let i = 0; i < resultOrdinals.length; i++) {{
            featureOrdinalSet.add(scenarioToFeature[resultOrdinals[i]]);
        }}
        const featureOrdinals = Array.from(featureOrdinalSet).sort(function(a, b) {{ return a - b; }});

        // Check if stale (newer query arrived while we were computing)
        if (querySeq < latestQuerySeq) {{
            return; // Stale result, discard
        }}

        const evalMs = performance.now() - t0;

        // 6. Send RESULT
        self.postMessage({{
            type: 'RESULT',
            protocol: PROTOCOL_VERSION,
            requestId: requestId,
            querySeq: querySeq,
            timestamp: Date.now(),
            payload: {{
                scenarioOrdinals: Array.from(resultOrdinals),
                featureOrdinals: featureOrdinals,
                count: resultOrdinals.length,
                stats: {{
                    evalMs: evalMs,
                    candidateCount: candidateSets.length > 0 ? candidateSets[0].length : totalScenarios,
                    indexLookups: indexLookups
                }}
            }}
        }});

    }} catch (err) {{
        sendError(requestId, querySeq, 'QUERY failed: ' + err.message);
    }}
}}

// ============================================
// QUERY_CANCEL
// ============================================

function handleQueryCancel(msg) {{
    if (msg.requestId) {{
        cancelledQueries.add(msg.requestId);
    }}
}}

// ============================================
// Helper: Tokenize query text
// ============================================
// Mirrors the C# TokenizerService normalization:
// lowercase, split on delimiters, min length 2.

const DELIMITERS = /[\s_\-\/.@#\t\r\n()\[\]{{}}\:,;""']+/;

function tokenize(text) {{
    if (!text) return [];
    return text.toLowerCase()
        .split(DELIMITERS)
        .filter(function(t) {{ return t.length >= 2; }});
}}

// ============================================
// Helper: Find token postings (prefix match)
// ============================================
// Returns union of postings for all tokens that start with the query token.
// This enables type-ahead / partial matching.

function findTokenPostings(queryToken) {{
    // Exact match first
    if (tokenIndex.has(queryToken)) {{
        // Also check for prefix matches and union them
        const sets = [tokenIndex.get(queryToken)];
        // For short tokens (< 4 chars), don't do prefix expansion to avoid excessive results
        if (queryToken.length >= 4) {{
            tokenIndex.forEach(function(postings, key) {{
                if (key !== queryToken && key.startsWith(queryToken)) {{
                    sets.push(postings);
                }}
            }});
        }}
        if (sets.length === 1) return sets[0];
        return unionSorted(sets);
    }}

    // No exact match — try prefix match
    const prefixSets = [];
    tokenIndex.forEach(function(postings, key) {{
        if (key.startsWith(queryToken)) {{
            prefixSets.push(postings);
        }}
    }});

    if (prefixSets.length === 0) return new Int32Array(0);
    if (prefixSets.length === 1) return prefixSets[0];
    return unionSorted(prefixSets);
}}

// ============================================
// Helper: Sorted set intersection (galloping)
// ============================================
// Both inputs must be sorted Int32Arrays.
// Uses galloping (exponential search) for large size ratio.

function intersectSorted(a, b) {{
    if (a.length === 0 || b.length === 0) return new Int32Array(0);

    // Ensure a is the smaller set
    if (a.length > b.length) {{
        const tmp = a; a = b; b = tmp;
    }}

    const result = [];
    let j = 0;

    // If size ratio > 4, use galloping
    if (b.length > a.length * 4) {{
        for (let i = 0; i < a.length && j < b.length; i++) {{
            const target = a[i];
            // Gallop forward in b
            j = gallopSearch(b, target, j);
            if (j < b.length && b[j] === target) {{
                result.push(target);
                j++;
            }}
        }}
    }} else {{
        // Linear merge intersection
        let i = 0;
        while (i < a.length && j < b.length) {{
            if (a[i] < b[j]) {{
                i++;
            }} else if (a[i] > b[j]) {{
                j++;
            }} else {{
                result.push(a[i]);
                i++;
                j++;
            }}
        }}
    }}

    return new Int32Array(result);
}}

// ============================================
// Helper: Galloping (exponential) search
// ============================================
// Finds the position of target in sorted array, starting from startIdx.
// Returns the index where target would be or is located.

function gallopSearch(arr, target, startIdx) {{
    let lo = startIdx;
    let hi = startIdx + 1;

    // Exponential search phase
    while (hi < arr.length && arr[hi] < target) {{
        lo = hi;
        hi = Math.min(hi * 2, arr.length);
    }}

    // Binary search phase
    while (lo < hi) {{
        const mid = (lo + hi) >>> 1;
        if (arr[mid] < target) {{
            lo = mid + 1;
        }} else {{
            hi = mid;
        }}
    }}

    return lo;
}}

// ============================================
// Helper: Sorted set union (for prefix match)
// ============================================

function unionSorted(sets) {{
    if (sets.length === 0) return new Int32Array(0);
    if (sets.length === 1) return sets[0];

    // Merge all sets and deduplicate
    let total = 0;
    for (let i = 0; i < sets.length; i++) total += sets[i].length;

    const merged = new Int32Array(total);
    let idx = 0;
    for (let i = 0; i < sets.length; i++) {{
        const s = sets[i];
        for (let j = 0; j < s.length; j++) {{
            merged[idx++] = s[j];
        }}
    }}

    // Sort and deduplicate
    merged.sort();
    const deduped = [];
    for (let i = 0; i < merged.length; i++) {{
        if (i === 0 || merged[i] !== merged[i - 1]) {{
            deduped.push(merged[i]);
        }}
    }}

    return new Int32Array(deduped);
}}

// ============================================
// Helper: All scenarios set
// ============================================

function allScenariosSet() {{
    const all = new Int32Array(totalScenarios);
    for (let i = 0; i < totalScenarios; i++) all[i] = i;
    return all;
}}

// ============================================
// Helper: Send error
// ============================================

function sendError(requestId, querySeq, message) {{
    self.postMessage({{
        type: 'ERROR',
        protocol: PROTOCOL_VERSION,
        requestId: requestId || '',
        querySeq: querySeq || 0,
        timestamp: Date.now(),
        payload: {{
            message: message
        }}
    }});
}}
";
    }
}

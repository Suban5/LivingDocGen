namespace LivingDocGen.Generator.Services.Assets;

/// <summary>
/// Generates the runtime JavaScript for chunked output mode.
/// This includes:
/// - LRU cache for feature chunks
/// - Manifest loader (fetches and parses feature-manifest.json)
/// - Chunk fetcher (lazy-loads features/{featureId}.json on demand)
/// - Sidebar builder (renders sidebar from manifest entries instead of inline HTML)
/// - Integration with existing UI (search, filters, theme, keyboard shortcuts)
/// </summary>
public static class ChunkedRuntimeJavaScript
{
    /// <summary>
    /// Generates the complete chunked runtime JavaScript module.
    /// This replaces the lazy rendering system from legacy mode with
    /// a fetch-based chunk loading system backed by manifest metadata.
    /// </summary>
    /// <param name="lruCacheSize">Maximum number of feature chunks to keep in memory (default: 10).</param>
    /// <returns>JavaScript code wrapped in script tags.</returns>
    public static string Generate(int lruCacheSize = 10)
    {
        return $@"
    <script>
    // ============================================
    // CHUNKED RUNTIME — PR-3: Runtime Loader + Chunk Rendering
    // ============================================
    // Replaces legacy monolithic feature-data embedding with
    // manifest-driven on-demand chunk fetching and LRU caching.

    const CHUNKED_MODE = true;
    const LRU_CACHE_SIZE = {lruCacheSize};

    // ============================================
    // 1. LRU CACHE
    // ============================================
    // Bounded in-memory cache for fetched feature chunks.
    // Evicts least-recently-used entries when capacity is exceeded.

    class LRUCache {{
        constructor(capacity) {{
            this._capacity = capacity;
            this._map = new Map(); // insertion-order map; most-recent at end
        }}

        /** Returns the cached value or undefined. Promotes entry on hit. */
        get(key) {{
            if (!this._map.has(key)) return undefined;
            const value = this._map.get(key);
            // Move to end (most-recently-used)
            this._map.delete(key);
            this._map.set(key, value);
            return value;
        }}

        /** Stores a value. Evicts oldest entry if over capacity. */
        set(key, value) {{
            if (this._map.has(key)) {{
                this._map.delete(key);
            }} else if (this._map.size >= this._capacity) {{
                // Evict least-recently-used (first entry)
                const oldestKey = this._map.keys().next().value;
                this._map.delete(oldestKey);
                // Unmount evicted feature DOM to cap memory
                unmountFeature(oldestKey);
            }}
            this._map.set(key, value);
        }}

        has(key) {{ return this._map.has(key); }}
        get size() {{ return this._map.size; }}
        keys() {{ return this._map.keys(); }}
        clear() {{ this._map.clear(); }}
    }}

    const chunkCache = new LRUCache(LRU_CACHE_SIZE);

    /** Remove the rendered DOM body of an evicted feature to cap DOM size. */
    function unmountFeature(featureId) {{
        const container = document.getElementById('chunk-' + featureId);
        if (container) {{
            container.innerHTML = '<div class=""lazy-placeholder""><i class=""fas fa-spinner fa-spin""></i> Loading...</div>';
            container.setAttribute('data-loaded', 'false');
        }}
    }}

    // ============================================
    // 2. MANIFEST LOADER
    // ============================================
    // Fetches feature-manifest.json once on page load.
    // Provides the feature map used to build the sidebar and resolve chunk paths.

    let manifest = null;
    let manifestError = null;
    let manifestReady = false;
    const manifestReadyCallbacks = [];

    function onManifestReady(cb) {{
        if (manifestReady) {{ cb(manifest); return; }}
        manifestReadyCallbacks.push(cb);
    }}

    async function loadManifest() {{
        try {{
            if (typeof showLoader === 'function') showLoader('Loading manifest...', 0);
            // PR-2.5: Delegate to ContractLoader for schema version validation,
            // buildId tracking, and retry-with-backoff fetch strategy.
            manifest = await ContractLoader.loadManifest();
            manifestReady = true;
            console.log('✓ Manifest loaded: ' + manifest.totalFeatures + ' features, build ' + manifest.buildId);
            manifestReadyCallbacks.forEach(cb => cb(manifest));
            manifestReadyCallbacks.length = 0;
        }} catch (err) {{
            manifestError = err;
            console.error('✗ Failed to load manifest:', err);
            showDegradedBanner('Could not load feature manifest. The report may not function correctly.');
        }} finally {{
            if (typeof hideLoader === 'function') hideLoader();
        }}
    }}

    // ============================================
    // 3. CHUNK FETCHER
    // ============================================
    // Fetches individual feature chunks on demand (when selected in sidebar).
    // Returns cached chunk if available; otherwise fetches from network.

    /** In-flight fetch promises to prevent duplicate requests. */
    const inflightFetches = new Map();

    /**
     * Fetches (or retrieves from cache) a feature chunk by featureId.
     * @param {{string}} featureId - The deterministic feature ID from the manifest.
     * @returns {{Promise<object>}} The parsed feature chunk JSON object.
     */
    async function fetchChunk(featureId) {{
        // 1. Cache hit
        const cached = chunkCache.get(featureId);
        if (cached) {{
            console.log('↩ Cache hit: ' + featureId);
            return cached;
        }}

        // 2. Already in flight — wait for the same promise
        if (inflightFetches.has(featureId)) {{
            return inflightFetches.get(featureId);
        }}

        // 3. Resolve chunk path from manifest
        const entry = manifest?.featureMap?.find(e => e.featureId === featureId);
        if (!entry) {{
            throw new Error('Feature not found in manifest: ' + featureId);
        }}

        // PR-2.5: Delegate to ContractLoader for retry, schema validation,
        // hash integrity check, and buildId consistency guard.
        const fetchPromise = (async () => {{
            try {{
                const chunk = await ContractLoader.loadChunk(entry);
                chunkCache.set(featureId, chunk);
                console.log('✓ Chunk loaded: ' + chunk.name + ' (' + featureId + ')');
                return chunk;
            }} finally {{
                inflightFetches.delete(featureId);
            }}
        }})();

        inflightFetches.set(featureId, fetchPromise);
        return fetchPromise;
    }}

    // ============================================
    // 4. CHUNK RENDERER
    // ============================================
    // Mounts fetched chunk HTML into the main content area.

    /** Currently active feature ID. */
    let activeFeatureId = null;

    /**
     * Renders a feature chunk into the content area.
     * Creates or reuses the container div for the feature.
     */
    function renderChunk(featureId, chunk) {{
        const mainContent = document.getElementById('main-content');
        if (!mainContent) return;

        // Hide all other feature containers
        mainContent.querySelectorAll('.chunk-container').forEach(el => {{
            el.classList.add('feature-hidden');
        }});

        // Find or create container
        let container = document.getElementById('chunk-' + featureId);
        if (!container) {{
            container = document.createElement('div');
            container.id = 'chunk-' + featureId;
            container.className = 'chunk-container';
            container.setAttribute('data-feature-id', featureId);
            mainContent.appendChild(container);
        }}

        // Populate if not already loaded
        if (container.getAttribute('data-loaded') !== 'true') {{
            container.innerHTML = chunk.html;
            container.setAttribute('data-loaded', 'true');
        }}

        // Show
        container.classList.remove('feature-hidden');
        activeFeatureId = featureId;

        // Scroll main content to top
        mainContent.scrollTop = 0;
    }}

    // ============================================
    // 5. SIDEBAR BUILDER FROM MANIFEST
    // ============================================
    // Builds the sidebar navigation tree from manifest featureMap entries
    // using the same folder-tree UX as legacy mode.

    /**
     * Mapping from manifest featureId → sidebar display index.
     * Used to bridge chunked IDs with sidebar item data attributes.
     */
    const featureIdIndex = new Map();

    /**
     * Builds the sidebar tree from manifest data and inserts it into the DOM.
     */
    function buildSidebarFromManifest(m) {{
        const nav = document.getElementById('sidebar-nav');
        if (!nav) return;

        // Update total count in header
        const totalSpan = document.querySelector('.feature-total');
        if (totalSpan) totalSpan.textContent = '(' + m.totalFeatures + ')';

        // Build folder tree from file paths
        const root = {{ name: 'Features', subFolders: {{}}, features: [] }};

        m.featureMap.forEach((entry, idx) => {{
            featureIdIndex.set(entry.featureId, idx);

            const filePath = (entry.filePath || '').replace(/\\\\/g, '/');
            const segments = filePath.split('/').filter(Boolean);
            // Remove the filename segment
            const fileName = segments.pop() || entry.name;

            // Strip common base path (heuristic: first segment is usually root like 'Features')
            // We use all remaining segments as folder hierarchy
            let node = root;
            segments.forEach(seg => {{
                if (!node.subFolders[seg]) {{
                    node.subFolders[seg] = {{ name: seg, subFolders: {{}}, features: [] }};
                }}
                node = node.subFolders[seg];
            }});

            node.features.push({{ entry, idx, fileName }});
        }});

        // Render tree into HTML
        nav.innerHTML = renderFolderNode(root, 0, true);

        // Select first feature automatically
        if (m.featureMap.length > 0) {{
            selectChunkedFeature(m.featureMap[0].featureId);
        }}
    }}

    /** Status icon HTML helper. */
    function statusIconHtml(status) {{
        switch (status) {{
            case 'passed':  return '<i class=""fas fa-check-circle""></i>';
            case 'failed':  return '<i class=""fas fa-times-circle""></i>';
            case 'skipped': return '<i class=""fas fa-minus-circle""></i>';
            default:        return '<i class=""fas fa-circle""></i>';
        }}
    }}

    /** Renders a folder node and its children recursively. */
    function renderFolderNode(node, depth, isRoot) {{
        let html = '';

        if (isRoot) {{
            // Root: render features then subfolders directly (no wrapper)
            node.features.forEach(f => {{
                html += renderFeatureItem(f, 0);
            }});
            Object.keys(node.subFolders).sort((a, b) => a.localeCompare(b, undefined, {{ sensitivity: 'base' }})).forEach(key => {{
                html += renderFolder(node.subFolders[key], 0);
            }});
        }}

        return html;
    }}

    /** Renders a collapsible folder. */
    function renderFolder(folder, depth) {{
        const folderId = 'folder-' + folder.name.toLowerCase().replace(/[\\s\\/\\\\]/g, '-');
        const levelClass = 'folder-level-' + Math.min(depth, 5);
        const totalCount = countFeatures(folder);
        const isExpanded = depth < 4;
        const expandedClass = isExpanded ? '' : ' collapsed';
        const ariaExpanded = isExpanded ? 'true' : 'false';
        const hasSubFolders = Object.keys(folder.subFolders).length > 0;
        const folderIcon = hasSubFolders ? 'fa-folder-tree' : 'fa-folder';

        let html = '<div class=""folder ' + levelClass + expandedClass + '"" role=""treeitem"" aria-expanded=""' + ariaExpanded + '"" data-depth=""' + depth + '"">';
        html += '<div class=""folder-header"" onclick=""toggleFolder(\'' + folderId + '\')"" tabindex=""0"" onkeydown=""handleFolderKeydown(event, \'' + folderId + '\')"" role=""button"" aria-label=""Folder: ' + escapeHtml(folder.name) + '"">';
        html += '<i class=""fas ' + folderIcon + ' folder-icon""></i>';
        html += '<span class=""folder-name"">' + escapeHtml(folder.name) + '</span>';
        html += '<span class=""folder-count"">(' + totalCount + ')</span>';
        html += '<i class=""fas fa-chevron-down folder-chevron""></i>';
        html += '</div>';
        html += '<div class=""folder-content"" id=""' + folderId + '"">';

        // Features first, then subfolders
        folder.features.forEach(f => {{
            html += renderFeatureItem(f, depth + 1);
        }});
        Object.keys(folder.subFolders).sort((a, b) => a.localeCompare(b, undefined, {{ sensitivity: 'base' }})).forEach(key => {{
            html += renderFolder(folder.subFolders[key], depth + 1);
        }});

        html += '</div></div>';
        return html;
    }}

    /** Counts total features in a folder tree recursively. */
    function countFeatures(folder) {{
        let count = folder.features.length;
        Object.values(folder.subFolders).forEach(sub => {{ count += countFeatures(sub); }});
        return count;
    }}

    /** Renders a single feature item in the sidebar. */
    function renderFeatureItem(f, depth) {{
        const entry = f.entry;
        const levelClass = 'feature-level-' + Math.min(depth, 5);
        const isFirst = f.idx === 0 ? ' active' : '';

        // Display: filename without .feature extension
        let displayName = f.fileName || entry.name;
        if (displayName.toLowerCase().endsWith('.feature')) {{
            displayName = displayName.substring(0, displayName.length - 8);
        }}

        return '<div class=""feature-item ' + levelClass + isFirst + '"" '
            + 'data-feature-id=""' + entry.featureId + '"" '
            + 'data-status=""' + entry.status + '"" '
            + 'data-search=""' + escapeHtml((entry.name + ' ' + displayName).toLowerCase()) + '"" '
            + 'onclick=""selectChunkedFeature(\'' + entry.featureId + '\')"" '
            + 'tabindex=""0"" role=""treeitem"" '
            + 'title=""' + escapeHtml(entry.name) + '"" '
            + 'onkeydown=""handleChunkedFeatureKeydown(event, \'' + entry.featureId + '\')"" '
            + 'aria-label=""Feature: ' + escapeHtml(entry.name) + '"">'
            + '<span class=""feature-status status-' + entry.status + '"">' + statusIconHtml(entry.status) + '</span>'
            + '<span class=""feature-name"">' + escapeHtml(displayName) + '</span>'
            + '</div>';
    }}

    /** Minimal HTML escaping for safe attribute/text insertion. */
    function escapeHtml(text) {{
        if (!text) return '';
        return text.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;')
                   .replace(/""/g, '&quot;').replace(/'/g, '&#39;');
    }}

    // ============================================
    // 6. FEATURE SELECTION (CHUNKED MODE)
    // ============================================

    /**
     * Selects a feature: fetches its chunk (or uses cache), renders it,
     * and updates the sidebar active state.
     */
    async function selectChunkedFeature(featureId) {{
        // Show loader for feedback
        if (typeof showLoader === 'function') showLoader('Loading feature...', 30);

        try {{
            // Fetch chunk (cache-backed)
            const chunk = await fetchChunk(featureId);

            // Render into main content
            renderChunk(featureId, chunk);

            // Update sidebar active state
            document.querySelectorAll('.feature-item').forEach(item => {{
                item.classList.remove('active');
            }});
            const activeItem = document.querySelector('.feature-item[data-feature-id=""' + featureId + '""]');
            if (activeItem) {{
                activeItem.classList.add('active');
                // Expand parent folders
                expandParentFolders(activeItem);
            }}

            // Save last viewed
            localStorage.setItem('bdd-last-feature', featureId);
        }} catch (err) {{
            console.error('Failed to load feature:', featureId, err);
            showDegradedBanner('Failed to load feature. Check that chunk files are accessible.');
        }} finally {{
            if (typeof hideLoader === 'function') hideLoader();
        }}
    }}

    /** Keyboard handler for chunked feature items. */
    function handleChunkedFeatureKeydown(event, featureId) {{
        if (event.key === 'Enter' || event.key === ' ') {{
            event.preventDefault();
            selectChunkedFeature(featureId);
        }}
    }}

    // ============================================
    // 7. IDLE PREFETCH
    // ============================================
    // Prefetches adjacent features in sidebar during idle time
    // to reduce perceived latency on next click.

    function schedulePrefetch() {{
        if (!manifest || !activeFeatureId) return;

        const currentIdx = manifest.featureMap.findIndex(e => e.featureId === activeFeatureId);
        if (currentIdx < 0) return;

        const candidates = [];
        // Next and previous features in sidebar order
        if (currentIdx + 1 < manifest.featureMap.length) candidates.push(manifest.featureMap[currentIdx + 1].featureId);
        if (currentIdx - 1 >= 0) candidates.push(manifest.featureMap[currentIdx - 1].featureId);

        candidates.forEach(id => {{
            if (!chunkCache.has(id) && !inflightFetches.has(id)) {{
                const idleCallback = typeof requestIdleCallback !== 'undefined'
                    ? requestIdleCallback
                    : function(cb) {{ setTimeout(cb, 200); }};

                idleCallback(() => {{
                    fetchChunk(id).catch(() => {{}}); // Silent prefetch
                }}, {{ timeout: 2000 }});
            }}
        }});
    }}

    // ============================================
    // 8. DEGRADED MODE BANNER
    // ============================================

    function showDegradedBanner(message) {{
        let banner = document.getElementById('degraded-banner');
        if (!banner) {{
            banner = document.createElement('div');
            banner.id = 'degraded-banner';
            banner.style.cssText = 'position:fixed;top:0;left:0;right:0;z-index:10000;'
                + 'background:#d32f2f;color:white;padding:12px 20px;text-align:center;font-size:14px;';
            banner.innerHTML = '<i class=""fas fa-exclamation-triangle""></i> <span></span> '
                + '<button onclick=""this.parentElement.remove()"" style=""background:none;border:1px solid white;'
                + 'color:white;padding:4px 12px;margin-left:16px;border-radius:4px;cursor:pointer;"">Dismiss</button>';
            document.body.prepend(banner);
        }}
        banner.querySelector('span').textContent = message;
    }}

    // ============================================
    // 9. SEARCH & FILTER — DELEGATED TO SEARCH BRIDGE (PR-4)
    // ============================================
    // Search/filter is now handled by the SearchBridge (search-bridge.js)
    // which uses a Web Worker for off-main-thread index-backed query evaluation.
    // The bridge provides:
    // - Worker-based set-intersection filtering (token + status + tag)
    // - Main-thread synchronous fallback if Worker is unavailable
    // - Delta DOM updates (only toggles changed sidebar items)
    // - Query debounce and cancellation
    //
    // applyChunkedFilters() and applyAllFilters() are defined in SearchBridge.
    // See SearchBridgeJavaScript.cs for implementation.

    // ============================================
    // 10. TAG POPULATION FROM MANIFEST
    // ============================================

    function populateTagsFromManifest(m) {{
        const tagFilter = document.getElementById('tag-filter');
        if (!tagFilter) return;

        const allTags = new Set();
        m.featureMap.forEach(entry => {{
            (entry.tags || []).forEach(tag => allTags.add(tag));
        }});

        const sorted = Array.from(allTags).sort((a, b) => a.localeCompare(b, undefined, {{ sensitivity: 'base' }}));
        sorted.forEach(tag => {{
            const opt = document.createElement('option');
            opt.value = tag;
            opt.textContent = tag;
            tagFilter.appendChild(opt);
        }});
    }}

    // ============================================
    // 11. INITIALIZATION
    // ============================================

    document.addEventListener('DOMContentLoaded', async function() {{
        // Load manifest first
        await loadManifest();

        if (!manifest) return; // Degraded banner already shown

        // Build sidebar from manifest
        buildSidebarFromManifest(manifest);

        // Populate tag dropdown from manifest
        populateTagsFromManifest(manifest);

        // Initialize search system (Web Worker + index loading)
        // This is non-blocking — the worker initializes in the background.
        // While it initializes, manifest-only fallback filtering is available.
        initSearchSystem().catch(function(err) {{
            console.warn('Search system init failed, manifest-only fallback active:', err);
        }});

        // Restore last viewed feature (if any)
        const lastFeature = localStorage.getItem('bdd-last-feature');
        if (lastFeature) {{
            const exists = manifest.featureMap.some(e => e.featureId === lastFeature);
            if (exists) {{
                selectChunkedFeature(lastFeature);
            }}
        }}

        // Schedule prefetch after initial load
        setTimeout(schedulePrefetch, 1000);
    }});

    // Override legacy selectFeature for chunked mode
    const _legacySelectFeature = typeof selectFeature === 'function' ? selectFeature : null;
    function selectFeature(featureIdOrChunkedId) {{
        if (CHUNKED_MODE && manifest) {{
            const entry = manifest.featureMap.find(e => e.featureId === featureIdOrChunkedId);
            if (entry) {{
                selectChunkedFeature(featureIdOrChunkedId);
                return;
            }}
        }}
        if (_legacySelectFeature) _legacySelectFeature(featureIdOrChunkedId);
    }}

    // Note: applyAllFilters and applyChunkedFilters are overridden by
    // SearchBridgeJavaScript (PR-4) which is loaded after this script.

    </script>";
    }
}

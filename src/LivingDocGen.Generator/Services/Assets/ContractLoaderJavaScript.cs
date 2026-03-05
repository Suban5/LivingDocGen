using LivingDocGen.Generator.Models.Contracts;

namespace LivingDocGen.Generator.Services.Assets;

/// <summary>
/// Generates the ContractLoader JavaScript module for chunked output mode (PR-2.5).
///
/// Provides runtime contract validation that mirrors the C# <see cref="ContractVersion"/>
/// and <see cref="ContractValidator"/> logic:
///
///   1. Schema version guards — major-must-match + minor-gte-minimum (mirrors <see cref="ContractVersion.IsCompatible"/>)
///   2. BuildId consistency — detects partial deploys / stale CDN artifacts
///   3. SHA-256 hash integrity — validates chunks/index against manifest-recorded hashes
///   4. Structured fallback strategy — retry with backoff, per-artifact error classification
///   5. Diagnostics — structured error codes, version logging, runtime state machine
///
/// This script is loaded BEFORE <see cref="ChunkedRuntimeJavaScript"/> and
/// <see cref="SearchBridgeJavaScript"/> so those modules can use the ContractLoader API.
/// </summary>
public static class ContractLoaderJavaScript
{
    /// <summary>
    /// Generates the ContractLoader JavaScript module.
    /// Schema version constants are injected from <see cref="ContractVersion"/>
    /// to guarantee build-time / runtime parity.
    /// </summary>
    /// <param name="maxRetries">Maximum retry attempts for fetch failures (default: 2).</param>
    /// <param name="baseRetryDelayMs">Base delay in ms for exponential backoff (default: 500).</param>
    /// <returns>JavaScript code wrapped in script tags.</returns>
    public static string Generate(int maxRetries = 2, int baseRetryDelayMs = 500)
    {
        return $@"
    <script>
    // ============================================
    // CONTRACT LOADER — PR-2.5: Runtime Compatibility Layer
    // ============================================
    // Provides schema version validation, hash integrity checking,
    // buildId consistency guards, and structured fallback strategy
    // for all chunked output contracts (manifest, index, chunks).
    //
    // Mirrors C# ContractVersion.IsCompatible() and ContractValidator logic.

    const ContractLoader = (function() {{

        // ============================================
        // 1. VERSION CONSTANTS (injected from C# ContractVersion)
        // ============================================

        const SCHEMA_VERSION = '{ContractVersion.SchemaVersion}';
        const COMPATIBILITY_MIN_VERSION = '{ContractVersion.CompatibilityMinVersion}';
        const GENERATOR_VERSION = '{ContractVersion.GeneratorVersion}';

        // ============================================
        // 2. CONFIGURATION
        // ============================================

        const MAX_RETRIES = {maxRetries};
        const BASE_RETRY_DELAY_MS = {baseRetryDelayMs};

        // ============================================
        // 3. ERROR CLASSIFICATION
        // ============================================

        const ErrorCode = Object.freeze({{
            NETWORK_ERROR: 'NETWORK_ERROR',
            VERSION_INCOMPATIBLE: 'VERSION_INCOMPATIBLE',
            VERSION_MISMATCH_MINOR: 'VERSION_MISMATCH_MINOR',
            HASH_MISMATCH: 'HASH_MISMATCH',
            BUILD_MISMATCH: 'BUILD_MISMATCH',
            PARSE_ERROR: 'PARSE_ERROR',
            NOT_FOUND: 'NOT_FOUND'
        }});

        // ============================================
        // 4. RUNTIME STATE
        // ============================================

        const RuntimeState = Object.freeze({{
            LOADING: 'LOADING',
            READY: 'READY',
            DEGRADED: 'DEGRADED'
        }});

        let currentState = RuntimeState.LOADING;
        const errors = []; // structured error log

        function setState(newState) {{
            const prev = currentState;
            currentState = newState;
            console.log('[ContractLoader] State: ' + prev + ' → ' + newState);
        }}

        function logError(code, message, context) {{
            const entry = {{
                code: code,
                message: message,
                context: context || {{}},
                timestamp: new Date().toISOString()
            }};
            errors.push(entry);
            console.error('[ContractLoader] ' + code + ': ' + message, context || '');
            return entry;
        }}

        // ============================================
        // 5. SCHEMA VERSION GUARD
        // ============================================
        // Mirrors C# ContractVersion.IsCompatible():
        //   - Major version must match exactly
        //   - Minor version must be >= minimum

        function parseVersion(versionStr) {{
            if (!versionStr || typeof versionStr !== 'string') return null;
            const parts = versionStr.split('.');
            if (parts.length !== 2) return null;
            const major = parseInt(parts[0], 10);
            const minor = parseInt(parts[1], 10);
            if (isNaN(major) || isNaN(minor)) return null;
            return {{ major: major, minor: minor }};
        }}

        /**
         * Checks if a contract's schema version is compatible with this runtime.
         * @param {{string}} schemaVersion - The schemaVersion from the contract.
         * @returns {{{{ compatible: boolean, warning: boolean, message: string }}}}
         */
        function checkSchemaCompatibility(schemaVersion) {{
            const contract = parseVersion(schemaVersion);
            const minimum = parseVersion(COMPATIBILITY_MIN_VERSION);
            const current = parseVersion(SCHEMA_VERSION);

            if (!contract) {{
                return {{
                    compatible: false,
                    warning: false,
                    message: 'Invalid schema version format: ' + schemaVersion
                }};
            }}

            if (!minimum || !current) {{
                // Should never happen — constants are injected at build time
                return {{ compatible: true, warning: false, message: 'OK (version check skipped)' }};
            }}

            // Major must match
            if (contract.major !== minimum.major) {{
                return {{
                    compatible: false,
                    warning: false,
                    message: 'Incompatible major version: contract=' + schemaVersion
                        + ', runtime=' + SCHEMA_VERSION
                        + '. This report was generated with a different major version of LivingDocGen.'
                }};
            }}

            // Minor must be >= minimum
            if (contract.minor < minimum.minor) {{
                return {{
                    compatible: false,
                    warning: false,
                    message: 'Schema version too old: contract=' + schemaVersion
                        + ', minimum=' + COMPATIBILITY_MIN_VERSION
                        + '. Please regenerate the report with the latest version.'
                }};
            }}

            // Newer minor than current runtime — still compatible, but warn
            if (contract.minor > current.minor) {{
                return {{
                    compatible: true,
                    warning: true,
                    message: 'Contract has newer minor version: contract=' + schemaVersion
                        + ', runtime=' + SCHEMA_VERSION
                        + '. Some features may not be fully supported.'
                }};
            }}

            return {{ compatible: true, warning: false, message: 'OK' }};
        }}

        /**
         * Validates a contract object's schema version. Throws on hard incompatibility.
         * @param {{object}} contract - The deserialized contract (manifest/index/chunk).
         * @param {{string}} artifactType - Label for error messages ('manifest', 'index', 'chunk').
         */
        function validateSchemaVersion(contract, artifactType) {{
            if (!contract || !contract.schemaVersion) {{
                logError(ErrorCode.VERSION_INCOMPATIBLE,
                    artifactType + ' missing schemaVersion field',
                    {{ artifactType: artifactType }});
                return; // Allow loading — old contracts may lack this field
            }}

            const result = checkSchemaCompatibility(contract.schemaVersion);

            if (!result.compatible) {{
                logError(ErrorCode.VERSION_INCOMPATIBLE, result.message,
                    {{ artifactType: artifactType, schemaVersion: contract.schemaVersion }});
                throw new Error('[ContractLoader] ' + result.message);
            }}

            if (result.warning) {{
                logError(ErrorCode.VERSION_MISMATCH_MINOR, result.message,
                    {{ artifactType: artifactType, schemaVersion: contract.schemaVersion }});
                showWarningBanner(result.message);
            }}
        }}

        // ============================================
        // 6. BUILD ID CONSISTENCY
        // ============================================

        let expectedBuildId = null;

        /**
         * Validates that a contract's buildId matches the manifest's buildId.
         * First call (manifest) sets the expected buildId.
         * @param {{object}} contract - The deserialized contract.
         * @param {{string}} artifactType - Label for error messages.
         */
        function validateBuildId(contract, artifactType) {{
            if (!contract || !contract.buildId) return;

            if (!expectedBuildId) {{
                // First loaded artifact (manifest) sets the expected buildId
                expectedBuildId = contract.buildId;
                return;
            }}

            if (contract.buildId !== expectedBuildId) {{
                logError(ErrorCode.BUILD_MISMATCH,
                    artifactType + ' buildId mismatch: expected=' + expectedBuildId
                        + ', actual=' + contract.buildId
                        + '. Artifacts may be from different builds (partial deploy?).',
                    {{ artifactType: artifactType, expected: expectedBuildId, actual: contract.buildId }});
                showWarningBanner('Build ID mismatch detected. Report data may be inconsistent. '
                    + 'Please regenerate the report.');
            }}
        }}

        // ============================================
        // 7. SHA-256 HASH INTEGRITY
        // ============================================
        // Uses Web Crypto API (SubtleCrypto.digest) for hash validation.
        // Matches C# ContractHashValidator.ComputeHash() output.

        /**
         * Computes SHA-256 hash of a string using Web Crypto API.
         * @param {{string}} content - The content to hash.
         * @returns {{Promise<string>}} Hex-encoded SHA-256 hash.
         */
        async function computeHash(content) {{
            try {{
                const encoder = new TextEncoder();
                const data = encoder.encode(content);
                const hashBuffer = await crypto.subtle.digest('SHA-256', data);
                const hashArray = Array.from(new Uint8Array(hashBuffer));
                return hashArray.map(b => b.toString(16).padStart(2, '0')).join('');
            }} catch (err) {{
                console.warn('[ContractLoader] crypto.subtle unavailable, skipping hash validation:', err.message);
                return null; // Skip validation if crypto API not available (e.g., non-HTTPS)
            }}
        }}

        /**
         * Validates a fetched artifact's content hash against an expected hash.
         * Non-blocking — logs warning but does not throw on mismatch.
         * @param {{string}} rawJson - The raw JSON string of the artifact.
         * @param {{string}} expectedHash - The expected SHA-256 hash from the manifest.
         * @param {{string}} artifactType - Label for error messages.
         */
        async function validateHash(rawJson, expectedHash, artifactType) {{
            if (!expectedHash || !rawJson) return;

            const actualHash = await computeHash(rawJson);
            if (actualHash === null) return; // Crypto API unavailable

            if (actualHash !== expectedHash) {{
                logError(ErrorCode.HASH_MISMATCH,
                    artifactType + ' hash mismatch: expected=' + expectedHash.substring(0, 16) + '...'
                        + ', actual=' + actualHash.substring(0, 16) + '...'
                        + '. Content may have been corrupted or modified.',
                    {{ artifactType: artifactType, expected: expectedHash, actual: actualHash }});
                showWarningBanner('Integrity check failed for ' + artifactType
                    + '. Data may be corrupted.');
            }}
        }}

        // ============================================
        // 8. FETCH WITH RETRY
        // ============================================

        /**
         * Fetches a URL with exponential backoff retry.
         * @param {{string}} url - The URL to fetch.
         * @param {{string}} artifactType - Label for error messages.
         * @returns {{Promise<Response>}} The fetch response.
         */
        async function fetchWithRetry(url, artifactType) {{
            let lastError = null;

            for (let attempt = 0; attempt <= MAX_RETRIES; attempt++) {{
                try {{
                    const response = await fetch(url);
                    if (response.ok) return response;

                    if (response.status === 404) {{
                        logError(ErrorCode.NOT_FOUND,
                            artifactType + ' not found: ' + url,
                            {{ url: url, status: 404 }});
                        throw new Error(artifactType + ' not found (404): ' + url);
                    }}

                    lastError = new Error(artifactType + ' fetch failed (' + response.status + '): ' + url);
                }} catch (err) {{
                    lastError = err;
                }}

                if (attempt < MAX_RETRIES) {{
                    const delay = BASE_RETRY_DELAY_MS * Math.pow(2, attempt);
                    console.warn('[ContractLoader] Retry ' + (attempt + 1) + '/' + MAX_RETRIES
                        + ' for ' + artifactType + ' in ' + delay + 'ms');
                    await new Promise(resolve => setTimeout(resolve, delay));
                }}
            }}

            logError(ErrorCode.NETWORK_ERROR,
                artifactType + ' fetch failed after ' + (MAX_RETRIES + 1) + ' attempts: ' + lastError?.message,
                {{ url: url, attempts: MAX_RETRIES + 1 }});
            throw lastError;
        }}

        // ============================================
        // 9. CONTRACT LOADING API
        // ============================================

        /**
         * Loads and validates the feature manifest.
         * @returns {{Promise<object>}} The validated manifest object.
         */
        async function loadManifest() {{
            const response = await fetchWithRetry('feature-manifest.json', 'manifest');
            const rawJson = await response.text();

            let parsed;
            try {{
                parsed = JSON.parse(rawJson);
            }} catch (err) {{
                logError(ErrorCode.PARSE_ERROR, 'Failed to parse manifest JSON: ' + err.message, {{}});
                throw err;
            }}

            // Schema version guard
            validateSchemaVersion(parsed, 'manifest');

            // Set expected buildId (first artifact loaded)
            validateBuildId(parsed, 'manifest');

            // Log version info
            console.log('[ContractLoader] Manifest validated: schema=' + (parsed.schemaVersion || 'unknown')
                + ', generator=' + (parsed.generatorVersion || 'unknown')
                + ', build=' + (parsed.buildId || 'unknown'));

            setState(RuntimeState.READY);
            return parsed;
        }}

        /**
         * Loads and validates the feature index.
         * @param {{object}} manifestRef - The manifest object (for indexHash validation).
         * @returns {{Promise<object>}} The validated index object.
         */
        async function loadIndex(manifestRef) {{
            const indexUrl = (manifestRef && manifestRef.indexFile) || 'feature-index.json';
            const response = await fetchWithRetry(indexUrl, 'index');
            const rawJson = await response.text();

            let parsed;
            try {{
                parsed = JSON.parse(rawJson);
            }} catch (err) {{
                logError(ErrorCode.PARSE_ERROR, 'Failed to parse index JSON: ' + err.message, {{}});
                throw err;
            }}

            // Schema version guard
            validateSchemaVersion(parsed, 'index');

            // BuildId consistency
            validateBuildId(parsed, 'index');

            // Hash integrity (async, non-blocking)
            if (manifestRef && manifestRef.indexHash) {{
                validateHash(rawJson, manifestRef.indexHash, 'index').catch(function() {{}});
            }}

            return parsed;
        }}

        /**
         * Loads and validates a feature chunk.
         * @param {{object}} manifestEntry - The manifest entry for this chunk (chunkFile, chunkHash, featureId).
         * @returns {{Promise<object>}} The validated chunk object.
         */
        async function loadChunk(manifestEntry) {{
            if (!manifestEntry || !manifestEntry.chunkFile) {{
                throw new Error('Invalid manifest entry: missing chunkFile');
            }}

            const response = await fetchWithRetry(manifestEntry.chunkFile, 'chunk (' + manifestEntry.name + ')');
            const rawJson = await response.text();

            let parsed;
            try {{
                parsed = JSON.parse(rawJson);
            }} catch (err) {{
                logError(ErrorCode.PARSE_ERROR,
                    'Failed to parse chunk JSON for ' + manifestEntry.name + ': ' + err.message, {{}});
                throw err;
            }}

            // Schema version guard
            validateSchemaVersion(parsed, 'chunk');

            // BuildId consistency
            validateBuildId(parsed, 'chunk');

            // Hash integrity (async, non-blocking — compare chunk content hash with manifest entry)
            if (manifestEntry.chunkHash) {{
                validateHash(rawJson, manifestEntry.chunkHash, 'chunk (' + manifestEntry.name + ')').catch(function() {{}});
            }}

            return parsed;
        }}

        // ============================================
        // 10. WARNING BANNER
        // ============================================

        function showWarningBanner(message) {{
            let banner = document.getElementById('contract-warning-banner');
            if (!banner) {{
                banner = document.createElement('div');
                banner.id = 'contract-warning-banner';
                banner.style.cssText = 'position:fixed;top:0;left:0;right:0;z-index:9999;'
                    + 'background:#ff9800;color:#000;padding:10px 20px;text-align:center;font-size:13px;';
                banner.innerHTML = '<i class=""fas fa-exclamation-triangle""></i> <span></span> '
                    + '<button onclick=""this.parentElement.remove()"" style=""background:none;border:1px solid #000;'
                    + 'color:#000;padding:3px 10px;margin-left:12px;border-radius:4px;cursor:pointer;font-size:12px;"">Dismiss</button>';
                document.body.prepend(banner);
            }}
            banner.querySelector('span').textContent = message;
        }}

        // ============================================
        // 11. DIAGNOSTICS API
        // ============================================

        function getDiagnostics() {{
            return {{
                state: currentState,
                schemaVersion: SCHEMA_VERSION,
                compatibilityMinVersion: COMPATIBILITY_MIN_VERSION,
                generatorVersion: GENERATOR_VERSION,
                expectedBuildId: expectedBuildId,
                errors: errors.slice(),
                errorCount: errors.length
            }};
        }}

        // ============================================
        // PUBLIC API
        // ============================================

        return {{
            // Loading API
            loadManifest: loadManifest,
            loadIndex: loadIndex,
            loadChunk: loadChunk,

            // Validation helpers (exposed for testing and external use)
            checkSchemaCompatibility: checkSchemaCompatibility,
            computeHash: computeHash,
            validateHash: validateHash,

            // State & diagnostics
            getDiagnostics: getDiagnostics,
            get state() {{ return currentState; }},
            get errors() {{ return errors.slice(); }},

            // Constants (exposed for testing)
            SCHEMA_VERSION: SCHEMA_VERSION,
            COMPATIBILITY_MIN_VERSION: COMPATIBILITY_MIN_VERSION,
            GENERATOR_VERSION: GENERATOR_VERSION,

            // Enums
            ErrorCode: ErrorCode,
            RuntimeState: RuntimeState
        }};
    }})();

    // Make diagnostics available globally
    window.getContractDiagnostics = ContractLoader.getDiagnostics;

    </script>";
    }
}

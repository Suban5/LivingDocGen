using Xunit;
using LivingDocGen.Generator.Services.Assets;
using LivingDocGen.Generator.Models.Contracts;

namespace LivingDocGen.Generator.Tests.Services.Assets;

/// <summary>
/// Tests for ContractLoaderJavaScript (PR-2.5: Runtime Compatibility Layer).
/// Verifies the generated JavaScript contains all required validation components
/// and that version constants stay in sync with C# ContractVersion.
/// </summary>
public class ContractLoaderJavaScriptTests
{
    // ===== Basic Structure =====

    [Fact]
    public void Generate_ReturnsNonEmptyString()
    {
        var result = ContractLoaderJavaScript.Generate();

        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public void Generate_ContainsScriptTags()
    {
        var result = ContractLoaderJavaScript.Generate();

        Assert.Contains("<script>", result);
        Assert.Contains("</script>", result);
    }

    [Fact]
    public void Generate_ContainsContractLoaderIIFE()
    {
        var result = ContractLoaderJavaScript.Generate();

        Assert.Contains("const ContractLoader = (function()", result);
    }

    // ===== Version Constants Sync with C# =====

    [Fact]
    public void Generate_SchemaVersion_MatchesCSharpContractVersion()
    {
        var result = ContractLoaderJavaScript.Generate();

        Assert.Contains($"const SCHEMA_VERSION = '{ContractVersion.SchemaVersion}'", result);
    }

    [Fact]
    public void Generate_CompatibilityMinVersion_MatchesCSharpContractVersion()
    {
        var result = ContractLoaderJavaScript.Generate();

        Assert.Contains($"const COMPATIBILITY_MIN_VERSION = '{ContractVersion.CompatibilityMinVersion}'", result);
    }

    [Fact]
    public void Generate_GeneratorVersion_MatchesCSharpContractVersion()
    {
        var result = ContractLoaderJavaScript.Generate();

        Assert.Contains($"const GENERATOR_VERSION = '{ContractVersion.GeneratorVersion}'", result);
    }

    [Fact]
    public void Generate_VersionConstants_AreNotEmpty()
    {
        // Guard: Ensure C# constants are not empty/null (they would break the JS)
        Assert.False(string.IsNullOrWhiteSpace(ContractVersion.SchemaVersion),
            "ContractVersion.SchemaVersion must not be empty");
        Assert.False(string.IsNullOrWhiteSpace(ContractVersion.CompatibilityMinVersion),
            "ContractVersion.CompatibilityMinVersion must not be empty");
        Assert.False(string.IsNullOrWhiteSpace(ContractVersion.GeneratorVersion),
            "ContractVersion.GeneratorVersion must not be empty");
    }

    // ===== Schema Version Guard =====

    [Fact]
    public void Generate_ContainsParseVersionFunction()
    {
        var result = ContractLoaderJavaScript.Generate();

        Assert.Contains("function parseVersion(versionStr)", result);
    }

    [Fact]
    public void Generate_ContainsCheckSchemaCompatibilityFunction()
    {
        var result = ContractLoaderJavaScript.Generate();

        Assert.Contains("function checkSchemaCompatibility(schemaVersion)", result);
    }

    [Fact]
    public void Generate_SchemaCompatibility_ChecksMajorVersionMatch()
    {
        var result = ContractLoaderJavaScript.Generate();

        // Mirror of C# IsCompatible: major must match
        Assert.Contains("contract.major !== minimum.major", result);
    }

    [Fact]
    public void Generate_SchemaCompatibility_ChecksMinorVersionGte()
    {
        var result = ContractLoaderJavaScript.Generate();

        // Mirror of C# IsCompatible: minor must be >= minimum
        Assert.Contains("contract.minor < minimum.minor", result);
    }

    [Fact]
    public void Generate_SchemaCompatibility_WarnsOnNewerMinorVersion()
    {
        var result = ContractLoaderJavaScript.Generate();

        Assert.Contains("contract.minor > current.minor", result);
        Assert.Contains("warning: true", result);
    }

    [Fact]
    public void Generate_ContainsValidateSchemaVersionFunction()
    {
        var result = ContractLoaderJavaScript.Generate();

        Assert.Contains("function validateSchemaVersion(contract, artifactType)", result);
    }

    // ===== BuildId Consistency =====

    [Fact]
    public void Generate_ContainsValidateBuildIdFunction()
    {
        var result = ContractLoaderJavaScript.Generate();

        Assert.Contains("function validateBuildId(contract, artifactType)", result);
    }

    [Fact]
    public void Generate_BuildId_DetectsMismatch()
    {
        var result = ContractLoaderJavaScript.Generate();

        Assert.Contains("contract.buildId !== expectedBuildId", result);
        Assert.Contains("BUILD_MISMATCH", result);
    }

    [Fact]
    public void Generate_BuildId_SetsExpectedOnFirstCall()
    {
        var result = ContractLoaderJavaScript.Generate();

        Assert.Contains("expectedBuildId = contract.buildId", result);
    }

    // ===== SHA-256 Hash Integrity =====

    [Fact]
    public void Generate_ContainsComputeHashFunction()
    {
        var result = ContractLoaderJavaScript.Generate();

        Assert.Contains("async function computeHash(content)", result);
    }

    [Fact]
    public void Generate_Hash_UsesWebCryptoSHA256()
    {
        var result = ContractLoaderJavaScript.Generate();

        Assert.Contains("crypto.subtle.digest('SHA-256'", result);
    }

    [Fact]
    public void Generate_Hash_ReturnsHexString()
    {
        var result = ContractLoaderJavaScript.Generate();

        Assert.Contains("toString(16).padStart(2, '0')", result);
    }

    [Fact]
    public void Generate_ContainsValidateHashFunction()
    {
        var result = ContractLoaderJavaScript.Generate();

        Assert.Contains("async function validateHash(rawJson, expectedHash, artifactType)", result);
    }

    [Fact]
    public void Generate_Hash_DetectsMismatch()
    {
        var result = ContractLoaderJavaScript.Generate();

        Assert.Contains("actualHash !== expectedHash", result);
        Assert.Contains("HASH_MISMATCH", result);
    }

    // ===== Fetch With Retry =====

    [Fact]
    public void Generate_ContainsFetchWithRetryFunction()
    {
        var result = ContractLoaderJavaScript.Generate();

        Assert.Contains("async function fetchWithRetry(url, artifactType)", result);
    }

    [Fact]
    public void Generate_FetchWithRetry_ExponentialBackoff()
    {
        var result = ContractLoaderJavaScript.Generate();

        Assert.Contains("Math.pow(2, attempt)", result);
        Assert.Contains("BASE_RETRY_DELAY_MS", result);
    }

    [Fact]
    public void Generate_FetchWithRetry_DefaultRetryConfig()
    {
        var result = ContractLoaderJavaScript.Generate();

        Assert.Contains("const MAX_RETRIES = 2", result);
        Assert.Contains("const BASE_RETRY_DELAY_MS = 500", result);
    }

    [Fact]
    public void Generate_FetchWithRetry_CustomRetryConfig()
    {
        var result = ContractLoaderJavaScript.Generate(maxRetries: 5, baseRetryDelayMs: 1000);

        Assert.Contains("const MAX_RETRIES = 5", result);
        Assert.Contains("const BASE_RETRY_DELAY_MS = 1000", result);
    }

    [Fact]
    public void Generate_FetchWithRetry_Handles404AsNotFound()
    {
        var result = ContractLoaderJavaScript.Generate();

        Assert.Contains("response.status === 404", result);
        Assert.Contains("NOT_FOUND", result);
    }

    // ===== Error Classification =====

    [Fact]
    public void Generate_ContainsErrorCodeEnum()
    {
        var result = ContractLoaderJavaScript.Generate();

        Assert.Contains("const ErrorCode = Object.freeze(", result);
    }

    [Theory]
    [InlineData("NETWORK_ERROR")]
    [InlineData("VERSION_INCOMPATIBLE")]
    [InlineData("VERSION_MISMATCH_MINOR")]
    [InlineData("HASH_MISMATCH")]
    [InlineData("BUILD_MISMATCH")]
    [InlineData("PARSE_ERROR")]
    [InlineData("NOT_FOUND")]
    public void Generate_ContainsErrorCode(string errorCode)
    {
        var result = ContractLoaderJavaScript.Generate();

        Assert.Contains(errorCode, result);
    }

    // ===== Runtime State =====

    [Fact]
    public void Generate_ContainsRuntimeStateEnum()
    {
        var result = ContractLoaderJavaScript.Generate();

        Assert.Contains("const RuntimeState = Object.freeze(", result);
    }

    [Theory]
    [InlineData("LOADING")]
    [InlineData("READY")]
    [InlineData("DEGRADED")]
    public void Generate_ContainsRuntimeState(string state)
    {
        var result = ContractLoaderJavaScript.Generate();

        Assert.Contains("'" + state + "'", result);
    }

    [Fact]
    public void Generate_InitialStateIsLoading()
    {
        var result = ContractLoaderJavaScript.Generate();

        Assert.Contains("let currentState = RuntimeState.LOADING", result);
    }

    // ===== Contract Loading API =====

    [Fact]
    public void Generate_ContainsLoadManifestFunction()
    {
        var result = ContractLoaderJavaScript.Generate();

        Assert.Contains("async function loadManifest()", result);
    }

    [Fact]
    public void Generate_LoadManifest_FetchesManifestJson()
    {
        var result = ContractLoaderJavaScript.Generate();

        Assert.Contains("feature-manifest.json", result);
    }

    [Fact]
    public void Generate_LoadManifest_ValidatesSchemaVersion()
    {
        var result = ContractLoaderJavaScript.Generate();

        // loadManifest calls validateSchemaVersion
        Assert.Contains("validateSchemaVersion(parsed, 'manifest')", result);
    }

    [Fact]
    public void Generate_LoadManifest_ValidatesBuildId()
    {
        var result = ContractLoaderJavaScript.Generate();

        Assert.Contains("validateBuildId(parsed, 'manifest')", result);
    }

    [Fact]
    public void Generate_LoadManifest_SetsStateToReady()
    {
        var result = ContractLoaderJavaScript.Generate();

        Assert.Contains("setState(RuntimeState.READY)", result);
    }

    [Fact]
    public void Generate_ContainsLoadIndexFunction()
    {
        var result = ContractLoaderJavaScript.Generate();

        Assert.Contains("async function loadIndex(manifestRef)", result);
    }

    [Fact]
    public void Generate_LoadIndex_ValidatesHashFromManifest()
    {
        var result = ContractLoaderJavaScript.Generate();

        Assert.Contains("manifestRef.indexHash", result);
        Assert.Contains("validateHash(rawJson, manifestRef.indexHash, 'index')", result);
    }

    [Fact]
    public void Generate_LoadIndex_ValidatesSchemaVersion()
    {
        var result = ContractLoaderJavaScript.Generate();

        Assert.Contains("validateSchemaVersion(parsed, 'index')", result);
    }

    [Fact]
    public void Generate_LoadIndex_ValidatesBuildId()
    {
        var result = ContractLoaderJavaScript.Generate();

        Assert.Contains("validateBuildId(parsed, 'index')", result);
    }

    [Fact]
    public void Generate_ContainsLoadChunkFunction()
    {
        var result = ContractLoaderJavaScript.Generate();

        Assert.Contains("async function loadChunk(manifestEntry)", result);
    }

    [Fact]
    public void Generate_LoadChunk_ValidatesHashFromManifestEntry()
    {
        var result = ContractLoaderJavaScript.Generate();

        Assert.Contains("manifestEntry.chunkHash", result);
    }

    [Fact]
    public void Generate_LoadChunk_ValidatesSchemaAndBuildId()
    {
        var result = ContractLoaderJavaScript.Generate();

        Assert.Contains("validateSchemaVersion(parsed, 'chunk')", result);
        Assert.Contains("validateBuildId(parsed, 'chunk')", result);
    }

    // ===== Warning Banner =====

    [Fact]
    public void Generate_ContainsWarningBanner()
    {
        var result = ContractLoaderJavaScript.Generate();

        Assert.Contains("function showWarningBanner(message)", result);
        Assert.Contains("contract-warning-banner", result);
    }

    // ===== Diagnostics API =====

    [Fact]
    public void Generate_ContainsDiagnosticsFunction()
    {
        var result = ContractLoaderJavaScript.Generate();

        Assert.Contains("function getDiagnostics()", result);
    }

    [Fact]
    public void Generate_Diagnostics_ExposesExpectedFields()
    {
        var result = ContractLoaderJavaScript.Generate();

        Assert.Contains("state: currentState", result);
        Assert.Contains("schemaVersion: SCHEMA_VERSION", result);
        Assert.Contains("compatibilityMinVersion: COMPATIBILITY_MIN_VERSION", result);
        Assert.Contains("generatorVersion: GENERATOR_VERSION", result);
        Assert.Contains("expectedBuildId: expectedBuildId", result);
        Assert.Contains("errorCount: errors.length", result);
    }

    [Fact]
    public void Generate_Diagnostics_AvailableGlobally()
    {
        var result = ContractLoaderJavaScript.Generate();

        Assert.Contains("window.getContractDiagnostics = ContractLoader.getDiagnostics", result);
    }

    // ===== Public API Surface =====

    [Fact]
    public void Generate_PublicAPI_ExposesLoadingMethods()
    {
        var result = ContractLoaderJavaScript.Generate();

        Assert.Contains("loadManifest: loadManifest", result);
        Assert.Contains("loadIndex: loadIndex", result);
        Assert.Contains("loadChunk: loadChunk", result);
    }

    [Fact]
    public void Generate_PublicAPI_ExposesValidationHelpers()
    {
        var result = ContractLoaderJavaScript.Generate();

        Assert.Contains("checkSchemaCompatibility: checkSchemaCompatibility", result);
        Assert.Contains("computeHash: computeHash", result);
        Assert.Contains("validateHash: validateHash", result);
    }

    [Fact]
    public void Generate_PublicAPI_ExposesConstants()
    {
        var result = ContractLoaderJavaScript.Generate();

        Assert.Contains("SCHEMA_VERSION: SCHEMA_VERSION", result);
        Assert.Contains("COMPATIBILITY_MIN_VERSION: COMPATIBILITY_MIN_VERSION", result);
        Assert.Contains("GENERATOR_VERSION: GENERATOR_VERSION", result);
    }

    [Fact]
    public void Generate_PublicAPI_ExposesEnums()
    {
        var result = ContractLoaderJavaScript.Generate();

        Assert.Contains("ErrorCode: ErrorCode", result);
        Assert.Contains("RuntimeState: RuntimeState", result);
    }

    // ===== Structured Error Logging =====

    [Fact]
    public void Generate_ContainsLogErrorFunction()
    {
        var result = ContractLoaderJavaScript.Generate();

        Assert.Contains("function logError(code, message, context)", result);
    }

    [Fact]
    public void Generate_ErrorLog_RecordsTimestamp()
    {
        var result = ContractLoaderJavaScript.Generate();

        Assert.Contains("new Date().toISOString()", result);
    }

    // ===== Integration Points =====

    [Fact]
    public void Generate_ChunkedRuntime_UsesContractLoaderForManifest()
    {
        // Verify ChunkedRuntimeJavaScript now delegates to ContractLoader.loadManifest()
        var chunkedResult = ChunkedRuntimeJavaScript.Generate();

        Assert.Contains("ContractLoader.loadManifest()", chunkedResult);
    }

    [Fact]
    public void Generate_ChunkedRuntime_UsesContractLoaderForChunks()
    {
        // Verify ChunkedRuntimeJavaScript now delegates to ContractLoader.loadChunk()
        var chunkedResult = ChunkedRuntimeJavaScript.Generate();

        Assert.Contains("ContractLoader.loadChunk(entry)", chunkedResult);
    }

    [Fact]
    public void Generate_SearchBridge_UsesContractLoaderForIndex()
    {
        // Verify SearchBridgeJavaScript now delegates to ContractLoader.loadIndex()
        var workerSource = SearchWorkerJavaScript.Generate();
        var bridgeResult = SearchBridgeJavaScript.Generate(workerSource);

        Assert.Contains("ContractLoader.loadIndex(manifest)", bridgeResult);
    }

    // ===== Crypto API Graceful Degradation =====

    [Fact]
    public void Generate_ComputeHash_HandlesCryptoUnavailable()
    {
        var result = ContractLoaderJavaScript.Generate();

        // Must gracefully handle environments where crypto.subtle is unavailable (e.g., non-HTTPS)
        Assert.Contains("crypto.subtle unavailable", result);
        Assert.Contains("return null", result);
    }

    // ===== Parse Error Handling =====

    [Fact]
    public void Generate_LoadManifest_HandlesJsonParseError()
    {
        var result = ContractLoaderJavaScript.Generate();

        Assert.Contains("Failed to parse manifest JSON", result);
        Assert.Contains("PARSE_ERROR", result);
    }

    [Fact]
    public void Generate_LoadIndex_HandlesJsonParseError()
    {
        var result = ContractLoaderJavaScript.Generate();

        Assert.Contains("Failed to parse index JSON", result);
    }

    [Fact]
    public void Generate_LoadChunk_HandlesJsonParseError()
    {
        var result = ContractLoaderJavaScript.Generate();

        Assert.Contains("Failed to parse chunk JSON", result);
    }
}

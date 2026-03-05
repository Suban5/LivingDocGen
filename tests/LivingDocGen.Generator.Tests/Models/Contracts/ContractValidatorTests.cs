using System;
using System.Collections.Generic;
using LivingDocGen.Core.Exceptions;
using LivingDocGen.Generator.Models.Contracts;
using Xunit;

namespace LivingDocGen.Generator.Tests.Models.Contracts;

public class ContractValidatorTests
{
    private static string ValidBuildId => "20260305120000-abcd1234";

    #region ContractBase Validation

    [Fact]
    public void ValidateContractBase_ValidContract_DoesNotThrow()
    {
        var manifest = CreateValidManifest();
        var exception = Record.Exception(() => ContractValidator.ValidateManifest(manifest));
        Assert.Null(exception);
    }

    [Fact]
    public void ValidateContractBase_NullSchemaVersion_ThrowsValidation()
    {
        var manifest = CreateValidManifest();
        manifest.SchemaVersion = null;

        Assert.Throws<ValidationException>(() => ContractValidator.ValidateManifest(manifest));
    }

    [Fact]
    public void ValidateContractBase_EmptyBuildId_ThrowsValidation()
    {
        var manifest = CreateValidManifest();
        manifest.BuildId = "";

        Assert.Throws<ValidationException>(() => ContractValidator.ValidateManifest(manifest));
    }

    [Fact]
    public void ValidateContractBase_IncompatibleSchemaVersion_ThrowsValidation()
    {
        var manifest = CreateValidManifest();
        manifest.SchemaVersion = "99.0";

        Assert.Throws<ValidationException>(() => ContractValidator.ValidateManifest(manifest));
    }

    #endregion

    #region Manifest Validation

    [Fact]
    public void ValidateManifest_Null_ThrowsValidation()
    {
        Assert.Throws<ValidationException>(() => ContractValidator.ValidateManifest(null));
    }

    [Fact]
    public void ValidateManifest_EmptyIndexFile_ThrowsValidation()
    {
        var manifest = CreateValidManifest();
        manifest.IndexFile = "";

        Assert.Throws<ValidationException>(() => ContractValidator.ValidateManifest(manifest));
    }

    [Fact]
    public void ValidateManifest_NegativeTotalFeatures_ThrowsValidation()
    {
        var manifest = CreateValidManifest();
        manifest.TotalFeatures = -1;
        manifest.FeatureMap.Clear();

        Assert.Throws<ValidationException>(() => ContractValidator.ValidateManifest(manifest));
    }

    [Fact]
    public void ValidateManifest_NegativeTotalScenarios_ThrowsValidation()
    {
        var manifest = CreateValidManifest();
        manifest.TotalScenarios = -1;

        Assert.Throws<ValidationException>(() => ContractValidator.ValidateManifest(manifest));
    }

    [Fact]
    public void ValidateManifest_FeatureMapCountMismatch_ThrowsValidation()
    {
        var manifest = CreateValidManifest();
        manifest.TotalFeatures = 5; // But FeatureMap only has 1 entry

        Assert.Throws<ValidationException>(() => ContractValidator.ValidateManifest(manifest));
    }

    [Fact]
    public void ValidateManifest_NullFeatureMap_ThrowsValidation()
    {
        var manifest = CreateValidManifest();
        manifest.FeatureMap = null;

        Assert.Throws<ValidationException>(() => ContractValidator.ValidateManifest(manifest));
    }

    [Fact]
    public void ValidateManifest_EntryMissingFeatureId_ThrowsValidation()
    {
        var manifest = CreateValidManifest();
        manifest.FeatureMap[0].FeatureId = "";

        Assert.Throws<ValidationException>(() => ContractValidator.ValidateManifest(manifest));
    }

    [Fact]
    public void ValidateManifest_EntryMissingName_ThrowsValidation()
    {
        var manifest = CreateValidManifest();
        manifest.FeatureMap[0].Name = "";

        Assert.Throws<ValidationException>(() => ContractValidator.ValidateManifest(manifest));
    }

    [Fact]
    public void ValidateManifest_EntryMissingChunkFile_ThrowsValidation()
    {
        var manifest = CreateValidManifest();
        manifest.FeatureMap[0].ChunkFile = "";

        Assert.Throws<ValidationException>(() => ContractValidator.ValidateManifest(manifest));
    }

    [Fact]
    public void ValidateManifest_EntryInvalidStatus_ThrowsValidation()
    {
        var manifest = CreateValidManifest();
        manifest.FeatureMap[0].Status = "invalid_status";

        Assert.Throws<ValidationException>(() => ContractValidator.ValidateManifest(manifest));
    }

    [Fact]
    public void ValidateManifest_AllValidStatuses_DoNotThrow()
    {
        foreach (var status in new[] { "passed", "failed", "skipped", "untested" })
        {
            var manifest = CreateValidManifest();
            manifest.FeatureMap[0].Status = status;

            var exception = Record.Exception(() => ContractValidator.ValidateManifest(manifest));
            Assert.Null(exception);
        }
    }

    #endregion

    #region Index Validation

    [Fact]
    public void ValidateIndex_Null_ThrowsValidation()
    {
        Assert.Throws<ValidationException>(() => ContractValidator.ValidateIndex(null));
    }

    [Fact]
    public void ValidateIndex_ValidIndex_DoesNotThrow()
    {
        var index = CreateValidIndex();
        var exception = Record.Exception(() => ContractValidator.ValidateIndex(index));
        Assert.Null(exception);
    }

    [Fact]
    public void ValidateIndex_NullScenarioRecords_ThrowsValidation()
    {
        var index = CreateValidIndex();
        index.ScenarioRecords = null;

        Assert.Throws<ValidationException>(() => ContractValidator.ValidateIndex(index));
    }

    [Fact]
    public void ValidateIndex_NullInvertedTokenIndex_ThrowsValidation()
    {
        var index = CreateValidIndex();
        index.InvertedTokenIndex = null;

        Assert.Throws<ValidationException>(() => ContractValidator.ValidateIndex(index));
    }

    [Fact]
    public void ValidateIndex_NullStatusIndex_ThrowsValidation()
    {
        var index = CreateValidIndex();
        index.StatusIndex = null;

        Assert.Throws<ValidationException>(() => ContractValidator.ValidateIndex(index));
    }

    [Fact]
    public void ValidateIndex_NullTagIndex_ThrowsValidation()
    {
        var index = CreateValidIndex();
        index.TagIndex = null;

        Assert.Throws<ValidationException>(() => ContractValidator.ValidateIndex(index));
    }

    [Fact]
    public void ValidateIndex_MismatchedOrdinal_ThrowsValidation()
    {
        var index = CreateValidIndex();
        index.ScenarioRecords[0].ScenarioOrdinal = 99; // Should be 0

        Assert.Throws<ValidationException>(() => ContractValidator.ValidateIndex(index));
    }

    #endregion

    #region Chunk Validation

    [Fact]
    public void ValidateChunk_Null_ThrowsValidation()
    {
        Assert.Throws<ValidationException>(() => ContractValidator.ValidateChunk(null));
    }

    [Fact]
    public void ValidateChunk_ValidChunk_DoesNotThrow()
    {
        var chunk = CreateValidChunk();
        var exception = Record.Exception(() => ContractValidator.ValidateChunk(chunk));
        Assert.Null(exception);
    }

    [Fact]
    public void ValidateChunk_MissingFeatureId_ThrowsValidation()
    {
        var chunk = CreateValidChunk();
        chunk.FeatureId = "";

        Assert.Throws<ValidationException>(() => ContractValidator.ValidateChunk(chunk));
    }

    [Fact]
    public void ValidateChunk_MissingName_ThrowsValidation()
    {
        var chunk = CreateValidChunk();
        chunk.Name = "";

        Assert.Throws<ValidationException>(() => ContractValidator.ValidateChunk(chunk));
    }

    [Fact]
    public void ValidateChunk_NullHtml_ThrowsValidation()
    {
        var chunk = CreateValidChunk();
        chunk.Html = null;

        Assert.Throws<ValidationException>(() => ContractValidator.ValidateChunk(chunk));
    }

    [Fact]
    public void ValidateChunk_NullScenarioSummary_ThrowsValidation()
    {
        var chunk = CreateValidChunk();
        chunk.ScenarioSummary = null;

        Assert.Throws<ValidationException>(() => ContractValidator.ValidateChunk(chunk));
    }

    [Fact]
    public void ValidateChunk_InvalidStatus_ThrowsValidation()
    {
        var chunk = CreateValidChunk();
        chunk.Status = "bogus";

        Assert.Throws<ValidationException>(() => ContractValidator.ValidateChunk(chunk));
    }

    [Fact]
    public void ValidateChunk_HashMismatch_ThrowsValidation()
    {
        var chunk = CreateValidChunk();
        chunk.ChunkHash = "aaaa";

        Assert.Throws<ValidationException>(() =>
            ContractValidator.ValidateChunk(chunk, "bbbb"));
    }

    [Fact]
    public void ValidateChunk_HashMatch_DoesNotThrow()
    {
        var chunk = CreateValidChunk();
        chunk.ChunkHash = "abc123";

        var exception = Record.Exception(() =>
            ContractValidator.ValidateChunk(chunk, "abc123"));
        Assert.Null(exception);
    }

    [Fact]
    public void ValidateChunk_NullExpectedHash_SkipsHashCheck()
    {
        var chunk = CreateValidChunk();
        chunk.ChunkHash = "anyhash";

        var exception = Record.Exception(() =>
            ContractValidator.ValidateChunk(chunk, null));
        Assert.Null(exception);
    }

    #endregion

    #region Hash Validation Helpers

    [Fact]
    public void ValidateIndexHash_EmptyExpected_ReturnsTrue()
    {
        Assert.True(ContractValidator.ValidateIndexHash("content", ""));
    }

    [Fact]
    public void ValidateIndexHash_NullExpected_ReturnsTrue()
    {
        Assert.True(ContractValidator.ValidateIndexHash("content", null));
    }

    [Fact]
    public void ValidateIndexHash_MatchingHash_ReturnsTrue()
    {
        var content = "index content";
        var hash = ContractHashValidator.ComputeHash(content);
        Assert.True(ContractValidator.ValidateIndexHash(content, hash));
    }

    [Fact]
    public void ValidateIndexHash_MismatchedHash_ReturnsFalse()
    {
        Assert.False(ContractValidator.ValidateIndexHash("content", "wronghash"));
    }

    [Fact]
    public void ValidateChunkHash_EmptyExpected_ReturnsTrue()
    {
        Assert.True(ContractValidator.ValidateChunkHash("content", ""));
    }

    [Fact]
    public void ValidateChunkHash_MatchingHash_ReturnsTrue()
    {
        var content = "chunk content";
        var hash = ContractHashValidator.ComputeHash(content);
        Assert.True(ContractValidator.ValidateChunkHash(content, hash));
    }

    #endregion

    #region Helpers

    private static FeatureManifest CreateValidManifest()
    {
        return new FeatureManifest
        {
            SchemaVersion = ContractVersion.SchemaVersion,
            GeneratorVersion = ContractVersion.GeneratorVersion,
            CompatibilityMinVersion = ContractVersion.CompatibilityMinVersion,
            BuildId = ValidBuildId,
            GeneratedAt = DateTime.UtcNow,
            TotalFeatures = 1,
            TotalScenarios = 3,
            IndexFile = "feature-index.json",
            ChunkCount = 1,
            FeatureMap = new List<FeatureManifestEntry>
            {
                new FeatureManifestEntry
                {
                    FeatureId = "abc123def456",
                    Name = "User Login",
                    FilePath = "features/login.feature",
                    Status = "passed",
                    ScenarioCount = 3,
                    ChunkFile = "features/abc123def456.json",
                    ChunkHash = "somehash",
                    Tags = new List<string> { "@smoke" }
                }
            }
        };
    }

    private static FeatureIndex CreateValidIndex()
    {
        return new FeatureIndex
        {
            SchemaVersion = ContractVersion.SchemaVersion,
            GeneratorVersion = ContractVersion.GeneratorVersion,
            CompatibilityMinVersion = ContractVersion.CompatibilityMinVersion,
            BuildId = ValidBuildId,
            ScenarioRecords = new List<ScenarioRecord>
            {
                new ScenarioRecord
                {
                    ScenarioOrdinal = 0,
                    FeatureOrdinal = 0,
                    Name = "Successful Login",
                    Status = "passed",
                    Tokens = new List<string> { "successful", "login" },
                    Tags = new List<string> { "@smoke" }
                },
                new ScenarioRecord
                {
                    ScenarioOrdinal = 1,
                    FeatureOrdinal = 0,
                    Name = "Failed Login",
                    Status = "failed",
                    Tokens = new List<string> { "failed", "login" },
                    Tags = new List<string> { "@regression" }
                }
            },
            InvertedTokenIndex = new Dictionary<string, List<int>>
            {
                ["login"] = new List<int> { 0, 1 },
                ["successful"] = new List<int> { 0 },
                ["failed"] = new List<int> { 1 }
            },
            StatusIndex = new StatusIndex
            {
                Passed = new List<int> { 0 },
                Failed = new List<int> { 1 },
                Skipped = new List<int>(),
                Untested = new List<int>()
            },
            TagIndex = new Dictionary<string, List<int>>
            {
                ["@smoke"] = new List<int> { 0 },
                ["@regression"] = new List<int> { 1 }
            }
        };
    }

    private static FeatureChunk CreateValidChunk()
    {
        return new FeatureChunk
        {
            SchemaVersion = ContractVersion.SchemaVersion,
            GeneratorVersion = ContractVersion.GeneratorVersion,
            CompatibilityMinVersion = ContractVersion.CompatibilityMinVersion,
            BuildId = ValidBuildId,
            FeatureId = "abc123def456",
            Name = "User Login",
            Status = "passed",
            Html = "<div>Feature content</div>",
            ScenarioSummary = new ChunkScenarioSummary
            {
                Total = 3,
                Passed = 2,
                Failed = 1,
                Skipped = 0,
                Untested = 0
            },
            ContentStats = new ChunkContentStats
            {
                MaxTableRows = 10,
                MaxTableColumns = 5,
                OutlineExamples = 3
            }
        };
    }

    #endregion
}

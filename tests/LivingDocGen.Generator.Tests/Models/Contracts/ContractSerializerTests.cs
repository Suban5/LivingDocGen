using System;
using System.Collections.Generic;
using LivingDocGen.Generator.Models.Contracts;
using Xunit;

namespace LivingDocGen.Generator.Tests.Models.Contracts;

public class ContractSerializerTests
{
    private static string ValidBuildId => "20260305120000-abcd1234";

    #region Manifest Serialization

    [Fact]
    public void SerializeManifest_ValidManifest_ProducesValidJson()
    {
        var manifest = CreateValidManifest();
        var json = ContractSerializer.SerializeManifest(manifest);

        Assert.NotEmpty(json);
        Assert.Contains("schemaVersion", json);
        Assert.Contains("featureMap", json);
    }

    [Fact]
    public void SerializeManifest_Null_ThrowsArgumentNull()
    {
        Assert.Throws<ArgumentNullException>(() => ContractSerializer.SerializeManifest(null));
    }

    [Fact]
    public void SerializeManifest_Indented_ContainsNewlines()
    {
        var manifest = CreateValidManifest();
        var json = ContractSerializer.SerializeManifest(manifest, indented: true);

        Assert.Contains("\n", json);
    }

    [Fact]
    public void DeserializeManifest_RoundTrip_PreservesData()
    {
        var original = CreateValidManifest();
        var json = ContractSerializer.SerializeManifest(original);
        var deserialized = ContractSerializer.DeserializeManifest(json);

        Assert.Equal(original.SchemaVersion, deserialized.SchemaVersion);
        Assert.Equal(original.BuildId, deserialized.BuildId);
        Assert.Equal(original.TotalFeatures, deserialized.TotalFeatures);
        Assert.Equal(original.TotalScenarios, deserialized.TotalScenarios);
        Assert.Equal(original.IndexFile, deserialized.IndexFile);
        Assert.Equal(original.ChunkStrategy, deserialized.ChunkStrategy);
        Assert.Equal(original.ChunkCount, deserialized.ChunkCount);
        Assert.Equal(original.FeatureMap.Count, deserialized.FeatureMap.Count);
        Assert.Equal(original.FeatureMap[0].FeatureId, deserialized.FeatureMap[0].FeatureId);
        Assert.Equal(original.FeatureMap[0].Name, deserialized.FeatureMap[0].Name);
        Assert.Equal(original.FeatureMap[0].Status, deserialized.FeatureMap[0].Status);
    }

    [Fact]
    public void DeserializeManifest_NullJson_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => ContractSerializer.DeserializeManifest(null));
    }

    [Fact]
    public void DeserializeManifest_EmptyJson_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => ContractSerializer.DeserializeManifest(""));
    }

    [Fact]
    public void DeserializeManifest_WithValidation_ValidatesStructure()
    {
        var manifest = CreateValidManifest();
        manifest.TotalFeatures = 999; // Mismatch with FeatureMap count
        var json = ContractSerializer.SerializeManifest(manifest);

        Assert.ThrowsAny<Exception>(() => ContractSerializer.DeserializeManifest(json, validate: true));
    }

    [Fact]
    public void DeserializeManifest_WithoutValidation_SkipsValidation()
    {
        var manifest = CreateValidManifest();
        manifest.TotalFeatures = 999; // Mismatch - but validation disabled
        var json = ContractSerializer.SerializeManifest(manifest);

        var result = ContractSerializer.DeserializeManifest(json, validate: false);
        Assert.Equal(999, result.TotalFeatures);
    }

    #endregion

    #region Index Serialization

    [Fact]
    public void SerializeIndex_ReturnsJsonAndHash()
    {
        var index = CreateValidIndex();
        var (json, hash) = ContractSerializer.SerializeIndex(index);

        Assert.NotEmpty(json);
        Assert.NotEmpty(hash);
        Assert.Equal(64, hash.Length);
    }

    [Fact]
    public void SerializeIndex_Null_ThrowsArgumentNull()
    {
        Assert.Throws<ArgumentNullException>(() => ContractSerializer.SerializeIndex(null));
    }

    [Fact]
    public void SerializeIndex_HashIsConsistent()
    {
        var index = CreateValidIndex();
        var (_, hash1) = ContractSerializer.SerializeIndex(index);
        var (_, hash2) = ContractSerializer.SerializeIndex(index);

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void DeserializeIndex_RoundTrip_PreservesData()
    {
        var original = CreateValidIndex();
        var (json, hash) = ContractSerializer.SerializeIndex(original);
        var deserialized = ContractSerializer.DeserializeIndex(json, hash);

        Assert.Equal(original.ScenarioRecords.Count, deserialized.ScenarioRecords.Count);
        Assert.Equal(original.ScenarioRecords[0].Name, deserialized.ScenarioRecords[0].Name);
        Assert.Equal(original.InvertedTokenIndex.Count, deserialized.InvertedTokenIndex.Count);
        Assert.Equal(original.StatusIndex.Passed.Count, deserialized.StatusIndex.Passed.Count);
        Assert.Equal(original.TagIndex.Count, deserialized.TagIndex.Count);
    }

    [Fact]
    public void DeserializeIndex_InvalidHash_ThrowsInvalidOperation()
    {
        var index = CreateValidIndex();
        var (json, _) = ContractSerializer.SerializeIndex(index);

        Assert.Throws<InvalidOperationException>(() =>
            ContractSerializer.DeserializeIndex(json, "badhash"));
    }

    [Fact]
    public void DeserializeIndex_NullExpectedHash_SkipsHashCheck()
    {
        var index = CreateValidIndex();
        var (json, _) = ContractSerializer.SerializeIndex(index);

        var result = ContractSerializer.DeserializeIndex(json, expectedHash: null);
        Assert.NotNull(result);
    }

    #endregion

    #region Chunk Serialization

    [Fact]
    public void SerializeChunk_ReturnsJsonAndHash()
    {
        var chunk = CreateValidChunk();
        var (json, hash) = ContractSerializer.SerializeChunk(chunk);

        Assert.NotEmpty(json);
        Assert.NotEmpty(hash);
        Assert.Contains("featureId", json);
    }

    [Fact]
    public void SerializeChunk_Null_ThrowsArgumentNull()
    {
        Assert.Throws<ArgumentNullException>(() => ContractSerializer.SerializeChunk(null));
    }

    [Fact]
    public void SerializeChunk_SetsChunkHashOnModel()
    {
        var chunk = CreateValidChunk();
        var (_, hash) = ContractSerializer.SerializeChunk(chunk);

        Assert.Equal(hash, chunk.ChunkHash);
    }

    [Fact]
    public void DeserializeChunk_RoundTrip_PreservesData()
    {
        var original = CreateValidChunk();
        var (json, hash) = ContractSerializer.SerializeChunk(original);
        var deserialized = ContractSerializer.DeserializeChunk(json);

        Assert.Equal(original.FeatureId, deserialized.FeatureId);
        Assert.Equal(original.Name, deserialized.Name);
        Assert.Equal(original.Status, deserialized.Status);
        Assert.Equal(original.Html, deserialized.Html);
        Assert.Equal(original.ScenarioSummary.Total, deserialized.ScenarioSummary.Total);
        Assert.Equal(original.ScenarioSummary.Passed, deserialized.ScenarioSummary.Passed);
    }

    [Fact]
    public void DeserializeChunk_NullJson_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => ContractSerializer.DeserializeChunk(null));
    }

    #endregion

    #region Worker Message Serialization

    [Fact]
    public void SerializeWorkerMessage_Query_ProducesValidJson()
    {
        var message = new WorkerMessage
        {
            Type = WorkerMessageType.Query,
            RequestId = "q-001",
            QuerySeq = 1,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Payload = new WorkerPayload
            {
                Query = "login",
                Status = "failed",
                Tags = new List<string> { "@critical" }
            }
        };

        var json = ContractSerializer.SerializeWorkerMessage(message);

        Assert.NotEmpty(json);
        Assert.Contains("query", json);
        Assert.Contains("login", json);
    }

    [Fact]
    public void SerializeWorkerMessage_Null_ThrowsArgumentNull()
    {
        Assert.Throws<ArgumentNullException>(() => ContractSerializer.SerializeWorkerMessage(null));
    }

    [Fact]
    public void DeserializeWorkerMessage_RoundTrip_PreservesData()
    {
        var original = new WorkerMessage
        {
            Type = WorkerMessageType.Result,
            RequestId = "q-042",
            QuerySeq = 42,
            Timestamp = 1709640000000,
            Payload = new WorkerPayload
            {
                ScenarioOrdinals = new List<int> { 0, 5, 10 },
                Count = 3,
                Stats = new WorkerQueryStats
                {
                    EvalMs = 18.5,
                    CandidateCount = 57,
                    IndexLookups = 3
                }
            }
        };

        var json = ContractSerializer.SerializeWorkerMessage(original);
        var deserialized = ContractSerializer.DeserializeWorkerMessage(json);

        Assert.Equal(WorkerMessageType.Result, deserialized.Type);
        Assert.Equal("q-042", deserialized.RequestId);
        Assert.Equal(42, deserialized.QuerySeq);
        Assert.Equal(3, deserialized.Payload.Count);
        Assert.Equal(3, deserialized.Payload.ScenarioOrdinals.Count);
        Assert.Equal(18.5, deserialized.Payload.Stats.EvalMs);
    }

    [Fact]
    public void DeserializeWorkerMessage_NullJson_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => ContractSerializer.DeserializeWorkerMessage(null));
    }

    #endregion

    #region BuildId Generation

    [Fact]
    public void GenerateBuildId_ReturnsNonEmpty()
    {
        var buildId = ContractSerializer.GenerateBuildId();
        Assert.NotEmpty(buildId);
    }

    [Fact]
    public void GenerateBuildId_ContainsTimestamp()
    {
        var buildId = ContractSerializer.GenerateBuildId();
        // Format: yyyyMMddHHmmss-{8hex}
        Assert.Contains("-", buildId);
        Assert.True(buildId.Length > 15); // 14 digits + '-' + 8 hex
    }

    [Fact]
    public void GenerateBuildId_IsUnique()
    {
        var id1 = ContractSerializer.GenerateBuildId();
        var id2 = ContractSerializer.GenerateBuildId();

        Assert.NotEqual(id1, id2);
    }

    #endregion

    #region CamelCase Serialization

    [Fact]
    public void Serialization_UsesCamelCasePropertyNames()
    {
        var manifest = CreateValidManifest();
        var json = ContractSerializer.SerializeManifest(manifest);

        Assert.Contains("\"schemaVersion\"", json);
        Assert.Contains("\"generatorVersion\"", json);
        Assert.Contains("\"buildId\"", json);
        Assert.Contains("\"totalFeatures\"", json);
        Assert.Contains("\"featureMap\"", json);
        Assert.Contains("\"indexFile\"", json);

        // Should NOT contain PascalCase
        Assert.DoesNotContain("\"SchemaVersion\"", json);
        Assert.DoesNotContain("\"BuildId\"", json);
    }

    [Fact]
    public void Serialization_EnumValuesAreCamelCase()
    {
        var message = new WorkerMessage
        {
            Type = WorkerMessageType.Query,
            RequestId = "test",
            Payload = new WorkerPayload()
        };

        var json = ContractSerializer.SerializeWorkerMessage(message);
        Assert.Contains("\"query\"", json);
        Assert.DoesNotContain("\"Query\"", json);
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
            GeneratedAt = new DateTime(2026, 3, 5, 12, 0, 0, DateTimeKind.Utc),
            TotalFeatures = 1,
            TotalScenarios = 3,
            IndexFile = "feature-index.json",
            ChunkStrategy = "per-feature",
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
                    EstimatedBytes = 4096,
                    Tags = new List<string> { "@smoke" },
                    ScenarioRange = new ScenarioRange { Start = 0, End = 3 }
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
                }
            },
            InvertedTokenIndex = new Dictionary<string, List<int>>
            {
                ["login"] = new List<int> { 0 },
                ["successful"] = new List<int> { 0 }
            },
            StatusIndex = new StatusIndex
            {
                Passed = new List<int> { 0 },
                Failed = new List<int>(),
                Skipped = new List<int>(),
                Untested = new List<int>()
            },
            TagIndex = new Dictionary<string, List<int>>
            {
                ["@smoke"] = new List<int> { 0 }
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
            Html = "<div class=\"feature\">Login Feature</div>",
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

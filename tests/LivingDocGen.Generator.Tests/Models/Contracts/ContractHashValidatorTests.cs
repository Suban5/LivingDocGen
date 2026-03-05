using System;
using LivingDocGen.Generator.Models.Contracts;
using Xunit;

namespace LivingDocGen.Generator.Tests.Models.Contracts;

public class ContractHashValidatorTests
{
    [Fact]
    public void ComputeHash_ReturnsConsistentHashForSameInput()
    {
        var content = "Hello, World!";
        var hash1 = ContractHashValidator.ComputeHash(content);
        var hash2 = ContractHashValidator.ComputeHash(content);

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void ComputeHash_ReturnsDifferentHashForDifferentInput()
    {
        var hash1 = ContractHashValidator.ComputeHash("Hello");
        var hash2 = ContractHashValidator.ComputeHash("World");

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void ComputeHash_ReturnsLowercaseHex64Chars()
    {
        var hash = ContractHashValidator.ComputeHash("test");

        // SHA-256 produces 32 bytes = 64 hex characters
        Assert.Equal(64, hash.Length);
        Assert.Matches("^[0-9a-f]+$", hash);
    }

    [Fact]
    public void ComputeHash_EmptyString_ReturnsValidHash()
    {
        var hash = ContractHashValidator.ComputeHash("");
        Assert.NotEmpty(hash);
        Assert.Equal(64, hash.Length);
    }

    [Fact]
    public void ComputeHash_NullString_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ContractHashValidator.ComputeHash((string)null!));
    }

    [Fact]
    public void ComputeHash_NullBytes_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ContractHashValidator.ComputeHash((byte[])null!));
    }

    [Fact]
    public void ComputeHash_ByteArray_ReturnsValidHash()
    {
        var bytes = new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F }; // "Hello"
        var hash = ContractHashValidator.ComputeHash(bytes);

        Assert.NotEmpty(hash);
        Assert.Equal(64, hash.Length);
    }

    [Fact]
    public void ValidateHash_MatchingContent_ReturnsTrue()
    {
        var content = "test content";
        var hash = ContractHashValidator.ComputeHash(content);

        Assert.True(ContractHashValidator.ValidateHash(content, hash));
    }

    [Fact]
    public void ValidateHash_NonMatchingContent_ReturnsFalse()
    {
        var hash = ContractHashValidator.ComputeHash("original");
        Assert.False(ContractHashValidator.ValidateHash("tampered", hash));
    }

    [Fact]
    public void ValidateHash_NullContent_ReturnsFalse()
    {
        Assert.False(ContractHashValidator.ValidateHash((string)null!, "somehash"));
    }

    [Fact]
    public void ValidateHash_EmptyExpectedHash_ReturnsFalse()
    {
        Assert.False(ContractHashValidator.ValidateHash("content", ""));
    }

    [Fact]
    public void ValidateHash_CaseInsensitiveHash_ReturnsTrue()
    {
        var content = "case test";
        var hash = ContractHashValidator.ComputeHash(content);
        var upperHash = hash.ToUpperInvariant();

        Assert.True(ContractHashValidator.ValidateHash(content, upperHash));
    }

    // --- Feature ID Generation ---

    [Fact]
    public void GenerateFeatureId_Deterministic_SameInputSameOutput()
    {
        var id1 = ContractHashValidator.GenerateFeatureId("features/login.feature", "User Login");
        var id2 = ContractHashValidator.GenerateFeatureId("features/login.feature", "User Login");

        Assert.Equal(id1, id2);
    }

    [Fact]
    public void GenerateFeatureId_DifferentPaths_DifferentIds()
    {
        var id1 = ContractHashValidator.GenerateFeatureId("features/login.feature", "Login");
        var id2 = ContractHashValidator.GenerateFeatureId("features/logout.feature", "Login");

        Assert.NotEqual(id1, id2);
    }

    [Fact]
    public void GenerateFeatureId_DifferentNames_DifferentIds()
    {
        var id1 = ContractHashValidator.GenerateFeatureId("features/auth.feature", "Login");
        var id2 = ContractHashValidator.GenerateFeatureId("features/auth.feature", "Register");

        Assert.NotEqual(id1, id2);
    }

    [Fact]
    public void GenerateFeatureId_BackslashNormalized_SameAsForwardSlash()
    {
        var id1 = ContractHashValidator.GenerateFeatureId("features\\login.feature", "Login");
        var id2 = ContractHashValidator.GenerateFeatureId("features/login.feature", "Login");

        Assert.Equal(id1, id2);
    }

    [Fact]
    public void GenerateFeatureId_CaseNormalized()
    {
        var id1 = ContractHashValidator.GenerateFeatureId("Features/Login.feature", "Login");
        var id2 = ContractHashValidator.GenerateFeatureId("features/login.feature", "Login");

        Assert.Equal(id1, id2);
    }

    [Fact]
    public void GenerateFeatureId_Returns12HexChars()
    {
        var id = ContractHashValidator.GenerateFeatureId("path/file.feature", "Feature Name");

        Assert.Equal(12, id.Length);
        Assert.Matches("^[0-9a-f]+$", id);
    }

    [Fact]
    public void GenerateFeatureId_NullName_DoesNotThrow()
    {
        var id = ContractHashValidator.GenerateFeatureId("path/file.feature", null);
        Assert.Equal(12, id.Length);
    }

    // --- Scenario ID Generation ---

    [Fact]
    public void GenerateScenarioId_Deterministic()
    {
        var id1 = ContractHashValidator.GenerateScenarioId("abc123def456", "Login Scenario", 10);
        var id2 = ContractHashValidator.GenerateScenarioId("abc123def456", "Login Scenario", 10);

        Assert.Equal(id1, id2);
    }

    [Fact]
    public void GenerateScenarioId_DifferentLineNumbers_DifferentIds()
    {
        var id1 = ContractHashValidator.GenerateScenarioId("abc123def456", "Scenario", 10);
        var id2 = ContractHashValidator.GenerateScenarioId("abc123def456", "Scenario", 20);

        Assert.NotEqual(id1, id2);
    }

    [Fact]
    public void GenerateScenarioId_Returns12HexChars()
    {
        var id = ContractHashValidator.GenerateScenarioId("feature1", "Scenario Name", 5);

        Assert.Equal(12, id.Length);
        Assert.Matches("^[0-9a-f]+$", id);
    }
}

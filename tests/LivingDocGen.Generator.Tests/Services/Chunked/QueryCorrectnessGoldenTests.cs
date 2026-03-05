using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using LivingDocGen.Generator.Models;
using LivingDocGen.Generator.Models.Contracts;
using LivingDocGen.Generator.Services.Chunked;
using LivingDocGen.Parser.Models;
using LivingDocGen.TestReporter.Models;

namespace LivingDocGen.Generator.Tests.Services.Chunked;

/// <summary>
/// PR-4.5: Query Correctness Gate
///
/// Golden tests that verify query correctness across all combinations
/// of status, tags, and text filters against a canonical dataset.
/// Ensures the C# index builder produces correct inverted indexes
/// and that set-intersection queries return deterministic, correct results.
/// </summary>
public class QueryCorrectnessGoldenTests
{
    // ================================================================
    // Canonical Dataset
    // ================================================================
    //
    // Feature 0: "User Authentication" [@smoke, @security]
    //   Scenario 0: "Successful login with valid credentials"  — passed   [@regression]
    //   Scenario 1: "Failed login with invalid password"        — failed   [@regression]
    //   Scenario 2: "Account lockout after 3 attempts"          — skipped  [@slow]
    //
    // Feature 1: "Shopping Cart Management" [@smoke, @ecommerce]
    //   Scenario 3: "Add item to empty cart"                    — passed   [@regression]
    //   Scenario 4: "Remove item from cart"                     — passed   []
    //   Scenario 5: "Apply discount coupon"                     — failed   [@ecommerce]
    //
    // Feature 2: "API Payment Processing" [@api, @security]
    //   Scenario 6: "Process credit card payment"               — passed   [@critical]
    //   Scenario 7: "Handle payment timeout"                    — untested [@critical, @slow]
    //   Scenario 8: "Refund completed order"                    — failed   [@regression]
    //
    // Feature 3: "User Profile" [@smoke]
    //   Scenario 9:  "Update email address"                     — passed   []
    //   Scenario 10: "Change password"                          — untested [@security]
    //   Scenario 11: "Delete account"                           — skipped  [@critical]
    //
    // ================================================================

    private readonly FeatureIndex _index;
    private readonly IndexBuilderService _indexBuilder;

    public QueryCorrectnessGoldenTests()
    {
        _indexBuilder = new IndexBuilderService(new TokenizerService());
        var doc = BuildCanonicalDataset();
        _index = _indexBuilder.BuildIndex(doc, "golden-build-001");
    }

    // ================================================================
    // 1. Dataset Integrity Tests
    // ================================================================

    [Fact]
    public void GoldenDataset_Has12Scenarios()
    {
        Assert.Equal(12, _index.ScenarioRecords.Count);
    }

    [Fact]
    public void GoldenDataset_Has4Features()
    {
        var featureOrdinals = _index.ScenarioRecords
            .Select(r => r.FeatureOrdinal)
            .Distinct()
            .OrderBy(x => x)
            .ToList();
        Assert.Equal(new List<int> { 0, 1, 2, 3 }, featureOrdinals);
    }

    [Fact]
    public void GoldenDataset_StatusDistribution_IsCorrect()
    {
        // 0=passed, 1=failed, 2=skipped, 3=passed, 4=passed, 5=failed,
        // 6=passed, 7=untested, 8=failed, 9=passed, 10=untested, 11=skipped
        Assert.Equal(5, _index.StatusIndex.Passed.Count);    // 0,3,4,6,9
        Assert.Equal(3, _index.StatusIndex.Failed.Count);    // 1,5,8
        Assert.Equal(2, _index.StatusIndex.Skipped.Count);   // 2,11
        Assert.Equal(2, _index.StatusIndex.Untested.Count);  // 7,10
    }

    [Fact]
    public void GoldenDataset_StatusOrdinals_AreExact()
    {
        AssertOrdinals(_index.StatusIndex.Passed, new[] { 0, 3, 4, 6, 9 });
        AssertOrdinals(_index.StatusIndex.Failed, new[] { 1, 5, 8 });
        AssertOrdinals(_index.StatusIndex.Skipped, new[] { 2, 11 });
        AssertOrdinals(_index.StatusIndex.Untested, new[] { 7, 10 });
    }

    [Fact]
    public void GoldenDataset_TagIndex_ContainsExpectedTags()
    {
        var expectedTags = new[]
        {
            "smoke", "security", "regression", "slow", "ecommerce",
            "api", "critical"
        };

        foreach (var tag in expectedTags)
        {
            Assert.True(_index.TagIndex.ContainsKey(tag),
                $"Expected tag '{tag}' in index");
        }
    }

    [Fact]
    public void GoldenDataset_TagOrdinals_AreExact()
    {
        // @smoke is on features 0,1,3 → scenarios 0,1,2,3,4,5,9,10,11
        AssertOrdinals(_index.TagIndex["smoke"], new[] { 0, 1, 2, 3, 4, 5, 9, 10, 11 });

        // @security is on features 0,2 + scenario 10 → scenarios 0,1,2,6,7,8,10
        AssertOrdinals(_index.TagIndex["security"], new[] { 0, 1, 2, 6, 7, 8, 10 });

        // @regression is on scenarios 0,1,3,8
        AssertOrdinals(_index.TagIndex["regression"], new[] { 0, 1, 3, 8 });

        // @slow is on scenarios 2,7
        AssertOrdinals(_index.TagIndex["slow"], new[] { 2, 7 });

        // @ecommerce is on feature 1 + scenario 5 → scenarios 3,4,5 (feature tag) + 5 (scenario tag, deduped)
        AssertOrdinals(_index.TagIndex["ecommerce"], new[] { 3, 4, 5 });

        // @api is on feature 2 → scenarios 6,7,8
        AssertOrdinals(_index.TagIndex["api"], new[] { 6, 7, 8 });

        // @critical is on scenarios 6,7,11
        AssertOrdinals(_index.TagIndex["critical"], new[] { 6, 7, 11 });
    }

    [Fact]
    public void GoldenDataset_ScenarioOrdinals_AreSequential()
    {
        for (int i = 0; i < _index.ScenarioRecords.Count; i++)
        {
            Assert.Equal(i, _index.ScenarioRecords[i].ScenarioOrdinal);
        }
    }

    [Fact]
    public void GoldenDataset_BuildId_IsCorrect()
    {
        Assert.Equal("golden-build-001", _index.BuildId);
    }

    // ================================================================
    // 2. Status-Only Queries
    // ================================================================

    [Theory]
    [InlineData("passed", new[] { 0, 3, 4, 6, 9 })]
    [InlineData("failed", new[] { 1, 5, 8 })]
    [InlineData("skipped", new[] { 2, 11 })]
    [InlineData("untested", new[] { 7, 10 })]
    public void Query_StatusOnly_ReturnsCorrectScenarios(string status, int[] expectedOrdinals)
    {
        var result = ExecuteQuery(status: status);
        AssertOrdinals(result, expectedOrdinals);
    }

    [Fact]
    public void Query_AllStatus_ReturnsAllScenarios()
    {
        var result = ExecuteQuery(status: "all");
        AssertOrdinals(result, Enumerable.Range(0, 12).ToArray());
    }

    [Fact]
    public void Query_NoFilters_ReturnsAllScenarios()
    {
        var result = ExecuteQuery();
        AssertOrdinals(result, Enumerable.Range(0, 12).ToArray());
    }

    // ================================================================
    // 3. Tag-Only Queries
    // ================================================================

    [Theory]
    [InlineData(new[] { "smoke" }, new[] { 0, 1, 2, 3, 4, 5, 9, 10, 11 })]
    [InlineData(new[] { "security" }, new[] { 0, 1, 2, 6, 7, 8, 10 })]
    [InlineData(new[] { "regression" }, new[] { 0, 1, 3, 8 })]
    [InlineData(new[] { "critical" }, new[] { 6, 7, 11 })]
    [InlineData(new[] { "api" }, new[] { 6, 7, 8 })]
    [InlineData(new[] { "slow" }, new[] { 2, 7 })]
    [InlineData(new[] { "ecommerce" }, new[] { 3, 4, 5 })]
    public void Query_SingleTag_ReturnsCorrectScenarios(string[] tags, int[] expectedOrdinals)
    {
        var result = ExecuteQuery(tags: tags);
        AssertOrdinals(result, expectedOrdinals);
    }

    [Theory]
    [InlineData(new[] { "smoke", "security" }, new[] { 0, 1, 2, 10 })]
    [InlineData(new[] { "smoke", "regression" }, new[] { 0, 1, 3 })]
    [InlineData(new[] { "security", "critical" }, new[] { 6, 7 })]
    [InlineData(new[] { "api", "critical" }, new[] { 6, 7 })]
    [InlineData(new[] { "api", "slow" }, new[] { 7 })]
    [InlineData(new[] { "smoke", "ecommerce" }, new[] { 3, 4, 5 })]
    [InlineData(new[] { "regression", "security" }, new[] { 0, 1, 8 })]
    public void Query_MultipleTagsAND_ReturnsIntersection(string[] tags, int[] expectedOrdinals)
    {
        var result = ExecuteQuery(tags: tags);
        AssertOrdinals(result, expectedOrdinals);
    }

    [Fact]
    public void Query_NonExistentTag_ReturnsEmpty()
    {
        var result = ExecuteQuery(tags: new[] { "nonexistent" });
        Assert.Empty(result);
    }

    [Fact]
    public void Query_NonExistentTagCombined_ReturnsEmpty()
    {
        var result = ExecuteQuery(tags: new[] { "smoke", "nonexistent" });
        Assert.Empty(result);
    }

    // ================================================================
    // 4. Text-Only Queries
    // ================================================================

    [Theory]
    [InlineData("login", new[] { 0, 1 })]           // "login" appears in scenario names 0,1 only (not 2 "Account lockout...")
    [InlineData("cart", new[] { 3, 4, 5 })]          // "cart" in feature/scenario names
    [InlineData("payment", new[] { 6, 7, 8 })]       // "payment" in feature/scenario names
    [InlineData("password", new[] { 1, 10 })]         // "password" in scenario 1 "Failed login with wrong password" and 10 "Update password"
    public void Query_TextOnly_ReturnsCorrectScenarios(string query, int[] expectedOrdinals)
    {
        var result = ExecuteQuery(query: query);
        AssertOrdinals(result, expectedOrdinals);
    }

    [Fact]
    public void Query_TextCaseInsensitive_ReturnsSameResults()
    {
        var lower = ExecuteQuery(query: "login");
        var upper = ExecuteQuery(query: "LOGIN");
        var mixed = ExecuteQuery(query: "Login");

        AssertOrdinals(lower, upper.ToArray());
        AssertOrdinals(lower, mixed.ToArray());
    }

    [Fact]
    public void Query_NonExistentText_ReturnsEmpty()
    {
        var result = ExecuteQuery(query: "xyznonexistent");
        Assert.Empty(result);
    }

    [Fact]
    public void Query_TextMatchingMultipleFeatures_ReturnsUnion()
    {
        // "user" appears in "User Authentication" (feature 0) and "User Profile" (feature 3)
        var result = ExecuteQuery(query: "user");
        // Scenarios from Feature 0 (0,1,2) and Feature 3 (9,10,11)
        AssertOrdinals(result, new[] { 0, 1, 2, 9, 10, 11 });
    }

    // ================================================================
    // 5. Status + Tag Combined Queries
    // ================================================================

    [Theory]
    [InlineData("passed", new[] { "smoke" }, new[] { 0, 3, 4, 9 })]
    [InlineData("failed", new[] { "regression" }, new[] { 1, 8 })]
    [InlineData("passed", new[] { "security" }, new[] { 0, 6 })]
    [InlineData("untested", new[] { "critical" }, new[] { 7 })]
    [InlineData("skipped", new[] { "smoke" }, new[] { 2, 11 })]
    [InlineData("failed", new[] { "ecommerce" }, new[] { 5 })]
    [InlineData("passed", new[] { "api" }, new[] { 6 })]
    public void Query_StatusAndTag_ReturnsIntersection(string status, string[] tags, int[] expectedOrdinals)
    {
        var result = ExecuteQuery(status: status, tags: tags);
        AssertOrdinals(result, expectedOrdinals);
    }

    [Fact]
    public void Query_StatusAndMultipleTags_ReturnsTripleIntersection()
    {
        // passed AND @smoke AND @regression → intersection of {0,3,4,6,9} ∩ {0,1,2,3,4,5,9,10,11} ∩ {0,1,3,8} = {0,3}
        var result = ExecuteQuery(status: "passed", tags: new[] { "smoke", "regression" });
        AssertOrdinals(result, new[] { 0, 3 });
    }

    [Fact]
    public void Query_StatusAndTag_EmptyIntersection()
    {
        // passed AND @slow → {0,3,4,6,9} ∩ {2,7} = empty
        var result = ExecuteQuery(status: "passed", tags: new[] { "slow" });
        Assert.Empty(result);
    }

    // ================================================================
    // 6. Status + Text Combined Queries
    // ================================================================

    [Theory]
    [InlineData("passed", "login", new[] { 0 })]
    [InlineData("failed", "login", new[] { 1 })]
    // "login" matches ordinals [0,1] only; skipped=[2,11]; intersection is empty
    [InlineData("passed", "cart", new[] { 3, 4 })]
    [InlineData("failed", "payment", new[] { 8 })]
    [InlineData("untested", "payment", new[] { 7 })]
    public void Query_StatusAndText_ReturnsIntersection(string status, string query, int[] expectedOrdinals)
    {
        var result = ExecuteQuery(status: status, query: query);
        AssertOrdinals(result, expectedOrdinals);
    }

    [Fact]
    public void Query_SkippedAndLogin_EmptyIntersection()
    {
        // skipped AND "login" → {2,11} ∩ {0,1} = empty
        // "login" only matches scenarios 0,1 (not scenario 2 "Account lockout...")
        var result = ExecuteQuery(status: "skipped", query: "login");
        Assert.Empty(result);
    }

    [Fact]
    public void Query_StatusAndText_EmptyIntersection()
    {
        // untested AND "cart" → {7,10} ∩ {3,4,5} = empty
        var result = ExecuteQuery(status: "untested", query: "cart");
        Assert.Empty(result);
    }

    // ================================================================
    // 7. Tag + Text Combined Queries
    // ================================================================

    [Theory]
    [InlineData(new[] { "regression" }, "login", new[] { 0, 1 })]
    [InlineData(new[] { "critical" }, "payment", new[] { 6, 7 })]
    [InlineData(new[] { "smoke" }, "cart", new[] { 3, 4, 5 })]
    [InlineData(new[] { "security" }, "payment", new[] { 6, 7, 8 })]
    [InlineData(new[] { "api" }, "refund", new[] { 8 })]
    public void Query_TagAndText_ReturnsIntersection(string[] tags, string query, int[] expectedOrdinals)
    {
        var result = ExecuteQuery(tags: tags, query: query);
        AssertOrdinals(result, expectedOrdinals);
    }

    [Fact]
    public void Query_TagAndText_EmptyIntersection()
    {
        // @api AND "login" → {6,7,8} ∩ {0,1} = empty
        var result = ExecuteQuery(tags: new[] { "api" }, query: "login");
        Assert.Empty(result);
    }

    // ================================================================
    // 8. Status + Tag + Text Triple Combined Queries
    // ================================================================

    [Theory]
    [InlineData("passed", new[] { "smoke" }, "login", new[] { 0 })]
    [InlineData("failed", new[] { "regression" }, "refund", new[] { 8 })]
    [InlineData("passed", new[] { "security" }, "payment", new[] { 6 })]
    [InlineData("untested", new[] { "critical" }, "payment", new[] { 7 })]
    [InlineData("passed", new[] { "smoke", "ecommerce" }, "cart", new[] { 3, 4 })]
    public void Query_StatusTagAndText_ReturnsTripleIntersection(
        string status, string[] tags, string query, int[] expectedOrdinals)
    {
        var result = ExecuteQuery(status: status, tags: tags, query: query);
        AssertOrdinals(result, expectedOrdinals);
    }

    [Fact]
    public void Query_TripleCombination_EmptyResult()
    {
        // passed AND @slow AND "login" → {0,3,4,6,9} ∩ {2,7} ∩ {0,1} = empty
        var result = ExecuteQuery(status: "passed", tags: new[] { "slow" }, query: "login");
        Assert.Empty(result);
    }

    [Fact]
    public void Query_TripleCombination_SingleResult()
    {
        // failed AND @security AND "refund" → {1,5,8} ∩ {0,1,2,6,7,8,10} ∩ {8 (refund)} = {8}
        var result = ExecuteQuery(status: "failed", tags: new[] { "security" }, query: "refund");
        AssertOrdinals(result, new[] { 8 });
    }

    // ================================================================
    // 9. Feature Ordinal Derivation Tests
    // ================================================================

    [Fact]
    public void Query_Passed_DerivedFeatureOrdinals_AreCorrect()
    {
        var scenarioOrdinals = ExecuteQuery(status: "passed");
        // Passed: 0→F0, 3→F1, 4→F1, 6→F2, 9→F3
        var featureOrdinals = DeriveFeatureOrdinals(scenarioOrdinals);
        AssertOrdinals(featureOrdinals, new[] { 0, 1, 2, 3 });
    }

    [Fact]
    public void Query_ApiTag_DerivedFeatureOrdinals_AreCorrect()
    {
        var scenarioOrdinals = ExecuteQuery(tags: new[] { "api" });
        // @api: 6→F2, 7→F2, 8→F2
        var featureOrdinals = DeriveFeatureOrdinals(scenarioOrdinals);
        AssertOrdinals(featureOrdinals, new[] { 2 });
    }

    [Fact]
    public void Query_FailedAndRegression_DerivedFeatureOrdinals()
    {
        var scenarioOrdinals = ExecuteQuery(status: "failed", tags: new[] { "regression" });
        // {1,8} → F0, F2
        var featureOrdinals = DeriveFeatureOrdinals(scenarioOrdinals);
        AssertOrdinals(featureOrdinals, new[] { 0, 2 });
    }

    // ================================================================
    // 10. Determinism and Reproducibility Tests
    // ================================================================

    [Fact]
    public void Query_SameInputs_ProduceSameResults_AcrossMultipleRuns()
    {
        var results = new List<List<int>>();
        for (int i = 0; i < 10; i++)
        {
            results.Add(ExecuteQuery(status: "passed", tags: new[] { "smoke" }, query: "cart"));
        }

        var first = results[0];
        foreach (var result in results.Skip(1))
        {
            Assert.Equal(first, result);
        }
    }

    [Fact]
    public void Index_RebuildFromSameDataset_ProducesIdenticalIndex()
    {
        var doc = BuildCanonicalDataset();
        var index2 = _indexBuilder.BuildIndex(doc, "golden-build-002");

        // ScenarioRecords must be identical (except buildId)
        Assert.Equal(_index.ScenarioRecords.Count, index2.ScenarioRecords.Count);
        for (int i = 0; i < _index.ScenarioRecords.Count; i++)
        {
            Assert.Equal(_index.ScenarioRecords[i].ScenarioOrdinal, index2.ScenarioRecords[i].ScenarioOrdinal);
            Assert.Equal(_index.ScenarioRecords[i].FeatureOrdinal, index2.ScenarioRecords[i].FeatureOrdinal);
            Assert.Equal(_index.ScenarioRecords[i].Name, index2.ScenarioRecords[i].Name);
            Assert.Equal(_index.ScenarioRecords[i].Status, index2.ScenarioRecords[i].Status);
            Assert.Equal(_index.ScenarioRecords[i].Tokens, index2.ScenarioRecords[i].Tokens);
            Assert.Equal(_index.ScenarioRecords[i].Tags, index2.ScenarioRecords[i].Tags);
        }

        // Inverted indexes must be identical
        Assert.Equal(_index.InvertedTokenIndex.Count, index2.InvertedTokenIndex.Count);
        foreach (var kvp in _index.InvertedTokenIndex)
        {
            Assert.True(index2.InvertedTokenIndex.ContainsKey(kvp.Key));
            Assert.Equal(kvp.Value, index2.InvertedTokenIndex[kvp.Key]);
        }

        // Status index must be identical
        Assert.Equal(_index.StatusIndex.Passed, index2.StatusIndex.Passed);
        Assert.Equal(_index.StatusIndex.Failed, index2.StatusIndex.Failed);
        Assert.Equal(_index.StatusIndex.Skipped, index2.StatusIndex.Skipped);
        Assert.Equal(_index.StatusIndex.Untested, index2.StatusIndex.Untested);

        // Tag index must be identical
        Assert.Equal(_index.TagIndex.Count, index2.TagIndex.Count);
        foreach (var kvp in _index.TagIndex)
        {
            Assert.True(index2.TagIndex.ContainsKey(kvp.Key));
            Assert.Equal(kvp.Value, index2.TagIndex[kvp.Key]);
        }
    }

    // ================================================================
    // 11. Edge Case Queries
    // ================================================================

    [Fact]
    public void Query_EmptyTextFilter_ReturnsAll()
    {
        var result = ExecuteQuery(query: "");
        AssertOrdinals(result, Enumerable.Range(0, 12).ToArray());
    }

    [Fact]
    public void Query_NullParameters_ReturnsAll()
    {
        var result = ExecuteQuery(query: null, status: null, tags: null);
        AssertOrdinals(result, Enumerable.Range(0, 12).ToArray());
    }

    [Fact]
    public void Query_EmptyTagArray_ReturnsAll()
    {
        var result = ExecuteQuery(tags: Array.Empty<string>());
        AssertOrdinals(result, Enumerable.Range(0, 12).ToArray());
    }

    [Fact]
    public void Query_SingleCharacterText_ReturnsAll()
    {
        // TokenizerService filters tokens with length < 2, so "a" normalizes to zero tokens.
        // With no text constraint, all scenarios are returned (no filter applied).
        var result = ExecuteQuery(query: "a");
        AssertOrdinals(result, Enumerable.Range(0, 12).ToArray());
    }

    [Fact]
    public void Query_TagCaseInsensitive_ReturnsSameResults()
    {
        var lower = ExecuteQuery(tags: new[] { "smoke" });
        var upper = ExecuteQuery(tags: new[] { "SMOKE" });
        var mixed = ExecuteQuery(tags: new[] { "Smoke" });

        AssertOrdinals(lower, upper.ToArray());
        AssertOrdinals(lower, mixed.ToArray());
    }

    [Fact]
    public void Query_TagWithAtPrefix_ReturnsSameResults()
    {
        // Tags in the dataset are stored with @, but TagIndex normalizes by trimming @
        var withAt = ExecuteQuery(tags: new[] { "@smoke" });
        var withoutAt = ExecuteQuery(tags: new[] { "smoke" });
        AssertOrdinals(withAt, withoutAt.ToArray());
    }

    // ================================================================
    // 12. Exhaustive Combinatorial Coverage
    // ================================================================

    [Fact]
    public void Exhaustive_AllSingleStatusQueries_CoverAllScenarios()
    {
        var allStatuses = new[] { "passed", "failed", "skipped", "untested" };
        var allResults = new HashSet<int>();

        foreach (var status in allStatuses)
        {
            var result = ExecuteQuery(status: status);
            foreach (var ordinal in result)
            {
                allResults.Add(ordinal);
            }
        }

        // Union of all status queries must cover all 12 scenarios
        Assert.Equal(12, allResults.Count);
        AssertOrdinals(allResults.OrderBy(x => x).ToList(), Enumerable.Range(0, 12).ToArray());
    }

    [Fact]
    public void Exhaustive_StatusQueries_AreDisjoint()
    {
        var passed = ExecuteQuery(status: "passed");
        var failed = ExecuteQuery(status: "failed");
        var skipped = ExecuteQuery(status: "skipped");
        var untested = ExecuteQuery(status: "untested");

        // No scenario should appear in more than one status bucket
        Assert.Empty(passed.Intersect(failed));
        Assert.Empty(passed.Intersect(skipped));
        Assert.Empty(passed.Intersect(untested));
        Assert.Empty(failed.Intersect(skipped));
        Assert.Empty(failed.Intersect(untested));
        Assert.Empty(skipped.Intersect(untested));
    }

    [Fact]
    public void Exhaustive_AllStatusTagCombinations_AreSubsetOfBothFilters()
    {
        var allStatuses = new[] { "passed", "failed", "skipped", "untested" };
        var allTags = _index.TagIndex.Keys.ToArray();

        foreach (var status in allStatuses)
        {
            var statusResults = ExecuteQuery(status: status);

            foreach (var tag in allTags)
            {
                var tagResults = ExecuteQuery(tags: new[] { tag });
                var combined = ExecuteQuery(status: status, tags: new[] { tag });

                // Combined result must be subset of both individual results
                Assert.True(combined.All(x => statusResults.Contains(x)),
                    $"Combined({status}, {tag}) not subset of status({status})");
                Assert.True(combined.All(x => tagResults.Contains(x)),
                    $"Combined({status}, {tag}) not subset of tag({tag})");

                // Combined result must equal the intersection
                var expected = statusResults.Intersect(tagResults).OrderBy(x => x).ToList();
                AssertOrdinals(combined, expected.ToArray());
            }
        }
    }

    [Fact]
    public void Exhaustive_DoubleTagCombinations_AreSubsetOfBothSingleTags()
    {
        var allTags = _index.TagIndex.Keys.OrderBy(x => x).ToArray();

        for (int i = 0; i < allTags.Length; i++)
        {
            for (int j = i + 1; j < allTags.Length; j++)
            {
                var tag1Results = ExecuteQuery(tags: new[] { allTags[i] });
                var tag2Results = ExecuteQuery(tags: new[] { allTags[j] });
                var combined = ExecuteQuery(tags: new[] { allTags[i], allTags[j] });

                // Must be subset of both
                Assert.True(combined.All(x => tag1Results.Contains(x)),
                    $"Combined({allTags[i]}, {allTags[j]}) not subset of tag({allTags[i]})");
                Assert.True(combined.All(x => tag2Results.Contains(x)),
                    $"Combined({allTags[i]}, {allTags[j]}) not subset of tag({allTags[j]})");

                // Must equal intersection
                var expected = tag1Results.Intersect(tag2Results).OrderBy(x => x).ToList();
                AssertOrdinals(combined, expected.ToArray());
            }
        }
    }

    // ================================================================
    // Query Evaluator (mirrors JS worker set-intersection logic)
    // ================================================================

    /// <summary>
    /// Executes a query against the golden index using the same
    /// set-intersection algorithm as the search worker.
    /// </summary>
    private List<int> ExecuteQuery(
        string? query = null,
        string? status = null,
        string[]? tags = null)
    {
        var candidateSets = new List<List<int>>();

        // 1. Status filter
        if (!string.IsNullOrEmpty(status) && status != "all")
        {
            var statusPostings = GetStatusPostings(status);
            if (statusPostings == null)
                return new List<int>();
            candidateSets.Add(statusPostings);
        }

        // 2. Tag filter (AND across all tags)
        if (tags != null && tags.Length > 0)
        {
            foreach (var tag in tags)
            {
                var normalizedTag = tag.TrimStart('@').ToLowerInvariant();
                if (_index.TagIndex.TryGetValue(normalizedTag, out var tagPostings))
                {
                    candidateSets.Add(tagPostings);
                }
                else
                {
                    return new List<int>(); // Unknown tag → empty result
                }
            }
        }

        // 3. Text filter (find matching token postings, union, then intersect)
        if (!string.IsNullOrEmpty(query))
        {
            var tokenizer = new TokenizerService();
            var queryTokens = tokenizer.Tokenize(query);

            if (queryTokens.Count == 0)
            {
                // Query normalized to no tokens → return all (no text constraint)
            }
            else
            {
                // For each query token, find postings (prefix match)
                var tokenUnion = new HashSet<int>();
                foreach (var qToken in queryTokens)
                {
                    // Exact match first
                    if (_index.InvertedTokenIndex.TryGetValue(qToken, out var exactPostings))
                    {
                        foreach (var p in exactPostings)
                            tokenUnion.Add(p);
                    }
                    else
                    {
                        // Prefix match (like the JS worker does)
                        foreach (var kvp in _index.InvertedTokenIndex)
                        {
                            if (kvp.Key.StartsWith(qToken, StringComparison.Ordinal))
                            {
                                foreach (var p in kvp.Value)
                                    tokenUnion.Add(p);
                            }
                        }
                    }
                }

                if (tokenUnion.Count == 0)
                    return new List<int>();

                candidateSets.Add(tokenUnion.OrderBy(x => x).ToList());
            }
        }

        // 4. Intersect all candidate sets (smallest first for efficiency)
        if (candidateSets.Count == 0)
        {
            // No filters → return all scenarios
            return Enumerable.Range(0, _index.ScenarioRecords.Count).ToList();
        }

        candidateSets.Sort((a, b) => a.Count.CompareTo(b.Count));

        var result = new HashSet<int>(candidateSets[0]);
        for (int i = 1; i < candidateSets.Count; i++)
        {
            result.IntersectWith(candidateSets[i]);
            if (result.Count == 0)
                return new List<int>();
        }

        return result.OrderBy(x => x).ToList();
    }

    /// <summary>
    /// Gets the postings list for a given status.
    /// </summary>
    private List<int>? GetStatusPostings(string status)
    {
        return status switch
        {
            "passed" => _index.StatusIndex.Passed,
            "failed" => _index.StatusIndex.Failed,
            "skipped" => _index.StatusIndex.Skipped,
            "untested" => _index.StatusIndex.Untested,
            _ => null
        };
    }

    /// <summary>
    /// Derives unique feature ordinals from scenario ordinals.
    /// </summary>
    private List<int> DeriveFeatureOrdinals(List<int> scenarioOrdinals)
    {
        return scenarioOrdinals
            .Select(so => _index.ScenarioRecords[so].FeatureOrdinal)
            .Distinct()
            .OrderBy(x => x)
            .ToList();
    }

    // ================================================================
    // Assertion Helpers
    // ================================================================

    private static void AssertOrdinals(IEnumerable<int> actual, int[] expected)
    {
        var actualSorted = actual.OrderBy(x => x).ToList();
        var expectedSorted = expected.OrderBy(x => x).ToList();
        Assert.Equal(expectedSorted, actualSorted);
    }

    // ================================================================
    // Canonical Dataset Builder
    // ================================================================

    private static LivingDocumentation BuildCanonicalDataset()
    {
        return new LivingDocumentation
        {
            Features = new List<EnrichedFeature>
            {
                // Feature 0: User Authentication
                CreateFeature("User Authentication",
                    tags: new[] { "@smoke", "@security" },
                    scenarios: new[]
                    {
                        CreateScenario("Successful login with valid credentials", ExecutionStatus.Passed, tags: new[] { "@regression" }),
                        CreateScenario("Failed login with invalid password", ExecutionStatus.Failed, tags: new[] { "@regression" }),
                        CreateScenario("Account lockout after 3 attempts", ExecutionStatus.Skipped, tags: new[] { "@slow" })
                    }),

                // Feature 1: Shopping Cart Management
                CreateFeature("Shopping Cart Management",
                    tags: new[] { "@smoke", "@ecommerce" },
                    scenarios: new[]
                    {
                        CreateScenario("Add item to empty cart", ExecutionStatus.Passed, tags: new[] { "@regression" }),
                        CreateScenario("Remove item from cart", ExecutionStatus.Passed),
                        CreateScenario("Apply discount coupon", ExecutionStatus.Failed, tags: new[] { "@ecommerce" })
                    }),

                // Feature 2: API Payment Processing
                CreateFeature("API Payment Processing",
                    tags: new[] { "@api", "@security" },
                    scenarios: new[]
                    {
                        CreateScenario("Process credit card payment", ExecutionStatus.Passed, tags: new[] { "@critical" }),
                        CreateScenario("Handle payment timeout", ExecutionStatus.NotExecuted, tags: new[] { "@critical", "@slow" }),
                        CreateScenario("Refund completed order", ExecutionStatus.Failed, tags: new[] { "@regression" })
                    }),

                // Feature 3: User Profile
                CreateFeature("User Profile",
                    tags: new[] { "@smoke" },
                    scenarios: new[]
                    {
                        CreateScenario("Update email address", ExecutionStatus.Passed),
                        CreateScenario("Change password", ExecutionStatus.NotExecuted, tags: new[] { "@security" }),
                        CreateScenario("Delete account", ExecutionStatus.Skipped, tags: new[] { "@critical" })
                    })
            }
        };
    }

    private static EnrichedFeature CreateFeature(
        string name, string[]? tags = null, EnrichedScenario[]? scenarios = null)
    {
        return new EnrichedFeature
        {
            Feature = new UniversalFeature
            {
                Name = name,
                Description = string.Empty,
                FilePath = $"/features/{name.ToLower().Replace(' ', '_')}.feature",
                Tags = tags?.ToList() ?? new List<string>()
            },
            Scenarios = scenarios?.ToList() ?? new List<EnrichedScenario>()
        };
    }

    private static EnrichedScenario CreateScenario(
        string name, ExecutionStatus status, string[]? tags = null)
    {
        return new EnrichedScenario
        {
            Scenario = new UniversalScenario
            {
                Name = name,
                Description = string.Empty,
                Tags = tags?.ToList() ?? new List<string>()
            },
            Status = status
        };
    }
}

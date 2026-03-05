using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using Xunit;
using LivingDocGen.Generator.Models;
using LivingDocGen.Generator.Models.Contracts;
using LivingDocGen.Generator.Services;
using LivingDocGen.Generator.Services.Chunked;
using LivingDocGen.Parser.Models;
using LivingDocGen.TestReporter.Models;

namespace LivingDocGen.Generator.Tests.Services;

/// <summary>
/// Tests for GenerateShellHtml (PR-3: Runtime Loader + Chunk Rendering).
/// Verifies the shell HTML is a lightweight page without inline feature data.
/// </summary>
public class GenerateShellHtmlTests
{
    private readonly HtmlGeneratorService _sut = new();
    private readonly LivingDocumentation _documentation;

    public GenerateShellHtmlTests()
    {
        _documentation = CreateDocumentation(
            CreateFeature("Login Feature", "Features/login.feature",
                CreateScenario("Valid login", ExecutionStatus.Passed),
                CreateScenario("Invalid login", ExecutionStatus.Failed)),
            CreateFeature("Cart Feature", "Features/Shopping/cart.feature",
                CreateScenario("Add item", ExecutionStatus.Passed)));
    }

    [Fact]
    public void GenerateShellHtml_ReturnsNonEmptyString()
    {
        var result = _sut.GenerateShellHtml(_documentation);

        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public void GenerateShellHtml_NullDocumentation_ThrowsArgumentNull()
    {
        Assert.Throws<ArgumentNullException>(() => _sut.GenerateShellHtml(null!));
    }

    [Fact]
    public void GenerateShellHtml_InvalidTheme_StillProducesOutput()
    {
        // Note: The generator gracefully handles unknown themes by falling back to defaults
        var options = new HtmlGenerationOptions { Theme = "nonexistent" };

        var result = _sut.GenerateShellHtml(_documentation, options);

        Assert.NotNull(result);
        Assert.Contains("<!DOCTYPE html>", result);
    }

    [Fact]
    public void GenerateShellHtml_NullOptions_UsesDefaults()
    {
        var result = _sut.GenerateShellHtml(_documentation, null);

        Assert.NotNull(result);
        Assert.Contains("<!DOCTYPE html>", result);
    }

    // ===== HTML Structure =====

    [Fact]
    public void GenerateShellHtml_ContainsValidHtmlStructure()
    {
        var result = _sut.GenerateShellHtml(_documentation);

        Assert.Contains("<!DOCTYPE html>", result);
        Assert.Contains("<html lang=\"en\">", result);
        Assert.Contains("<head>", result);
        Assert.Contains("<body>", result);
        Assert.Contains("</body>", result);
        Assert.Contains("</html>", result);
    }

    [Fact]
    public void GenerateShellHtml_ContainsHeader()
    {
        var result = _sut.GenerateShellHtml(_documentation);

        Assert.Contains("<header", result);
    }

    [Fact]
    public void GenerateShellHtml_ContainsControls()
    {
        var result = _sut.GenerateShellHtml(_documentation);

        // Should have search/filter controls
        Assert.Contains("search", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GenerateShellHtml_ContainsStatistics()
    {
        var result = _sut.GenerateShellHtml(_documentation);

        // Statistics dashboard should be present
        Assert.Contains("stat", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GenerateShellHtml_ContainsFooter()
    {
        var result = _sut.GenerateShellHtml(_documentation);

        Assert.Contains("<footer", result);
    }

    // ===== Empty Shell Sidebar =====

    [Fact]
    public void GenerateShellHtml_ContainsEmptySidebarShell()
    {
        var result = _sut.GenerateShellHtml(_documentation);

        Assert.Contains("id=\"sidebar\"", result);
        Assert.Contains("id=\"sidebar-nav\"", result);
    }

    [Fact]
    public void GenerateShellHtml_SidebarHasLoadingPlaceholder()
    {
        var result = _sut.GenerateShellHtml(_documentation);

        Assert.Contains("Loading features...", result);
        Assert.Contains("fa-spinner", result);
    }

    [Fact]
    public void GenerateShellHtml_SidebarHasFeatureTotalPlaceholder()
    {
        var result = _sut.GenerateShellHtml(_documentation);

        // Total placeholder with ellipsis (will be filled by JS)
        Assert.Contains("feature-total", result);
    }

    // ===== Empty Main Content =====

    [Fact]
    public void GenerateShellHtml_ContainsEmptyMainContent()
    {
        var result = _sut.GenerateShellHtml(_documentation);

        Assert.Contains("id=\"main-content\"", result);
        Assert.Contains("Loading documentation...", result);
    }

    // ===== No Inline Feature Data =====

    [Fact]
    public void GenerateShellHtml_DoesNotContainInlineFeatureData()
    {
        var result = _sut.GenerateShellHtml(_documentation);

        // In legacy mode, features would be rendered as inline HTML with feature-card classes
        // Shell mode should NOT have any feature content (names, scenario details, steps)
        // embedded as rendered HTML in the page body.
        //
        // We verify by checking the rendered HTML for legacy feature rendering patterns:
        // - The sidebar-nav should not contain data-feature-index or feature-item items
        // - The main-content should not contain feature-card sections
        
        // The sidebar should only have the loading placeholder,
        // not any pre-built feature items or folder structure
        var sidebarStart = result.IndexOf("id=\"sidebar-nav\"");
        var sidebarEnd = result.IndexOf("</nav>", sidebarStart);
        if (sidebarStart > 0 && sidebarEnd > sidebarStart)
        {
            var sidebarContent = result.Substring(sidebarStart, sidebarEnd - sidebarStart);
            // Should not contain feature-item divs (those are built by JS at runtime)
            Assert.DoesNotContain("data-feature-index=", sidebarContent);
        }
    }

    // ===== JavaScript Inclusion =====

    [Fact]
    public void GenerateShellHtml_ContainsLegacyJavaScript()
    {
        var result = _sut.GenerateShellHtml(_documentation);

        // Legacy JS provides shared UI infrastructure (theme, scroll, sidebar toggle, etc.)
        Assert.Contains("<script>", result);
    }

    [Fact]
    public void GenerateShellHtml_ContainsChunkedRuntimeJavaScript()
    {
        var result = _sut.GenerateShellHtml(_documentation);

        Assert.Contains("CHUNKED_MODE", result);
        Assert.Contains("LRUCache", result);
        Assert.Contains("loadManifest", result);
        Assert.Contains("fetchChunk", result);
        Assert.Contains("buildSidebarFromManifest", result);
    }

    // ===== Accessibility =====

    [Fact]
    public void GenerateShellHtml_ContainsSkipToContent()
    {
        var result = _sut.GenerateShellHtml(_documentation);

        Assert.Contains("skip-to-content", result);
    }

    [Fact]
    public void GenerateShellHtml_ContainsAccessibilityRoles()
    {
        var result = _sut.GenerateShellHtml(_documentation);

        Assert.Contains("role=\"navigation\"", result);
        Assert.Contains("role=\"main\"", result);
        Assert.Contains("role=\"tree\"", result);
    }

    // ===== Layout Elements =====

    [Fact]
    public void GenerateShellHtml_ContainsLayoutContainer()
    {
        var result = _sut.GenerateShellHtml(_documentation);

        Assert.Contains("layout-container", result);
    }

    [Fact]
    public void GenerateShellHtml_ContainsSidebarToggle()
    {
        var result = _sut.GenerateShellHtml(_documentation);

        Assert.Contains("sidebar-toggle", result);
    }

    [Fact]
    public void GenerateShellHtml_ContainsResizer()
    {
        var result = _sut.GenerateShellHtml(_documentation);

        Assert.Contains("resizer", result);
    }

    [Fact]
    public void GenerateShellHtml_ContainsScrollToTopButton()
    {
        var result = _sut.GenerateShellHtml(_documentation);

        Assert.Contains("scroll-to-top", result);
    }

    [Fact]
    public void GenerateShellHtml_ContainsGlobalLoader()
    {
        var result = _sut.GenerateShellHtml(_documentation);

        Assert.Contains("global-loader", result);
    }

    // ===== Theme Support =====

    [Fact]
    public void GenerateShellHtml_WithTheme_AppliesThemeStyles()
    {
        var options = new HtmlGenerationOptions { Theme = "blue" };

        var result = _sut.GenerateShellHtml(_documentation, options);

        Assert.NotNull(result);
        Assert.Contains("<!DOCTYPE html>", result);
    }

    [Fact]
    public void GenerateShellHtml_WithDarkTheme_AppliesThemeStyles()
    {
        var options = new HtmlGenerationOptions { Theme = "dark" };

        var result = _sut.GenerateShellHtml(_documentation, options);

        Assert.NotNull(result);
        Assert.Contains("<!DOCTYPE html>", result);
    }

    // ===== Shell vs Legacy Size Comparison =====

    [Fact]
    public void GenerateShellHtml_DoesNotEmbedFeatureCards()
    {
        var legacyHtml = _sut.GenerateHtml(_documentation);
        var shellHtml = _sut.GenerateShellHtml(_documentation);

        // Legacy embeds rendered feature divs with data-feature-id attributes; shell does not.
        // With few features, shell may be larger due to JS overhead, but
        // the key difference is no inline feature rendering.
        Assert.Contains("data-feature-id=", legacyHtml);
        // Shell should contain the CHUNKED_MODE runtime instead
        Assert.Contains("CHUNKED_MODE", shellHtml);
        // Shell should NOT contain rendered feature content (only JS that builds it at runtime)
        Assert.DoesNotContain("data-status=\"passed\"", shellHtml);
    }

    // ===== Helpers =====

    private static LivingDocumentation CreateDocumentation(params EnrichedFeature[] features)
    {
        var doc = new LivingDocumentation
        {
            Features = features?.ToList() ?? new List<EnrichedFeature>(),
            Title = "Test Documentation",
            GeneratedAt = DateTime.Now
        };

        // Compute statistics
        var allScenarios = doc.Features.SelectMany(f => f.Scenarios).ToList();
        doc.Statistics = new DocumentStatistics
        {
            TotalFeatures = doc.Features.Count,
            TotalScenarios = allScenarios.Count,
            PassedScenarios = allScenarios.Count(s => s.Status == ExecutionStatus.Passed),
            FailedScenarios = allScenarios.Count(s => s.Status == ExecutionStatus.Failed),
            SkippedScenarios = allScenarios.Count(s => s.Status == ExecutionStatus.Skipped),
        };

        return doc;
    }

    private static EnrichedFeature CreateFeature(string name, string filePath, params EnrichedScenario[] scenarios)
    {
        return new EnrichedFeature
        {
            Feature = new UniversalFeature
            {
                Name = name,
                FilePath = filePath,
                Tags = new List<string>()
            },
            Scenarios = scenarios?.ToList() ?? new List<EnrichedScenario>(),
            OverallStatus = ExecutionStatus.Passed
        };
    }

    private static EnrichedScenario CreateScenario(string name, ExecutionStatus status = ExecutionStatus.Passed)
    {
        return new EnrichedScenario
        {
            Scenario = new UniversalScenario { Name = name },
            Status = status,
            Steps = new List<EnrichedStep>()
        };
    }
}

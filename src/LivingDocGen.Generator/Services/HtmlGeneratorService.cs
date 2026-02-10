using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LivingDocGen.Generator.Services;

using LivingDocGen.Generator.Models;
using LivingDocGen.Generator.Services.Assets;
using LivingDocGen.Parser.Models;
using LivingDocGen.TestReporter.Models;

/// <summary>
/// Generates self-contained HTML living documentation
/// 
/// PERFORMANCE OPTIMIZATIONS FOR LARGE REPORTS:
/// 1. StringBuilder pre-allocation: Estimates capacity based on feature count to reduce memory allocations
/// 2. HTML encoding cache: Caches encoded strings to avoid redundant encoding operations (up to 5000 unique strings)
/// 3. CSS theme cache: Static cache prevents regenerating CSS for the same theme across multiple reports
/// 4. Batched string operations: Uses chained Append() instead of multiple AppendLine() calls where possible
/// 5. JavaScript optimizations: Includes performance hints and detection for reports with 100+ features
/// 
/// PHASE 1 PERFORMANCE IMPROVEMENTS (v2.x):
/// 6. CSS Containment: Uses contain: layout style paint on .feature and .scenario for isolated rendering
/// 7. GPU Acceleration: transform properties trigger hardware acceleration for smooth animations
/// 8. Event Delegation: Single click listener instead of individual onclick handlers (massive improvement for 1000+ scenarios)
/// 9. requestAnimationFrame: Batches DOM updates for smooth 60fps animations
/// 10. Smart Debouncing: Adaptive debounce delays (300ms standard, 400ms for large reports >100 features)
/// 11. Read/Write Batching: Separates DOM reads and writes to prevent layout thrashing
/// 12. Optimized Transitions: Uses max-height instead of display:none for smoother expand/collapse
/// 
/// RECOMMENDED LIMITS:
/// - Optimal performance: < 50 features
/// - Good performance: 50-200 features  
/// - Acceptable: 200-500 features
/// - Large report mode: 500-1800+ features (Phase 1 optimizations active)
/// - Very large reports: > 2000 features (consider Phase 2: pagination or virtual scrolling)
/// 
/// PHASE 2 PERFORMANCE IMPROVEMENTS (v2.1+):
/// 13. Lazy Content Rendering: Feature bodies render only when scrolled into view (50+ features)
/// 14. Progressive Loading: Initial page shows structure, content loads as needed
/// 15. Optimized Event Delegation: Single delegated click handler for all toggles
/// 16. Reduced Observer Overhead: IntersectionObserver only monitors visible features
/// </summary>
public class HtmlGeneratorService : IHtmlGeneratorService
{
    // Capacity estimation constants
    private const int BaseHtmlCapacity = 10 * 1024; // 10 KB base HTML
    private const int PerFeatureCapacity = 5 * 1024; // 5 KB per feature
    private const int HeadSectionCapacity = 4 * 1024; // 4 KB for CSS/meta
    
    // Lazy rendering threshold (Phase 3 optimization: lowered from 50 to 30)
    private const int LazyRenderingThreshold = 30; // Enable lazy rendering for 30+ features
    
    // HTML encoding cache to avoid redundant encoding operations
    private readonly Dictionary<string, string> _encodingCache = new Dictionary<string, string>(StringComparer.Ordinal);
    private readonly object _encodingCacheLock = new object();
    private const int MaxEncodingCacheSize = 5000; // Prevent unbounded growth
    
    // HTML generation options (set during GenerateHtml call)
    private HtmlGenerationOptions _currentOptions;
    
    // Feature index counter for sidebar generation
    private int _sidebarFeatureIndex = 0;
    
    // Extracted services for CSS and JavaScript generation
    private readonly ICssGenerator _cssGenerator;
    private readonly IJavaScriptGenerator _jsGenerator;
    
    /// <summary>
    /// Initializes a new instance of the HtmlGeneratorService with default service implementations
    /// </summary>
    public HtmlGeneratorService()
    {
        _cssGenerator = new CssGenerator();
        _jsGenerator = new JavaScriptGenerator();
    }
    
    /// <summary>
    /// Initializes a new instance with custom service implementations (for testing/DI)
    /// </summary>
    public HtmlGeneratorService(ICssGenerator cssGenerator, IJavaScriptGenerator jsGenerator)
    {
        _cssGenerator = cssGenerator ?? throw new ArgumentNullException(nameof(cssGenerator));
        _jsGenerator = jsGenerator ?? throw new ArgumentNullException(nameof(jsGenerator));
    }
    
    /// <summary>
    /// Cached HTML encoding with thread safety
    /// </summary>
    private string HtmlEncode(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;
        
        lock (_encodingCacheLock)
        {
            if (_encodingCache.TryGetValue(text, out var encoded))
                return encoded;
                
            // Clear cache if it gets too large
            if (_encodingCache.Count >= MaxEncodingCacheSize)
                _encodingCache.Clear();
                
            encoded = System.Web.HttpUtility.HtmlEncode(text);
            _encodingCache[text] = encoded;
            return encoded;
        }
    }
    
    public string GenerateHtml(LivingDocumentation documentation, HtmlGenerationOptions options = null)
    {
        // Input validation
        if (documentation == null)
            throw new ArgumentNullException(nameof(documentation));
        
        if (documentation.Features == null)
            throw new ArgumentException("Features collection cannot be null", nameof(documentation));
        
        options ??= new HtmlGenerationOptions();
        _currentOptions = options;
        
        // Validate theme exists
        if (!string.IsNullOrEmpty(options.Theme) && ThemeConfig.GetTheme(options.Theme) == null)
        {
            throw new ArgumentException($"Theme '{options.Theme}' not found. Use one of: purple, blue, green, red, dark, light", nameof(options));
        }
        
        // Reset state for each report generation
        _scenarioCounter = 0;
        lock (_encodingCacheLock)
        {
            _encodingCache.Clear();
        }
        
        // Pre-calculate approximate capacity for better performance
        var estimatedCapacity = BaseHtmlCapacity + (documentation.Features.Count * PerFeatureCapacity);
        var html = new StringBuilder(estimatedCapacity);
        
        // HTML Header
        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html lang=\"en\">");
        html.AppendLine(GenerateHead(documentation, options));
        html.AppendLine("<body>");
        
        // Skip to content link for accessibility
        html.AppendLine(@"
    <a href=""#main-content"" class=""skip-to-content"">Skip to main content</a>");
        
        // Global loading spinner (Phase 3 optimization)
        html.AppendLine(@"
    <div id=""global-loader"" class=""global-loader"">
        <div class=""spinner""></div>
        <p class=""loader-text"">Loading...</p>
    </div>");
        
        // Header
        html.AppendLine(GenerateHeader(documentation));
        
        // Controls (Search, Filters)
        html.AppendLine(GenerateControls());
        
        // Statistics Dashboard
        html.AppendLine(GenerateStatistics(documentation));
        
        // Master-Detail Layout Container
        html.AppendLine("<div class=\"layout-container\">");
        
        // Sidebar Navigation
        html.AppendLine(GenerateSidebar(documentation));
        
        // Floating Toggle Button (visible when sidebar is collapsed)
        html.AppendLine(@"
    <button id=""floating-sidebar-toggle"" title=""Show Sidebar (⌘B)"">
        <i class=""fas fa-bars""></i>
    </button>");
        
        // Resizer Handle
        html.AppendLine("<div class=\"resizer\"></div>");
        
        // Main Content Area
        html.AppendLine("<main id=\"main-content\" class=\"main-content\" role=\"main\" aria-label=\"Feature documentation\">");
        
        // Lazy rendering for large reports
        bool useLazyRendering = documentation.Features.Count >= LazyRenderingThreshold;
        
        if (useLazyRendering)
        {
            // For large reports: render feature containers with data attributes
            // JavaScript will populate content on demand
            for (int i = 0; i < documentation.Features.Count; i++)
            {
                var feature = documentation.Features[i];
                var featureId = $"feature-{i}";
                var hiddenClass = i == 0 ? "" : " feature-hidden";
                html.AppendLine($"<div class=\"feature lazy-feature{hiddenClass}\" id=\"{featureId}\" data-feature-id=\"{featureId}\" data-feature-index=\"{i}\" data-lazy=\"true\">");
                html.AppendLine("    <div class=\"lazy-placeholder\"><i class=\"fas fa-spinner fa-spin\"></i> Loading...</div>");
                html.AppendLine("</div>");
            }
        }
        else
        {
            // For smaller reports: render all content immediately
            for (int i = 0; i < documentation.Features.Count; i++)
            {
                html.AppendLine(GenerateFeature(documentation.Features[i], i));
            }
        }
        
        html.AppendLine("</main>");
        html.AppendLine("</div>"); // End layout-container
        
        // Scroll to Top Button
        html.AppendLine(@"
    <button id=""scroll-to-top"" title=""Back to top"" aria-label=""Scroll to top"">
        <i class=""fas fa-arrow-up""></i>
    </button>");
        
        // Embed feature data for lazy loading (if needed)
        if (useLazyRendering)
        {
            html.AppendLine("<script id=\"feature-data\" type=\"application/json\">");
            html.AppendLine(GenerateFeatureDataJson(documentation));
            html.AppendLine("</script>");
        }
        
        // Footer
        html.AppendLine(GenerateFooter(documentation));
        
        // Embedded JavaScript
        html.AppendLine(_jsGenerator.Generate(documentation, useLazyRendering));
        
        html.AppendLine("</body>");
        html.AppendLine("</html>");
        
        return html.ToString();
    }

    private string GenerateHead(LivingDocumentation documentation, HtmlGenerationOptions options)
    {
        var head = new StringBuilder(HeadSectionCapacity); // CSS is large, allocate enough space
        head.Append("<head>\n    <meta charset=\"UTF-8\">\n    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">\n    <title>")
            .Append(documentation.Title)
            .Append("</title>\n    <link rel=\"stylesheet\" href=\"https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.4.0/css/all.min.css\">\n    <style>\n")
            .Append(_cssGenerator.GetCSS(options))
            .Append("\n    </style>\n</head>\n");
        return head.ToString();
    }

    private string GenerateHeader(LivingDocumentation documentation)
    {
        return $@"
    <header>
        <div class=""container"">
            <h1><i class=""fas fa-book""></i> {documentation.Title}</h1>
            <p class=""subtitle"">Generated on {documentation.GeneratedAt:MMMM dd, yyyy 'at' HH:mm}</p>
        </div>
    </header>";
    }

    private string GenerateControls()
    {
        return @"
    <div id=""controls"" role=""search"" aria-label=""Search and filter controls"">
        <div class=""filter-group"" role=""group"" aria-label=""Status filters"">
            <button class=""filter-btn active"" data-filter=""all"" aria-label=""Show all scenarios"" aria-pressed=""true"">
                <i class=""fas fa-list"" aria-hidden=""true""></i> <span>All</span>
            </button>
            <button class=""filter-btn"" data-filter=""passed"" aria-label=""Show only passed scenarios"" aria-pressed=""false"">
                <i class=""fas fa-check-circle"" aria-hidden=""true""></i> <span>Passed</span>
            </button>
            <button class=""filter-btn"" data-filter=""failed"" aria-label=""Show only failed scenarios"" aria-pressed=""false"">
                <i class=""fas fa-times-circle"" aria-hidden=""true""></i> <span>Failed</span>
            </button>
            <button class=""filter-btn"" data-filter=""skipped"" aria-label=""Show only skipped scenarios"" aria-pressed=""false"">
                <i class=""fas fa-minus-circle"" aria-hidden=""true""></i> <span>Skipped</span>
            </button>
            <button class=""filter-btn"" data-filter=""untested"" aria-label=""Show only untested scenarios"" aria-pressed=""false"">
                <i class=""fas fa-circle"" aria-hidden=""true""></i> <span>Untested</span>
            </button>
        </div>
        <div class=""theme-group"" role=""group"" aria-label=""Tag and theme filters"">
            <select id=""tag-filter"" 
                    class=""theme-selector"" 
                    onchange=""filterByTag(this.value)""
                    aria-label=""Filter by tag"">
                <option value=""all"">🏷️ Tags</option>
            </select>
        </div>
        <div class=""search-wrapper"">
            <div class=""search-input-container"">
                <input type=""text"" 
                       id=""search-box"" 
                       placeholder=""Search features, scenarios...""
                       aria-label=""Search documentation""
                       aria-describedby=""search-result-count"">
                <button type=""button"" 
                        id=""search-clear-btn"" 
                        class=""search-clear-btn"" 
                        aria-label=""Clear search""
                        title=""Clear search (Esc)"">
                    <i class=""fas fa-times"" aria-hidden=""true""></i>
                </button>
            </div>
            <span id=""search-result-count"" class=""search-result-count"" aria-live=""polite""></span>
            <button type=""button"" 
                    id=""search-prev-btn"" 
                    class=""search-nav-btn"" 
                    aria-label=""Previous search result""
                    title=""Previous result (Shift+Enter)"">
                <i class=""fas fa-caret-up"" aria-hidden=""true""></i>
            </button>
            <button type=""button"" 
                    id=""search-next-btn"" 
                    class=""search-nav-btn"" 
                    aria-label=""Next search result""
                    title=""Next result (Enter)"">
                <i class=""fas fa-caret-down"" aria-hidden=""true""></i>
            </button>
        </div>
        <div class=""clear-all-group"">
            <button id=""clear-all-filters-btn"" class=""filter-btn clear-all-btn"" aria-label=""Clear all filters"" title=""Reset all filters"">
                <i class=""fas fa-eraser"" aria-hidden=""true""></i> <span>Clear All</span>
            </button>
        </div>
        <div class=""theme-selector-group"">
            <select id=""theme-selector"" 
                    class=""theme-selector"" 
                    onchange=""changeTheme(this.value)""
                    aria-label=""Select theme"">
                <option value=""purple"">🎨 Theme</option>
                <option value=""blue"">🌊 Blue</option>
                <option value=""green"">🌲 Green</option>
                <option value=""dark"">🌙 Dark</option>
                <option value=""light"">☀️ Light</option>
                <option value=""pickles"">🥒 Pickles</option>
            </select>
        </div>
    </div>";
    }

           private string GenerateStatistics(LivingDocumentation documentation)
    {
        var stats = documentation.Statistics;
        var failedCount = stats.FailedScenarios;
        var executedCount = stats.PassedScenarios + stats.FailedScenarios + stats.SkippedScenarios;
        
        // Determine action message based on test execution state
        string actionText;
        string actionIcon;
        string actionColor;
        
        if (executedCount == 0)
        {
            // No tests executed - no test results attached
            actionText = "No test results attached";
            actionIcon = "fa-info-circle";
            actionColor = "var(--info-color)";
        }
        else if (failedCount > 0)
        {
            // Some tests failed
            actionText = $"{failedCount} failure{(failedCount == 1 ? "" : "s")} need attention";
            actionIcon = "fa-exclamation-triangle";
            actionColor = "var(--danger-color)";
        }
        else if (stats.SkippedScenarios > 0 && stats.PassedScenarios == 0)
        {
            // All tests skipped
            actionText = "All tests skipped";
            actionIcon = "fa-minus-circle";
            actionColor = "var(--warning-color)";
        }
        else
        {
            // All executed tests passed
            actionText = "All tests passing!";
            actionIcon = "fa-check-circle";
            actionColor = "var(--success-color)";
        }
        
        return $@"
    <div id=""stats-container"">
        <button id=""stats-toggle"" onclick=""toggleStats()"" aria-expanded=""true"" aria-controls=""stats"">
            <div style=""display: flex; align-items: center; gap: 0.75rem;"">
                <i class=""fas fa-chart-bar""></i>
                <span>Statistics</span>
                <div class=""stats-summary"">
                    <span style=""color: var(--success-color);"">
                        <i class=""fas fa-check-circle""></i> {stats.PassRate:F1}% ({stats.PassedScenarios})
                    </span>
                    <span style=""color: var(--danger-color);"">
                        <i class=""fas fa-times-circle""></i> {stats.FailRate:F1}% ({stats.FailedScenarios})
                    </span>
                    <span style=""color: var(--warning-color);"">
                        <i class=""fas fa-minus-circle""></i> {stats.SkipRate:F1}% ({stats.SkippedScenarios})
                    </span>
                </div>
            </div>
            <div style=""display: flex; align-items: center; gap: 1rem;"">
                <span style=""color: {actionColor}; font-size: 0.9rem; font-weight: 500;"">
                    <i class=""fas {actionIcon}""></i> {actionText}
                </span>
                <i class=""fas fa-chevron-down""></i>
            </div>
        </button>
        <div id=""stats"">
            <div class=""stat-card stat-info clickable"" onclick=""filterByStatus('all')"" title=""Click to show all features"" role=""button"" tabindex=""0"">
                <div class=""label"">
                    <i class=""fas fa-file-alt icon""></i>
                    <span>Features</span>
                </div>
                <div class=""value"">{stats.TotalFeatures}</div>
            </div>
            <div class=""stat-card stat-info clickable"" onclick=""filterByStatus('all')"" title=""Click to show all scenarios"" role=""button"" tabindex=""0"">
                <div class=""label"">
                    <i class=""fas fa-list-check icon""></i>
                    <span>Scenarios</span>
                </div>
                <div class=""value"">{stats.TotalScenarios}</div>
            </div>
            <div class=""stat-card stat-passed clickable"" onclick=""filterByStatus('passed')"" title=""Click to show only passed scenarios"" role=""button"" tabindex=""0"">
                <div class=""label"">
                    <i class=""fas fa-check-circle icon""></i>
                    <span>Passed ({stats.PassRate:F1}%)</span>
                </div>
                <div class=""value"">{stats.PassedScenarios}</div>
            </div>
            <div class=""stat-card stat-failed clickable"" onclick=""filterByStatus('failed')"" title=""Click to show only failed scenarios"" role=""button"" tabindex=""0"">
                <div class=""label"">
                    <i class=""fas fa-times-circle icon""></i>
                    <span>Failed ({stats.FailRate:F1}%)</span>
                </div>
                <div class=""value"">{stats.FailedScenarios}</div>
            </div>
            <div class=""stat-card stat-skipped clickable"" onclick=""filterByStatus('skipped')"" title=""Click to show only skipped scenarios"" role=""button"" tabindex=""0"">
                <div class=""label"">
                    <i class=""fas fa-minus-circle icon""></i>
                    <span>Skipped ({stats.SkipRate:F1}%)</span>
                </div>
                <div class=""value"">{stats.SkippedScenarios}</div>
            </div>
            <div class=""stat-card stat-info clickable"" onclick=""filterByStatus('untested')"" title=""Click to show untested scenarios"" role=""button"" tabindex=""0"">
                <div class=""label"">
                    <i class=""fas fa-question-circle icon""></i>
                    <span>Untested</span>
                </div>
                <div class=""value"">{stats.UntestedScenarios}</div>
            </div>
            <div class=""stat-card stat-info"" title=""Test coverage percentage"">
                <div class=""label"">
                    <i class=""fas fa-chart-line icon""></i>
                    <span>Coverage</span>
                </div>
                <div class=""value"">{stats.Coverage:F1}%</div>
            </div>
        </div>
    </div>";
    }

    private string GenerateSidebar(LivingDocumentation documentation)
    {
        var html = new StringBuilder();
        var totalFeatures = documentation.Features.Count;
        
        html.AppendLine($@"
    <aside id=""sidebar"" class=""sidebar"" role=""navigation"" aria-label=""Feature navigation"">
        <div class=""sidebar-header"">
            <h3><i class=""fas fa-folder-tree"" aria-hidden=""true""></i> Features <span class=""feature-total"">({totalFeatures})</span></h3>
            <div class=""sidebar-actions"">
                <button id=""toggle-folders-btn"" 
                        class=""sidebar-action-btn""
                        title=""Collapse All Folders""
                        onclick=""toggleAllFolders()""
                        data-state=""expanded"">
                    <i class=""fas fa-folder-open""></i>
                </button>
                <button id=""sidebar-toggle"" 
                        class=""sidebar-toggle"" 
                        title=""Toggle Sidebar (⌘B)""
                        aria-label=""Toggle sidebar navigation""
                        aria-expanded=""true"">
                    <i class=""fas fa-angles-left""></i>
                </button>
            </div>
        </div>
        
        <nav class=""sidebar-nav"" id=""sidebar-nav"" role=""tree"" aria-label=""Features tree"">");
        
        // Build nested folder tree structure
        _sidebarFeatureIndex = 0; // Reset counter for each sidebar generation
        var rootNode = BuildNestedFolderTree(documentation.Features);
        html.AppendLine(GenerateNestedFolderTree(rootNode, 0));
        
        html.AppendLine(@"
        </nav>
    </aside>");
        
        return html.ToString();
    }

    /// <summary>
    /// Builds a nested folder tree structure from feature file paths.
    /// Strips the common base path to show only relative folder structure.
    /// Uses the last segment of the common base path as the root folder name (e.g., "Features").
    /// </summary>
    private FolderNode BuildNestedFolderTree(List<EnrichedFeature> features)
    {
        // Find the common base path to strip from all feature paths
        var commonBasePath = FindCommonBasePath(features);
        
        // Extract root folder name from common base path (e.g., "Features" from "/path/to/Features")
        var rootFolderName = "Features"; // Default name
        if (!string.IsNullOrEmpty(commonBasePath))
        {
            var segments = commonBasePath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (segments.Any())
            {
                rootFolderName = segments.Last();
            }
        }
        
        var root = new FolderNode { Name = rootFolderName, FullPath = rootFolderName };
        
        // Create a lookup for proper ordering - maintain original feature order within folders
        var orderedFeatures = features.Select((f, i) => new { Feature = f, Index = i }).ToList();
        
        foreach (var item in orderedFeatures)
        {
            var feature = item.Feature;
            var filePath = feature.Feature.FilePath ?? "";
            
            if (string.IsNullOrEmpty(filePath))
            {
                // Features without path go to root
                root.Features.Add(feature);
                continue;
            }
            
            // Normalize path separators and get directory path
            var normalizedPath = filePath.Replace('\\', '/');
            var directory = Path.GetDirectoryName(normalizedPath)?.Replace('\\', '/') ?? "";
            
            // Strip the common base path to get relative directory
            if (!string.IsNullOrEmpty(commonBasePath) && directory.StartsWith(commonBasePath, StringComparison.OrdinalIgnoreCase))
            {
                directory = directory.Substring(commonBasePath.Length).TrimStart('/');
            }
            
            if (string.IsNullOrEmpty(directory))
            {
                // Features in root directory (after stripping base path)
                root.Features.Add(feature);
                continue;
            }
            
            // Split path into folder segments
            var segments = directory.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            
            // Navigate/create the folder hierarchy
            var currentNode = root;
            var currentPath = "";
            
            foreach (var segment in segments)
            {
                currentPath = string.IsNullOrEmpty(currentPath) ? segment : $"{currentPath}/{segment}";
                
                if (!currentNode.SubFolders.ContainsKey(segment))
                {
                    currentNode.SubFolders[segment] = new FolderNode
                    {
                        Name = segment,
                        FullPath = currentPath
                    };
                }
                currentNode = currentNode.SubFolders[segment];
            }
            
            // Add feature to the deepest folder
            currentNode.Features.Add(feature);
        }
        
        return root;
    }
    
    /// <summary>
    /// Finds the common base path shared by all feature file paths.
    /// This is typically the "Features" folder specified in configuration.
    /// </summary>
    private string FindCommonBasePath(List<EnrichedFeature> features)
    {
        var paths = features
            .Where(f => !string.IsNullOrEmpty(f.Feature.FilePath))
            .Select(f => Path.GetDirectoryName(f.Feature.FilePath?.Replace('\\', '/'))?.Replace('\\', '/') ?? "")
            .Where(p => !string.IsNullOrEmpty(p))
            .ToList();
        
        if (!paths.Any())
            return "";
        
        // Split all paths into segments
        var segmentLists = paths.Select(p => p.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries)).ToList();
        
        if (!segmentLists.Any())
            return "";
        
        // Find the minimum segment count
        var minSegments = segmentLists.Min(s => s.Length);
        
        // Find common prefix segments
        var commonSegments = new List<string>();
        for (int i = 0; i < minSegments; i++)
        {
            var segment = segmentLists[0][i];
            if (segmentLists.All(s => string.Equals(s[i], segment, StringComparison.OrdinalIgnoreCase)))
            {
                commonSegments.Add(segment);
            }
            else
            {
                break;
            }
        }
        
        // Preserve leading slash if original paths had one (Unix-style absolute paths)
        var hasLeadingSlash = paths.Any(p => p.StartsWith("/"));
        var result = commonSegments.Any() ? string.Join("/", commonSegments) : "";
        
        return hasLeadingSlash && !string.IsNullOrEmpty(result) ? "/" + result : result;
    }

    /// <summary>
    /// Generates nested HTML for the folder tree with proper indentation and recursion.
    /// Supports unlimited nesting depth with performance optimizations.
    /// Root level features and folders are displayed directly without a wrapper folder.
    /// Features within a folder are displayed BEFORE subfolders.
    /// </summary>
    private string GenerateNestedFolderTree(FolderNode node, int depth)
    {
        var html = new StringBuilder();
        
        // For root level (depth 0), we directly display features and subfolders
        // without wrapping in a "Features" folder (since header already shows "Features")
        if (depth == 0)
        {
            // Features at root level (directly in the Features folder)
            foreach (var feature in node.Features)
            {
                html.AppendLine(GenerateFeatureItem(feature, 0));
            }
            
            // Subfolders at root level
            foreach (var subFolder in node.SubFolders.Values.OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase))
            {
                html.AppendLine(GenerateFolderNode(subFolder, 0));
            }
        }
        
        return html.ToString();
    }

    /// <summary>
    /// Generates HTML for a single folder node including all its contents.
    /// </summary>
    private string GenerateFolderNode(FolderNode folder, int depth)
    {
        var html = new StringBuilder();
        var folderIdBase = folder.FullPath.Replace("/", "-").Replace(" ", "-").Replace("\\", "-").ToLowerInvariant();
        var folderId = $"folder-{folderIdBase}";
        var levelClass = $"folder-level-{Math.Min(depth, 5)}"; // Cap at level 5 for CSS
        var totalCount = folder.TotalFeatureCount;
        var hasSubFolders = folder.SubFolders.Any();
        var hasFeatures = folder.Features.Any();
        var folderIcon = hasSubFolders ? "fa-folder-tree" : "fa-folder";
        
        // Start collapsed for very deep folders (level 4+) to improve initial load performance
        // Levels 0-3 are expanded by default to show common folder structures
        var isExpanded = depth < 4;
        var expandedClass = isExpanded ? "" : " collapsed";
        var ariaExpanded = isExpanded ? "true" : "false";
        
        html.AppendLine($@"
            <div class=""folder {levelClass}{expandedClass}"" role=""treeitem"" aria-expanded=""{ariaExpanded}"" data-depth=""{depth}"">
                <div class=""folder-header"" 
                     onclick=""toggleFolder('{folderId}')""
                     tabindex=""0""
                     onkeydown=""handleFolderKeydown(event, '{folderId}')""
                     role=""button""
                     aria-label=""Folder: {System.Web.HttpUtility.HtmlEncode(folder.Name)}"">
                    <i class=""fas {folderIcon} folder-icon""></i>
                    <span class=""folder-name"">{System.Web.HttpUtility.HtmlEncode(folder.Name)}</span>
                    <span class=""folder-count"">({totalCount})</span>
                    <i class=""fas fa-chevron-down folder-chevron""></i>
                </div>
                <div class=""folder-content"" id=""{folderId}"">");
        
        // Generate features in this folder first
        foreach (var feature in folder.Features)
        {
            html.AppendLine(GenerateFeatureItem(feature, depth + 1));
        }
        
        // Generate nested subfolders recursively
        foreach (var subFolder in folder.SubFolders.Values.OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase))
        {
            html.AppendLine(GenerateFolderNode(subFolder, depth + 1));
        }
        
        html.AppendLine(@"
                </div>
            </div>");
        
        return html.ToString();
    }

    /// <summary>
    /// Generates HTML for a single feature item in the sidebar.
    /// Shows filename in sidebar, with feature name as tooltip for context.
    /// </summary>
    private string GenerateFeatureItem(EnrichedFeature feature, int depth)
    {
        var statusClass = GetStatusClass(feature.OverallStatus);
        var statusIcon = GetStatusIcon(feature.OverallStatus);
        var featureId = $"feature-{_sidebarFeatureIndex}";
        var isActive = _sidebarFeatureIndex == 0 ? " active" : "";
        var levelClass = $"feature-level-{Math.Min(depth, 5)}";
        
        // Extract filename from path for sidebar display
        var filePath = feature.Feature.FilePath ?? "";
        var fileName = !string.IsNullOrEmpty(filePath) 
            ? Path.GetFileName(filePath) 
            : feature.Feature.Name; // Fallback to feature name if no path
        
        // Remove .feature extension for cleaner display
        if (fileName.EndsWith(".feature", StringComparison.OrdinalIgnoreCase))
        {
            fileName = fileName.Substring(0, fileName.Length - 8);
        }
        
        // Use feature name for tooltip/accessibility
        var featureName = feature.Feature.Name;
        
        _sidebarFeatureIndex++;
        
        return $@"
                    <div class=""feature-item {levelClass}{isActive}"" 
                         data-feature-id=""{featureId}"" 
                         onclick=""selectFeature('{featureId}')""
                         tabindex=""0""
                         role=""treeitem""
                         title=""{System.Web.HttpUtility.HtmlEncode(featureName)}""
                         onkeydown=""handleFeatureKeydown(event, '{featureId}')""
                         aria-label=""Feature: {System.Web.HttpUtility.HtmlEncode(featureName)}"">
                        <span class=""feature-status status-{statusClass}"">{statusIcon}</span>
                        <span class=""feature-name"">{System.Web.HttpUtility.HtmlEncode(fileName)}</span>
                    </div>";
    }

    private int _scenarioCounter = 0;

    private string GenerateFeature(EnrichedFeature feature, int index = 0, bool forLazyLoading = false)
    {
        var statusClass = GetStatusClass(feature.OverallStatus);
        var statusIcon = GetStatusIcon(feature.OverallStatus);
        var featureId = $"feature-{index}";
        var isFirstFeature = index == 0;
        
        var html = new StringBuilder();
        // When generating for lazy loading, dont include feature-hidden (JavaScript will handle visibility)
        var hiddenClass = forLazyLoading ? "" : (isFirstFeature ? "" : " feature-hidden");
        html.AppendLine($@"
    <div class=""feature{hiddenClass}"" data-status=""{statusClass}"" id=""{featureId}"" data-feature-id=""{featureId}"">
        <div class=""feature-header status-{statusClass}"">
            <div class=""feature-title"">
                <span class=""status-icon {statusClass}"">{statusIcon}</span>
                <h2>{System.Web.HttpUtility.HtmlEncode(feature.Feature.Name)}</h2>
            </div>
            <div class=""feature-meta"">");

        if (feature.PassedCount > 0)
            html.AppendLine($@"                <span class=""badge badge-passed""><i class=""fas fa-check""></i> {feature.PassedCount} passed</span>");
        if (feature.FailedCount > 0)
            html.AppendLine($@"                <span class=""badge badge-failed""><i class=""fas fa-times""></i> {feature.FailedCount} failed</span>");
        if (feature.SkippedCount > 0)
            html.AppendLine($@"                <span class=""badge badge-skipped""><i class=""fas fa-minus""></i> {feature.SkippedCount} skipped</span>");

        html.AppendLine(@"            </div>
        </div>
        <div class=""feature-body"">");

        if (!string.IsNullOrWhiteSpace(feature.Feature.Description))
        {
            html.AppendLine($@"            <div class=""feature-description"">{HtmlEncode(feature.Feature.Description)}</div>");
        }

        // Generate feature comments if present and enabled
        if (_currentOptions?.IncludeComments == true && feature.Feature.Comments?.Any() == true)
        {
            html.AppendLine(GenerateComments(feature.Feature.Comments));
        }

        if (feature.Feature.Tags.Any())
        {
            html.AppendLine(@"            <div class=""tags"">");
            foreach (var tag in feature.Feature.Tags)
            {
                html.AppendLine($@"                <span class=""tag""><i class=""fas fa-tag""></i> {HtmlEncode(tag)}</span>");
            }
            html.AppendLine(@"            </div>");
        }

        // Generate Background if present (at feature level)
        if (feature.Feature.Background != null)
        {
            html.AppendLine(GenerateBackground(feature.Feature.Background));
        }

        // Generate Rules if present
        if (feature.Feature.Rules != null && feature.Feature.Rules.Any())
        {
            foreach (var rule in feature.Feature.Rules)
            {
                html.AppendLine(GenerateRule(rule, feature.Scenarios, featureId));
            }
        }
        else
        {
            // Generate scenarios directly (no rules)
            foreach (var scenario in feature.Scenarios)
            {
                html.AppendLine(GenerateScenario(scenario, featureId));
            }
        }

        html.AppendLine(@"        </div>
    </div>");

        return html.ToString();
    }

    private string GenerateBackground(LivingDocGen.Parser.Models.UniversalBackground background)
    {
        var html = new StringBuilder();
        
        html.AppendLine($@"
            <div class=""background"">
                <div class=""background-header"">
                    <div style=""display: flex; align-items: center; gap: 0.75rem; flex: 1;"">
                        <i class=""fas fa-layer-group""></i>
                        <strong>Background: {HtmlEncode(background.Name)}</strong>
                    </div>
                    <i class=""fas fa-chevron-down toggle-icon""></i>
                </div>
                <div class=""background-body expanded"">");

        if (!string.IsNullOrWhiteSpace(background.Description))
        {
            html.AppendLine($@"                <div class=""background-description"">{System.Web.HttpUtility.HtmlEncode(background.Description)}</div>");
        }

        // Generate background steps
        foreach (var step in background.Steps)
        {
            html.AppendLine($@"                    <div class=""step"">
                        <div class=""step-line"">
                            <span class=""step-keyword"">{System.Web.HttpUtility.HtmlEncode(step.Keyword)}</span> <span class=""step-text"">{System.Web.HttpUtility.HtmlEncode(step.Text)}</span>
                        </div>");

            // Add DocString if present
            if (!string.IsNullOrWhiteSpace(step.DocString))
            {
                var contentType = step.DocString.TrimStart().StartsWith("{") || step.DocString.TrimStart().StartsWith("[") ? "JSON" : "Text";
                html.AppendLine($@"                        <div class=""doc-string-container"">
                            <div class=""doc-string-header"" onclick=""toggleDocString(this)"">
                                <i class=""fas fa-chevron-down toggle-icon""></i>
                                <i class=""fas fa-file-alt""></i>
                                <span>{contentType} Content</span>
                            </div>
                            <div class=""doc-string"">{System.Web.HttpUtility.HtmlEncode(step.DocString)}</div>
                        </div>");
            }

            // Add DataTable if present
            if (step.DataTable != null && step.DataTable.Rows.Any())
            {
                var rowCount = step.DataTable.Rows.Count > 1 ? step.DataTable.Rows.Count - 1 : 0;
                var colCount = step.DataTable.Rows.Any() ? step.DataTable.Rows[0].Count : 0;
                
                html.AppendLine(@"                        <div class=""data-table-container"">");
                html.AppendLine(@"                            <div class=""data-table-wrapper"">");
                html.AppendLine(@"                                <table class=""data-table"">");
                
                // First row as headers
                var headers = step.DataTable.Rows[0];
                html.AppendLine(@"                                    <thead onclick=""toggleDataTable(this)""><tr>");
                foreach (var header in headers)
                {
                    html.AppendLine($@"                                        <th>{System.Web.HttpUtility.HtmlEncode(header)}</th>");
                }
                html.AppendLine(@"                                    </tr></thead>");

                // Remaining rows as data
                html.AppendLine(@"                                    <tbody>");
                for (int i = 1; i < step.DataTable.Rows.Count; i++)
                {
                    html.AppendLine(@"                                        <tr>");
                    foreach (var cell in step.DataTable.Rows[i])
                    {
                        html.AppendLine($@"                                            <td>{System.Web.HttpUtility.HtmlEncode(cell)}</td>");
                    }
                    html.AppendLine(@"                                        </tr>");
                }
                html.AppendLine(@"                                    </tbody>
                                </table>
                            </div>
                        </div>");
            }

            html.AppendLine(@"                    </div>");
        }

        html.AppendLine(@"                </div>
            </div>");

        return html.ToString();
    }

    private string GenerateRule(UniversalRule rule, List<EnrichedScenario> allScenarios, string featureId = "")
    {
        var html = new StringBuilder();
        
        html.AppendLine($@"
            <div class=""rule"">
                <div class=""rule-header"">
                    <div class=""rule-title"">
                        <i class=""fas fa-gavel""></i>
                        <h3>{System.Web.HttpUtility.HtmlEncode(rule.Name)}</h3>
                    </div>
                    <i class=""fas fa-chevron-down toggle-icon""></i>
                </div>
                <div class=""rule-body expanded"">" );

        if (!string.IsNullOrWhiteSpace(rule.Description))
        {
            html.AppendLine($@"                <div class=""rule-description"">{System.Web.HttpUtility.HtmlEncode(rule.Description)}</div>");
        }

        // Generate rule-level tags if present (important for tag filtering)
        if (rule.Tags != null && rule.Tags.Any())
        {
            html.AppendLine(@"                <div class=""tags rule-tags"">");
            foreach (var tag in rule.Tags)
            {
                html.AppendLine($@"                    <span class=""tag""><i class=""fas fa-tag""></i> {HtmlEncode(tag)}</span>");
            }
            html.AppendLine(@"                </div>");
        }

        // Generate Background if present (at rule level)
        if (rule.Background != null)
        {
            html.AppendLine(GenerateBackground(rule.Background));
        }

        // Generate scenarios that belong to this rule
        foreach (var ruleScenario in rule.Scenarios)
        {
            // Find matching enriched scenario
            var enrichedScenario = allScenarios.FirstOrDefault(s => 
                s.Scenario.Name.Equals(ruleScenario.Name, StringComparison.OrdinalIgnoreCase));
            
            if (enrichedScenario != null)
            {
                html.AppendLine(GenerateScenario(enrichedScenario, featureId));
            }
        }

        html.AppendLine(@"                </div>
            </div>");

        return html.ToString();
    }

    private string GenerateScenario(EnrichedScenario scenario, string featureId = "")
    {
        var statusClass = GetStatusClass(scenario.Status);
        var statusIcon = GetStatusIcon(scenario.Status);
        var scenarioId = $"scenario-{_scenarioCounter++}";
        
        var html = new StringBuilder();
        var isOutline = scenario.Scenario.Type == LivingDocGen.Parser.Models.ScenarioType.ScenarioOutline;
        
        html.AppendLine($@"
            <div class=""scenario status-{statusClass}"" data-status=""{statusClass}"" id=""{scenarioId}"" data-feature-id=""{featureId}"">
                <div class=""scenario-header"" data-toggle-scenario>
                    <div class=""scenario-title"">
                        <span class=""status-icon {statusClass}"">{statusIcon}</span>
                        <span class=""scenario-type"">{(isOutline ? "Scenario Outline:" : "Scenario:")}</span>
                        <strong>{System.Web.HttpUtility.HtmlEncode(scenario.Scenario.Name)}</strong>");

        if (isOutline)
        {
            html.AppendLine($@"                        <span class=""badge badge-outline""><i class=""fas fa-layer-group""></i> Outline</span>");
        }

        if (scenario.Duration != default(TimeSpan))
        {
            html.AppendLine($@"                        <span class=""step-duration"">({scenario.Duration.TotalSeconds:F2}s)</span>");
        }

        html.AppendLine(@"                    </div>
                </div>
                <div class=""scenario-body"">");

        // Generate scenario-level tags if present (important for tag filtering)
        if (scenario.Scenario.Tags != null && scenario.Scenario.Tags.Any())
        {
            html.AppendLine(@"                    <div class=""tags scenario-tags"">");
            foreach (var tag in scenario.Scenario.Tags)
            {
                html.AppendLine($@"                        <span class=""tag""><i class=""fas fa-tag""></i> {HtmlEncode(tag)}</span>");
            }
            html.AppendLine(@"                    </div>");
        }

        // Generate scenario comments if present and enabled
        if (_currentOptions?.IncludeComments == true && scenario.Scenario.Comments?.Any() == true)
        {
            html.AppendLine(GenerateComments(scenario.Scenario.Comments));
        }

        if (!string.IsNullOrEmpty(scenario.ErrorMessage))
        {
            html.AppendLine($@"
                    <div class=""error-message"">
                        <strong><i class=""fas fa-exclamation-triangle""></i> Error{(scenario.FailedAtLine > 0 ? $" at line {scenario.FailedAtLine}" : "")}</strong>
                        <pre>{System.Web.HttpUtility.HtmlEncode(scenario.ErrorMessage)}</pre>
                    </div>");
        }

        html.AppendLine(@"                    <ul class=""steps"">");

        foreach (var step in scenario.Steps)
        {
            html.AppendLine(GenerateStep(step));
        }

        html.AppendLine(@"                    </ul>");

        // Generate Examples table for Scenario Outlines (Pickles style)
        if (scenario.Scenario.Examples != null && scenario.Scenario.Examples.Any())
        {
            foreach (var example in scenario.Scenario.Examples)
            {
                html.AppendLine(GenerateExamplesTable(example, scenario));
            }
        }

        html.AppendLine(@"                </div>
            </div>");

        return html.ToString();
    }

    private string GenerateStep(EnrichedStep step)
    {
        var statusClass = GetStatusClass(step.Status);
        var html = new StringBuilder();
        
        html.AppendLine($@"                        <li class=""step status-{statusClass}"">");
        html.AppendLine($@"                            <span class=""step-keyword"">{System.Web.HttpUtility.HtmlEncode(step.Step.Keyword)}</span> <span class=""step-text"">{System.Web.HttpUtility.HtmlEncode(step.Step.Text)}");
        
        if (step.Duration != default(TimeSpan) && step.Duration.TotalMilliseconds > 0)
        {
            html.AppendLine($@" <span class=""step-duration"">({step.Duration.TotalMilliseconds:F0}ms)</span>");
        }
        
        html.AppendLine(@"</span>");

        // Add error message if failed
        if (!string.IsNullOrEmpty(step.ErrorMessage))
        {
            html.AppendLine($@"                            <div style=""color: var(--danger-color); margin-top: 0.5rem; margin-left: 65px;""><small>{System.Web.HttpUtility.HtmlEncode(step.ErrorMessage)}</small></div>");
        }

        // Add data table if present
        if (step.Step.DataTable != null && step.Step.DataTable.Rows.Any())
        {
            html.AppendLine(GenerateDataTable(step.Step.DataTable));
        }

        // Add doc string if present
        if (!string.IsNullOrEmpty(step.Step.DocString))
        {
            var contentType = step.Step.DocString.TrimStart().StartsWith("{") || step.Step.DocString.TrimStart().StartsWith("[") ? "JSON" : "Text";
            html.AppendLine($@"                                <div class=""doc-string-container"">
                                    <div class=""doc-string-header"" onclick=""toggleDocString(this)"">
                                        <i class=""fas fa-chevron-down toggle-icon""></i>
                                        <i class=""fas fa-file-alt""></i>
                                        <span>{contentType} Content</span>
                                    </div>
                                    <div class=""doc-string"">{System.Web.HttpUtility.HtmlEncode(step.Step.DocString)}</div>
                                </div>");
        }

        html.AppendLine(@"                        </li>");

        return html.ToString();
    }

    private string GenerateDataTable(UniversalDataTable dataTable)
    {
        var html = new StringBuilder();
        var rowCount = dataTable.Rows.Count > 1 ? dataTable.Rows.Count - 1 : 0;
        var colCount = dataTable.Rows.Any() ? dataTable.Rows[0].Count : 0;
        
        html.AppendLine(@"                                <div class=""data-table-container"">");
        html.AppendLine(@"                                    <div class=""data-table-wrapper"">");
        html.AppendLine(@"                                        <table class=""data-table"">");
        
        if (dataTable.Rows.Any())
        {
            // First row as headers
            html.AppendLine(@"                                            <thead onclick=""toggleDataTable(this)""><tr>");
            foreach (var cell in dataTable.Rows[0])
            {
                html.AppendLine($@"                                                <th>{System.Web.HttpUtility.HtmlEncode(cell)}</th>");
            }
            html.AppendLine(@"                                            </tr></thead>");

            // Remaining rows as data
            if (dataTable.Rows.Count > 1)
            {
                html.AppendLine(@"                                            <tbody>");
                foreach (var row in dataTable.Rows.Skip(1))
                {
                    html.AppendLine(@"                                                <tr>");
                    foreach (var cell in row)
                    {
                        html.AppendLine($@"                                                    <td>{System.Web.HttpUtility.HtmlEncode(cell)}</td>");
                    }
                    html.AppendLine(@"                                                </tr>");
                }
                html.AppendLine(@"                                            </tbody>");
            }
        }
        
        html.AppendLine(@"                                        </table>");
        html.AppendLine(@"                                    </div>");
        html.AppendLine(@"                                </div>");
        return html.ToString();
    }

    private string GenerateExamplesTable(UniversalExample example, EnrichedScenario enrichedScenario)
    {
        var html = new StringBuilder();
        
        html.AppendLine(@"                    <div class=""examples-section"">");
        
        // Examples header with toggle functionality (like Pickles)
        html.AppendLine($@"                        <h4 class=""examples-header"">");
        html.AppendLine($@"                            <i class=""fas fa-table""></i> Examples{(!string.IsNullOrEmpty(example.Name) ? $": {System.Web.HttpUtility.HtmlEncode(example.Name)}" : "")}");
        html.AppendLine($@"                            <i class=""fas fa-chevron-up toggle-icon""></i>");
        html.AppendLine($@"                        </h4>");

        // Examples content wrapper (collapsible)
        html.AppendLine(@"                        <div class=""examples-content"">");

        // Examples tags if present
        if (example.Tags != null && example.Tags.Any())
        {
            html.AppendLine(@"                            <div class=""examples-tags"">");
            foreach (var tag in example.Tags)
            {
                html.AppendLine($@"                                <span class=""tag""><i class=""fas fa-tag""></i> {System.Web.HttpUtility.HtmlEncode(tag)}</span>");
            }
            html.AppendLine(@"                            </div>");
        }

        // Examples table
        if (example.Headers != null && example.Headers.Any())
        {
            var rowCount = example.Rows?.Count ?? 0;
            var colCount = example.Headers.Count;
            
            html.AppendLine(@"                            <div class=""examples-table-container"">");
            html.AppendLine(@"                                <div class=""table-wrapper"">");
            html.AppendLine(@"                                    <table class=""examples-table"">");
            
            // Headers - Add Status column at the beginning
            html.AppendLine(@"                                        <thead onclick=""toggleExamplesTable(this)""><tr>");
            html.AppendLine(@"                                            <th style=""width: 80px; text-align: center;""><i class=""fas fa-vial""></i> Status</th>");
            foreach (var header in example.Headers)
            {
                html.AppendLine($@"                                            <th>{System.Web.HttpUtility.HtmlEncode(header)}</th>");
            }
            html.AppendLine(@"                                        </tr></thead>");

            // Rows - Add status icon for each row
            if (example.Rows != null && example.Rows.Any())
            {
                html.AppendLine(@"                                <tbody>");
                for (int rowIndex = 0; rowIndex < example.Rows.Count; rowIndex++)
                {
                    var row = example.Rows[rowIndex];
                    
                    // Get test result for this example row
                    var rowStatus = enrichedScenario.ExampleResults.ContainsKey(rowIndex) 
                        ? enrichedScenario.ExampleResults[rowIndex].Status 
                        : ExecutionStatus.NotExecuted;
                    
                    var statusClass = rowStatus.ToString().ToLower();
                    var statusIcon = rowStatus switch
                    {
                        ExecutionStatus.Passed => @"<i class=""fas fa-check-circle status-icon passed"" title=""Passed""></i>",
                        ExecutionStatus.Failed => @"<i class=""fas fa-times-circle status-icon failed"" title=""Failed""></i>",
                        ExecutionStatus.Skipped => @"<i class=""fas fa-minus-circle status-icon skipped"" title=""Skipped""></i>",
                        _ => @"<i class=""fas fa-circle status-icon untested"" title=""Not Executed""></i>"
                    };
                    
                    html.AppendLine($@"                                            <tr class=""example-row {statusClass}"">");
                    html.AppendLine($@"                                                <td style=""text-align: center;"">{statusIcon}</td>");
                    foreach (var cell in row)
                    {
                        html.AppendLine($@"                                                <td>{System.Web.HttpUtility.HtmlEncode(cell)}</td>");
                    }
                    html.AppendLine(@"                                            </tr>");
                }
                html.AppendLine(@"                                        </tbody>");
            }

            html.AppendLine(@"                                    </table>");
            html.AppendLine(@"                                </div>");
            html.AppendLine(@"                            </div>");
        }
        
        html.AppendLine(@"                        </div>"); // End examples-content
        html.AppendLine(@"                    </div>");
        return html.ToString();
    }

    private string GenerateComments(List<string> comments)
    {
        if (comments == null || !comments.Any())
            return string.Empty;

        var html = new StringBuilder();
        html.AppendLine(@"            <div class=""comments"">");
        
        foreach (var comment in comments)
        {
            // Remove leading # if present (it will be added by CSS)
            var commentText = comment.TrimStart().TrimStart('#').TrimStart();
            html.AppendLine($@"                <div class=""comment"">{HtmlEncode(commentText)}</div>");
        }
        
        html.AppendLine(@"            </div>");
        return html.ToString();
    }

    private string GenerateFooter(LivingDocumentation documentation)
    {
        return $@"
    <footer>
        <small>Generated by BDD Living Documentation Generator • {documentation.Features.Count} features • {documentation.Statistics.TotalScenarios} scenarios • {documentation.Statistics.TotalSteps} steps</small>
    </footer>";
    }

    private string GenerateFeatureDataJson(LivingDocumentation documentation)
    {
        var json = new StringBuilder();
        json.AppendLine("{");
        
        // Collect all unique tags from all features and scenarios for dropdown population
        var allTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var feature in documentation.Features)
        {
            // Feature-level tags
            if (feature.Feature.Tags != null)
            {
                foreach (var tag in feature.Feature.Tags)
                {
                    if (!string.IsNullOrWhiteSpace(tag))
                        allTags.Add(tag);
                }
            }
            
            // Scenario-level tags and Example-level tags
            foreach (var scenario in feature.Scenarios)
            {
                if (scenario.Scenario.Tags != null)
                {
                    foreach (var tag in scenario.Scenario.Tags)
                    {
                        if (!string.IsNullOrWhiteSpace(tag))
                            allTags.Add(tag);
                    }
                }
                
                // Example-level tags (tags on Examples tables within Scenario Outlines)
                if (scenario.Scenario.Examples != null)
                {
                    foreach (var example in scenario.Scenario.Examples)
                    {
                        if (example.Tags != null)
                        {
                            foreach (var tag in example.Tags)
                            {
                                if (!string.IsNullOrWhiteSpace(tag))
                                    allTags.Add(tag);
                            }
                        }
                    }
                }
            }
            
            // Rule-level tags (if rules exist)
            if (feature.Feature.Rules != null)
            {
                foreach (var rule in feature.Feature.Rules)
                {
                    if (rule.Tags != null)
                    {
                        foreach (var tag in rule.Tags)
                        {
                            if (!string.IsNullOrWhiteSpace(tag))
                                allTags.Add(tag);
                        }
                    }
                    
                    // Scenarios within rules
                    foreach (var ruleScenario in rule.Scenarios)
                    {
                        if (ruleScenario.Tags != null)
                        {
                            foreach (var tag in ruleScenario.Tags)
                            {
                                if (!string.IsNullOrWhiteSpace(tag))
                                    allTags.Add(tag);
                            }
                        }
                    }
                }
            }
        }
        
        // Output all tags as a JSON array (sorted)
        var sortedTags = allTags.OrderBy(t => t, StringComparer.OrdinalIgnoreCase).ToList();
        json.Append("  \"allTags\": [");
        for (int i = 0; i < sortedTags.Count; i++)
        {
            var escapedTag = sortedTags[i].Replace("\\", "\\\\").Replace("\"", "\\\"");
            json.Append($"\"{escapedTag}\"");
            if (i < sortedTags.Count - 1)
                json.Append(", ");
        }
        json.AppendLine("],");
        
        json.AppendLine("  \"features\": [");
        
        for (int i = 0; i < documentation.Features.Count; i++)
        {
            var featureHtml = GenerateFeature(documentation.Features[i], i, forLazyLoading: true);
            // Escape for JSON
            var escapedHtml = featureHtml
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "")
                .Replace("\t", "  ");
            
            json.Append($"    {{\"index\": {i}, \"html\": \"{escapedHtml}\"}}");
            if (i < documentation.Features.Count - 1)
                json.AppendLine(",");
            else
                json.AppendLine();
        }
        
        json.AppendLine("  ]");
        json.AppendLine("}");
        return json.ToString();
    }

    private string GetStatusClass(ExecutionStatus status)
    {
        return status switch
        {
            ExecutionStatus.Passed => "passed",
            ExecutionStatus.Failed => "failed",
            ExecutionStatus.Skipped => "skipped",
            ExecutionStatus.Pending => "skipped",
            ExecutionStatus.Undefined => "failed",
            _ => "untested"
        };
    }

    private string GetStatusIcon(ExecutionStatus status)
    {
        return status switch
        {
            ExecutionStatus.Passed => "<i class=\"fas fa-check-circle\"></i>",
            ExecutionStatus.Failed => "<i class=\"fas fa-times-circle\"></i>",
            ExecutionStatus.Skipped => "<i class=\"fas fa-minus-circle\"></i>",
            ExecutionStatus.Pending => "<i class=\"fas fa-clock\"></i>",
            ExecutionStatus.Undefined => "<i class=\"fas fa-question-circle\"></i>",
            _ => "<i class=\"fas fa-circle\"></i>"
        };
    }
}

/// <summary>
/// Options for HTML generation
/// </summary>
public class HtmlGenerationOptions
{
    public string Theme { get; set; } = "purple";
    public bool IncludeComments { get; set; } = true;
    public bool SyntaxHighlighting { get; set; } = true;
}
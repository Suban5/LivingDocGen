using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LivingDocGen.Generator.Services;

using LivingDocGen.Generator.Models;
using LivingDocGen.Parser.Models;
using LivingDocGen.TestReporter.Models;
using System.Text;

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
    
    // Lazy rendering threshold
    private const int LazyRenderingThreshold = 50; // Enable lazy rendering for 50+ features
    
    // HTML encoding cache to avoid redundant encoding operations
    private readonly Dictionary<string, string> _encodingCache = new Dictionary<string, string>(StringComparer.Ordinal);
    private readonly object _encodingCacheLock = new object();
    private const int MaxEncodingCacheSize = 5000; // Prevent unbounded growth
    
    // CSS theme cache to avoid regenerating CSS for each theme
    private const int MaxCssThemes = 20; // Reasonable limit for cached themes
    private static readonly Dictionary<string, string> _cssCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    private static readonly Queue<string> _cssCacheOrder = new Queue<string>();
    private static readonly object _cssLock = new object();
    
    // HTML generation options (set during GenerateHtml call)
    private HtmlGenerationOptions _currentOptions;
    
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
        html.AppendLine(GenerateJavaScript(documentation, useLazyRendering));
        
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
            .Append(GetCSS(options))
            .Append("\n    </style>\n</head>\n");
        return head.ToString();
    }

    private string GetCSS(HtmlGenerationOptions options)
    {
        var themeName = options.Theme ?? "purple";
        
        // Check cache first
        lock (_cssLock)
        {
            if (_cssCache.TryGetValue(themeName, out var cachedCss))
            {
                return cachedCss;
            }
        }
        
        // Generate CSS if not cached
        var theme = ThemeConfig.GetTheme(themeName);
        var css = GenerateCssContent(theme);
        
        // Cache the result with bounded eviction
        lock (_cssLock)
        {
            if (!_cssCache.ContainsKey(themeName))
            {
                // Evict oldest theme if at capacity (FIFO)
                if (_cssCache.Count >= MaxCssThemes)
                {
                    var oldestTheme = _cssCacheOrder.Dequeue();
                    _cssCache.Remove(oldestTheme);
                }
                
                _cssCache[themeName] = css;
                _cssCacheOrder.Enqueue(themeName);
            }
        }
        
        return css;
    }
    
    private string GenerateCssContent(ThemeConfig theme)
    {
        // Use verbatim string with single braces (they'll be doubled in output)
        return @"
        * {
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }

        :root {
            --primary-color: " + theme.PrimaryColor + @";
            --success-color: " + theme.SuccessColor + @";
            --danger-color: " + theme.DangerColor + @";
            --warning-color: " + theme.WarningColor + @";
            --info-color: " + theme.InfoColor + @";
            --bg-color: " + theme.BgColor + @";
            --card-bg: " + theme.CardBg + @";
            --text-color: " + theme.TextColor + @";
            --text-secondary: " + theme.TextSecondary + @";
            --border-color: " + theme.BorderColor + @";
            --hover-bg: " + theme.HoverBg + @";
            --primary-gradient: " + theme.PrimaryGradient + @";
            --accent-color: " + theme.AccentColor + @";
            --focus-ring: " + theme.FocusRing + @";
            --shadow-color: " + theme.ShadowColor + @";
            --code-bg: " + theme.CodeBg + @";
            
            /* Animation Guardrails - Performance & Accessibility */
            --transition-fast: 100ms ease-out;
            --transition-normal: 150ms ease-out;
            --transition-slow: 300ms ease-out;
            
            /* Layout Variables */
            --header-height: 80px;
            --header-height-shrunk: 50px;
            --controls-height: 60px;
            --stats-height: auto;
        }
        
        /* Respect user motion preferences - Accessibility */
        @media (prefers-reduced-motion: reduce) {
            *, *::before, *::after {
                animation-duration: 0.01ms !important;
                animation-iteration-count: 1 !important;
                transition-duration: 0.01ms !important;
                scroll-behavior: auto !important;
            }
        }

        body {
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;
            background: var(--bg-color);
            color: var(--text-color);
            line-height: 1.7;
            font-weight: 400;
            -webkit-font-smoothing: antialiased;
            -moz-osx-font-smoothing: grayscale;
        }

        header {
            background: var(--primary-gradient);
            color: white;
            padding: 1.25rem 1.5rem;
            box-shadow: 0 2px 8px rgba(0,0,0,0.1);
            position: sticky;
            top: 0;
            z-index: 100;
            transition: padding var(--transition-normal), height var(--transition-normal), box-shadow var(--transition-normal);
            height: var(--header-height);
            display: flex;
            flex-direction: column;
            justify-content: center;
        }
        
        /* Shrunk header on scroll */
        header.shrunk {
            padding: 0.75rem 1.5rem;
            height: var(--header-height-shrunk);
            box-shadow: 0 4px 12px rgba(0,0,0,0.15);
        }

        header h1 {
            font-size: clamp(1.5rem, 2vw, 1.75rem);
            margin-bottom: 0.25rem;
            font-weight: 700;
            letter-spacing: -0.02em;
            transition: font-size var(--transition-normal), margin var(--transition-normal);
        }
        
        header.shrunk h1 {
            font-size: clamp(1.3rem, 1.8vw, 1.5rem);
            margin-bottom: 0;
        }

        header .subtitle {
            opacity: 0.9;
            font-size: 0.95rem;
            font-weight: 300;
            letter-spacing: 0.01em;
            transition: opacity var(--transition-fast), height var(--transition-fast), margin var(--transition-fast);
            max-height: 2rem;
            overflow: hidden;
        }
        
        header.shrunk .subtitle {
            opacity: 0;
            max-height: 0;
            margin: 0;
        }

        .container {
            max-width: 1400px;
            margin: 0 auto;
            padding: 0 2rem;
        }

        /* Skip to content link for accessibility */
        .skip-to-content {
            position: absolute;
            left: -9999px;
            z-index: 999;
            padding: 1rem 1.5rem;
            background: var(--primary-color);
            color: white;
            text-decoration: none;
            border-radius: 0 0 8px 0;
            font-weight: 600;
        }

        .skip-to-content:focus {
            left: 0;
            top: 0;
        }

        /* Controls Section - Compact Single-Row Design */
        #controls {
            background: var(--card-bg);
            padding: 0.75rem 1.5rem;
            margin: 0;
            max-width: 100%;
            border-bottom: 1px solid var(--border-color);
            box-shadow: 0 2px 4px rgba(0,0,0,0.04);
            display: flex;
            gap: 0.75rem;
            align-items: center;
            flex-wrap: wrap;
            position: sticky;
            top: var(--header-height);
            z-index: 99;
            transition: top var(--transition-normal);
        }
        
        header.shrunk ~ #controls {
            top: var(--header-height-shrunk);
        }

        /* Search Highlighting */
        .search-highlight {
            background-color: var(--warning-color);
            color: var(--text-color);
            padding: 0.1rem 0.2rem;
            border-radius: 3px;
            font-weight: 600;
        }

        #search-box {
            padding: 0.625rem 3rem 0.625rem 2.5rem;
            border: 2px solid var(--border-color);
            border-radius: 8px;
            font-size: 0.95rem;
            flex: 1 1 300px;
            min-width: 250px;
            max-width: 500px;
            background-color: var(--card-bg);
            background-image: url('data:image/svg+xml,%3Csvg xmlns=%22http://www.w3.org/2000/svg%22 width=%2218%22 height=%2218%22 viewBox=%220 0 24 24%22 fill=%22none%22 stroke=%22%236b7280%22 stroke-width=%222%22%3E%3Ccircle cx=%2211%22 cy=%2211%22 r=%228%22%3E%3C/circle%3E%3Cpath d=%22m21 21-4.35-4.35%22%3E%3C/path%3E%3C/svg%3E');
            background-repeat: no-repeat;
            background-position: 8px center;
            background-size: 18px;
            color: var(--text-color);
            transition: border-color var(--transition-fast), box-shadow var(--transition-fast);
        }

        #search-box:focus {
            outline: none;
            border-color: var(--focus-ring);
        }

        #search-box:focus-visible {
            outline: 3px solid var(--focus-ring);
            outline-offset: 2px;
        }

        #search-box::placeholder {
            color: var(--text-secondary);
        }

        /* Clear search button */
        .search-clear-btn {
            position: absolute;
            right: 0.5rem;
            top: 50%;
            transform: translateY(-50%);
            background: transparent;
            border: none;
            color: var(--text-secondary);
            cursor: pointer;
            padding: 0.5rem;
            border-radius: 4px;
            display: none;
            align-items: center;
            justify-content: center;
            font-size: 1rem;
            transition: all 0.2s;
            width: 32px;
            height: 32px;
            z-index: 1;
        }

        .search-clear-btn.visible {
            display: flex;
        }

        .search-clear-btn:hover {
            background: var(--hover-bg);
            color: var(--text-color);
        }

        .search-clear-btn:focus-visible {
            outline: 2px solid var(--focus-ring);
            outline-offset: 2px;
        }

        .search-clear-btn:active {
            transform: translateY(-50%) scale(0.9);
        }

        /* Search navigation buttons */
        .search-nav-btn {
            position: absolute;
            top: 50%;
            transform: translateY(-50%);
            background: transparent;
            border: none;
            color: var(--text-secondary);
            cursor: pointer;
            padding: 0.5rem;
            border-radius: 4px;
            display: none;
            align-items: center;
            justify-content: center;
            font-size: 0.9rem;
            transition: all 0.2s;
            width: 32px;
            height: 32px;
            z-index: 1;
        }

        .search-nav-btn.visible {
            display: flex;
        }

        .search-nav-btn:hover:not(:disabled) {
            background: var(--hover-bg);
            color: var(--text-color);
        }

        .search-nav-btn:disabled {
            opacity: 0.3;
            cursor: not-allowed;
        }

        .search-nav-btn:focus-visible {
            outline: 2px solid var(--focus-ring);
            outline-offset: 2px;
        }

        #search-prev-btn {
            right: 6.5rem;
        }

        #search-next-btn {
            right: 3.5rem;
        }

        /* Search result count */
        .search-result-count {
            position: absolute;
            right: 9.5rem;
            top: 50%;
            transform: translateY(-50%);
            font-size: 0.85rem;
            color: var(--text-secondary);
            background: var(--hover-bg);
            padding: 0.25rem 0.75rem;
            border-radius: 12px;
            font-weight: 500;
            pointer-events: none;
            display: none; 
        }

        .search-result-count.visible {
            display: block; 
        }

        .search-wrapper {
            position: relative;
            flex: 1 1 300px;
            min-width: 250px;
            max-width: 500px;
        }

        .filter-group {
            display: flex;
            gap: 0.5rem;
            flex-wrap: wrap;
        }
        
        .theme-group {
            display: flex;
            gap: 0.5rem;
            margin-left: auto;
        }
        
        /* Responsive Breakpoints */
        @media (max-width: 1400px) {
            #controls {
                padding: 0.75rem 1rem;
            }
            
            .theme-group {
                margin-left: 0;
            }
        }
        
        @media (max-width: 1024px) {
            .filter-group .filter-btn span:not(.fa) {
                display: none;
            }
            
            .filter-btn {
                padding: 0.625rem;
                min-width: 40px;
            }
            
            header h1 {
                font-size: 1.4rem;
            }
        }
        
        @media (max-width: 768px) {
            #controls {
                position: relative;
                top: 0;
                flex-direction: column;
                align-items: stretch;
            }
            
            .search-wrapper {
                max-width: 100%;
                flex: 1;
            }
            
            .filter-group, .theme-group {
                width: 100%;
                justify-content: space-between;
            }
            
            #stats {
                grid-template-columns: repeat(auto-fit, minmax(120px, 1fr));
            }
        }

        .theme-selector {
            padding: 0.625rem 0.875rem;
            border: 2px solid var(--border-color);
            background: var(--card-bg);
            color: var(--text-color);
            border-radius: 8px;
            cursor: pointer;
            font-size: 0.9rem;
            font-weight: 500;
            transition: all var(--transition-fast);
            min-width: 150px;
        }

        .theme-selector:hover {
            background: var(--hover-bg);
            border-color: var(--primary-color);
        }

        .theme-selector:focus {
            outline: none;
            border-color: var(--focus-ring);
            box-shadow: 0 0 0 3px var(--shadow-color);
        }

        .theme-selector:focus-visible {
            outline: 3px solid var(--focus-ring);
            outline-offset: 2px;
        }

        .filter-btn {
            padding: 0.625rem 1rem;
            border: 2px solid var(--border-color);
            background: var(--card-bg);
            color: var(--text-color);
            border-radius: 8px;
            cursor: pointer;
            font-size: 0.9rem;
            font-weight: 500;
            transition: all var(--transition-fast);
            display: flex;
            align-items: center;
            gap: 0.4rem;
            white-space: nowrap;
        }

        .filter-btn[data-filter=""all""] i.fa-list {
            color: var(--info-color);
        }

        .filter-btn[data-filter=""passed""] i.fa-check-circle {
            color: var(--success-color);
        }

        .filter-btn[data-filter=""failed""] i.fa-times-circle {
            color: var(--danger-color);
        }

        .filter-btn[data-filter=""skipped""] i.fa-minus-circle {
            color: var(--warning-color);
        }

        .filter-btn[data-filter=""untested""] i.fa-circle {
            color: var(--text-secondary);
        }

        /* Keep icons white when button is active */
        .filter-btn.active i {
            color: white !important;
        }

        .filter-btn:hover {
            background: var(--hover-bg);
        }

        .filter-btn:focus-visible {
            outline: 3px solid var(--focus-ring);
            outline-offset: 2px;
        }

        .filter-btn.active {
            border-color: var(--primary-color);
            background: var(--primary-color);
            color: white;
        }

        /* Clear All button styling */
        .filter-btn.clear-all-btn {
            background: #f44336;
            color: white;
            border-color: #f44336;
        }

        .filter-btn.clear-all-btn:hover {
            background: #d32f2f;
            border-color: #d32f2f;
        }

        .filter-btn.clear-all-btn i {
            color: white;
        }

        /* Statistics Dashboard - Collapsible */
        #stats-container {
            background: var(--card-bg);
            border-bottom: 1px solid var(--border-color);
            position: sticky;
            top: calc(var(--header-height) + var(--controls-height));
            z-index: 98;
            transition: top var(--transition-normal);
        }
        
        header.shrunk ~ #stats-container {
            top: calc(var(--header-height-shrunk) + var(--controls-height));
        }
        
        #stats-toggle {
            width: 100%;
            background: var(--card-bg);
            border: none;
            padding: 0.75rem 1.5rem;
            display: flex;
            align-items: center;
            justify-content: space-between;
            cursor: pointer;
            color: var(--text-color);
            font-weight: 600;
            font-size: 0.95rem;
            transition: background var(--transition-fast);
        }
        
        #stats-toggle:hover {
            background: var(--hover-bg);
        }
        
        #stats-toggle .stats-summary {
            display: flex;
            gap: 1.5rem;
            font-size: 0.9rem;
            font-weight: 500;
        }
        
        #stats-toggle .stats-summary span {
            display: flex;
            align-items: center;
            gap: 0.4rem;
        }
        
        #stats-toggle i.fa-chevron-down {
            transition: transform var(--transition-normal);
        }
        
        #stats-toggle.collapsed i.fa-chevron-down {
            transform: rotate(-90deg);
        }
        
       #stats {
            max-width: 100%;
            margin: 0;
            padding: 1rem 1.5rem;
            transition: max-height var(--transition-normal), opacity var(--transition-fast), padding var(--transition-normal);
            max-height: 500px;
            opacity: 1;
            overflow: hidden;
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
            gap: 0.75rem;
        }
        
        #stats.collapsed {
            max-height: 0;
            opacity: 0;
            padding: 0 1.5rem;
        }

        .stat-card {
            background: var(--card-bg);
            padding: 1rem;\n            border-radius: 10px;
            box-shadow: 0 2px 4px rgba(0,0,0,0.06);
            transition: transform var(--transition-fast), box-shadow var(--transition-fast);
            cursor: pointer;
        }

        .stat-card:hover {
            transform: translateY(-2px);
            box-shadow: 0 4px 12px var(--shadow-color), 0 2px 4px var(--shadow-color);
        }

        .stat-card.clickable:active {
            transform: translateY(-1px);
        }

        .stat-card .label {
            display: flex;
            align-items: center;
            gap: 0.5rem;
            color: var(--text-secondary);
            font-size: 0.875rem;
            font-weight: 500;
            margin-bottom: 0.5rem;
        }

        .stat-card .icon {
            font-size: 1rem;
        }

        .stat-card .value {
            font-size: 1.75rem;
            font-weight: 700;
            letter-spacing: -0.02em;
            color: var(--text-color);
        }

        .stat-passed .icon { color: var(--success-color); }
        .stat-failed .icon { color: var(--danger-color); }
        .stat-skipped .icon { color: var(--warning-color); }
        .stat-info .icon { color: var(--info-color); }

        /* Feature Cards */
        main {
            max-width: 1400px;
            margin: 0 auto;
            padding: 1rem 1rem 2rem 1rem;
        }

        .feature {
            background: var(--card-bg);
            margin-bottom: 1.5rem;
            border-radius: 12px;
            box-shadow: 0 2px 8px var(--shadow-color), 0 1px 3px var(--shadow-color);
            overflow: hidden;
            transition: box-shadow 0.3s ease, transform 0.2s ease;
            animation: fadeIn 0.3s ease-out;
        }
        
        /* Lazy loading styles */
        .lazy-feature {
            min-height: 200px;
            display: flex;
            align-items: center;
            justify-content: center;
        }
        
        .lazy-placeholder {
            display: flex;
            align-items: center;
            gap: 0.75rem;
            color: var(--text-secondary);
            font-size: 1rem;
            opacity: 0.7;
        }
        
        .lazy-placeholder i {
            font-size: 1.5rem;
            color: var(--primary-color);
        }

        @keyframes fadeIn {
            from {
                opacity: 0;
                transform: translateY(10px);
            }
            to {
                opacity: 1;
                transform: translateY(0);
            }
        }

        .feature:hover {
            box-shadow: 0 4px 16px var(--shadow-color), 0 2px 6px var(--shadow-color);
            transform: translateY(-2px);
        }

        .feature-header {
            padding: 1.5rem;
            background: linear-gradient(to right, var(--bg-color), var(--card-bg));
            border-left: 4px solid var(--primary-color);
            cursor: pointer;
            transition: background 0.2s;
            display: flex;
            align-items: center;
            justify-content: space-between;
        }

        .feature-header:hover {
            background: var(--hover-bg);
        }

        .feature-header.status-passed { border-left-color: var(--success-color); }
        .feature-header.status-failed { border-left-color: var(--danger-color); }
        .feature-header.status-skipped { border-left-color: var(--warning-color); }

        .feature-title {
            display: flex;
            align-items: center;
            gap: 1rem;
            flex: 1;
        }

        .feature-title h2 {
            font-size: 1.5rem;
            color: var(--text-color);
            font-weight: 600;
            letter-spacing: -0.01em;
            line-height: 1.4;
        }

        .feature-meta {
            display: flex;
            gap: 1rem;
            align-items: center;
        }

        .badge {
            padding: 0.25rem 0.75rem;
            border-radius: 20px;
            font-size: 0.85rem;
            font-weight: 600;
        }

        .badge-passed { background: #d1fae5; color: #065f46; }
        .badge-failed { background: #fee2e2; color: #991b1b; }
        .badge-skipped { background: #fef3c7; color: #92400e; }
        .badge-untested { background: #e5e7eb; color: #374151; }
        .badge-outline { background: var(--hover-bg); color: var(--primary-color); border: 1px solid var(--primary-color); margin-left: 0.5rem; }

        .feature-body {
            padding: 1.5rem;
            border-top: 1px solid var(--border-color);
            display: block;
        }

        .feature-description {
            color: var(--text-secondary);
            margin-bottom: 1.5rem;
            white-space: pre-wrap;
        }

        .tags {
            display: flex;
            flex-wrap: wrap;
            gap: 0.5rem;
            margin-bottom: 1.5rem;
        }

        .tag {
            background: var(--hover-bg);
            padding: 0.25rem 0.75rem;
            border-radius: 6px;
            font-size: 0.85rem;
            color: var(--text-secondary);
        }

        /* Background */
        .background {
            background: transparent;
            margin-bottom: 1.5rem;
            border-radius: 8px;
            border-left: 3px solid var(--info-color);
            overflow: hidden;
        }

        .background-header {
            padding: 0.75rem 1rem;
            background: var(--card-bg);
            display: flex;
            align-items: center;
            justify-content: space-between;
            gap: 0.75rem;
            font-weight: 600;
            color: var(--info-color);
            cursor: pointer;
            transition: background 0.2s;
        }

        .background-header:hover {
            background: var(--hover-bg);
        }

        .background-header i {
            font-size: 1rem;
        }

        .background-header .toggle-icon {
            transition: transform 0.3s;
            font-size: 0.875rem;
        }

        .background-body {
            padding: 0;
            max-height: 0;
            overflow: hidden;
            transition: max-height 0.3s ease-out, padding 0.3s ease-out;
            background: transparent;
        }

        .background-body.expanded {
            padding: 1rem;
            max-height: 5000px;
            transition: max-height 0.5s ease-in, padding 0.5s ease-in;
        }

        .background-description {
            margin-bottom: 1rem;
            color: var(--text-secondary);
            font-style: italic;
        }

        /* Rules */
        .rule {
            margin-bottom: 2rem;
            border-radius: 8px;
            border: 2px solid var(--primary-color);
            overflow: hidden;
        }

        .rule-header {
            padding: 1rem 1.5rem;
            background: var(--primary-gradient);
            color: white;
            display: flex;
            align-items: center;
            justify-content: space-between;
            gap: 1rem;
            cursor: pointer;
            transition: background 0.2s;
        }

        .rule-header:hover {
            filter: brightness(1.1);
        }

        .rule-header .toggle-icon {
            transition: transform 0.3s;
            font-size: 0.875rem;
        }

        .rule-title {
            display: flex;
            align-items: center;
            gap: 1rem;
            flex: 1;
        }

        .rule-title i {
            font-size: 1.25rem;
        }

        .rule-title h3 {
            margin: 0;
            font-size: 1.25rem;
            font-weight: 600;
        }

        .rule-body {
            padding: 0;
            background: var(--hover-bg);
            max-height: 0;
            overflow: hidden;
            transition: max-height 0.3s ease-out, padding 0.3s ease-out;
        }

        .rule-body.expanded {
            padding: 1.5rem;
            max-height: 10000px;
            transition: max-height 0.5s ease-in, padding 0.5s ease-in;
        }

        .rule-description {
            margin-bottom: 1.5rem;
            color: var(--text-secondary);
            font-style: italic;
            padding: 0.75rem;
            background: var(--card-bg);
            border-radius: 6px;
            border-left: 3px solid var(--accent-color);
        }

        /* Comments */
        .comments {
            margin: 1rem 0;
            padding: 0.75rem 1rem;
            background: var(--code-bg);
            border-left: 3px solid var(--text-secondary);
            border-radius: 4px;
        }

        .comment {
            color: var(--text-secondary);
            font-family: 'Courier New', Consolas, monospace;
            font-size: 0.9rem;
            margin: 0.25rem 0;
            line-height: 1.5;
        }

        .comment::before {
            content: '# ';
            font-weight: bold;
        }

        /* Scenarios */
        .scenario {
            background: var(--bg-color);
            margin-bottom: 1rem;
            border-radius: 8px;
            border-left: 3px solid var(--border-color);
            overflow: hidden;
            box-shadow: 0 1px 4px var(--shadow-color);
            transition: box-shadow 0.2s ease, transform 0.2s ease;
            /* Performance: Use CSS containment for isolated rendering */
            contain: layout style;
            /* Performance: GPU acceleration hint */
            will-change: transform;
        }

        .scenario:hover {
            box-shadow: 0 2px 8px var(--shadow-color);
            /* Performance: Use transform for GPU-accelerated animation */
            transform: translateX(2px);
        }

        .scenario.status-passed { border-left-color: var(--success-color); }
        .scenario.status-failed { border-left-color: var(--danger-color); }
        .scenario.status-skipped { border-left-color: var(--warning-color); }

        .scenario-header {
            padding: 1rem;
            cursor: pointer;
            display: flex;
            align-items: center;
            justify-content: space-between;
        }

        .scenario-header:hover {
            background: rgba(0,0,0,0.02);
        }

        .scenario-title {
            display: flex;
            align-items: center;
            gap: 0.75rem;
            flex: 1;
            font-size: 1.15rem;
        }

        .scenario-type {
            color: var(--primary-color);
            font-weight: 600;
            font-size: 0.95rem;
        }

        .status-icon {
            font-size: 1.25rem;
        }

        .status-icon.passed { color: var(--success-color); }
        .status-icon.failed { color: var(--danger-color); }
        .status-icon.skipped { color: var(--warning-color); }
        .status-icon.untested { color: var(--text-secondary); }

        .scenario-body {
            padding: 0 1rem 1rem 1rem;
            /* Performance: Use max-height instead of display for smoother animations */
            max-height: 0;
            overflow: hidden;
            transition: max-height 0.3s ease, opacity 0.2s ease;
            opacity: 0;
        }

        .scenario-body.expanded {
            max-height: 10000px; /* Large enough for any scenario */
            opacity: 1;
        }

        .error-message {
            background: var(--hover-bg);
            border-left: 4px solid var(--danger-color);
            padding: 1rem;
            margin-bottom: 1rem;
            border-radius: 4px;
        }

        .error-message strong {
            color: var(--danger-color);
            display: block;
            margin-bottom: 0.5rem;
        }

        /* Steps */
        .steps {
            list-style: none;
        }

        .step {
            padding: 0.875rem 1rem;
            margin-bottom: 0.625rem;
            background: var(--card-bg);
            border-radius: 6px;
            box-shadow: 0 1px 3px var(--shadow-color);
            transition: box-shadow 0.2s ease;
        }

        .step:hover {
            box-shadow: 0 2px 6px var(--shadow-color);
        }

        .step-keyword {
            font-weight: 700;
            color: var(--primary-color);
            min-width: 60px;
            font-size: 0.95rem;
        }

        .step-text {
            flex: 1;
            line-height: 1.6;
            font-weight: 400;
        }

        .step.status-failed {
            background: var(--card-bg);
            border-left: 4px solid var(--danger-color);
        }

        .step.status-passed {
            background: var(--card-bg);
            border-left: 4px solid var(--success-color);
        }

        .step.status-skipped {
            background: var(--card-bg);
            border-left: 4px solid var(--warning-color);
            opacity: 0.8;
        }

        .step-duration {
            color: var(--text-secondary);
            font-size: 0.85rem;
        }

        /* Data Tables - Collapsible */
        .data-table-container {
            margin: 1rem 0;
            border: 1px solid var(--border-color);
            border-radius: 6px;
            overflow: hidden;
        }

        .data-table-header {
            background: var(--card-bg);
            padding: 0.75rem 1rem;
            cursor: pointer;
            display: flex;
            align-items: center;
            gap: 0.5rem;
            font-weight: 600;
            color: var(--text-secondary);
            transition: background 0.2s;
            user-select: none;
        }

        .data-table-header:hover {
            background: var(--hover-bg);
        }

        .data-table-header .toggle-icon {
            transition: transform 0.3s;
            font-size: 0.75rem;
        }

        .data-table-header.collapsed .toggle-icon {
            transform: rotate(-90deg);
        }

        .data-table-wrapper {
            overflow-x: auto;
            overflow-y: visible;
            -webkit-overflow-scrolling: touch;
            max-height: 500px;
            transition: max-height 0.3s ease-out;
        }

        .data-table-wrapper.collapsed {
            max-height: 0;
            overflow: hidden;
        }

        .data-table-wrapper::-webkit-scrollbar {
            height: 8px;
        }

        .data-table-wrapper::-webkit-scrollbar-track {
            background: var(--hover-bg);
            border-radius: 4px;
        }

        .data-table-wrapper::-webkit-scrollbar-thumb {
            background: var(--primary-color);
            border-radius: 4px;
            opacity: 0.7;
        }

        .data-table-wrapper::-webkit-scrollbar-thumb:hover {
            background: var(--primary-color);
            opacity: 1;
        }

        .step-line {
            display: flex;
            align-items: center;
            gap: 0.5rem;
            margin-bottom: 0.5rem;
        }

        .data-table-wrapper {
            margin-top: 0.75rem;
            width: 100%;
        }

        .data-table {
            width: 100%;
            min-width: 400px;
            margin: 0;
            border-collapse: collapse;
            font-size: 0.9rem;
            background: var(--card-bg);
        }

        .data-table th {
            background: var(--hover-bg);
            padding: 0.875rem 1rem;
            text-align: left;
            font-weight: 600;
            border-bottom: 2px solid var(--border-color);
            white-space: nowrap;
            color: var(--text-color);
            letter-spacing: 0.01em;
            font-size: 0.875rem;
            position: sticky;
            top: 0;
            z-index: 10;
        }

        .data-table thead {
            cursor: pointer;
            user-select: none;
        }

        .data-table thead:hover th {
            background: var(--primary-color);
            color: white;
        }

        .data-table tbody.collapsed {
            display: none;
        }

        .data-table td {
            padding: 0.875rem 1rem;
            border-bottom: 1px solid var(--border-color);
            white-space: nowrap;
            color: var(--text-color);
            line-height: 1.6;
        }

        .data-table tbody tr:hover {
            background: var(--hover-bg);
        }

        .data-table tbody tr:nth-child(even) {
            background: rgba(0, 0, 0, 0.02);
        }

        .data-table tbody tr:nth-child(even):hover {
            background: var(--hover-bg);
        }

        .data-table tbody tr:last-child td {
            border-bottom: none;
        }

        /* Doc Strings - Collapsible */
        .doc-string-container {
            margin: 1rem 0;
            border: 1px solid var(--border-color);
            border-radius: 6px;
            overflow: hidden;
        }

        .doc-string-header {
            background: var(--card-bg);
            padding: 0.75rem 1rem;
            cursor: pointer;
            display: flex;
            align-items: center;
            gap: 0.5rem;
            font-weight: 600;
            color: var(--text-secondary);
            transition: background 0.2s;
            user-select: none;
        }

        .doc-string-header:hover {
            background: var(--hover-bg);
        }

        .doc-string-header .toggle-icon {
            transition: transform 0.3s;
            font-size: 0.75rem;
        }

        .doc-string-header.collapsed .toggle-icon {
            transform: rotate(-90deg);
        }

        .doc-string {
            background: var(--hover-bg);
            padding: 1rem;
            font-family: 'Courier New', monospace;
            font-size: 0.9rem;
            white-space: pre-wrap;
            color: var(--text-color);
            max-height: 500px;
            overflow: auto;
            transition: max-height 0.3s ease-out;
        }

        .doc-string.collapsed {
            max-height: 0;
            padding: 0;
            overflow: hidden;
        }

        /* Examples Section - Theme Aware */
        .examples-section {
            margin-top: 1.5rem;
            background: var(--hover-bg);
            border: 2px solid var(--primary-color);
            border-radius: 8px;
            padding: 1.25rem;
        }

        .examples-header {
            color: var(--text-color);
            font-size: 1.1rem;
            margin-bottom: 1rem;
            display: flex;
            align-items: center;
            gap: 0.5rem;
            cursor: pointer;
            user-select: none;
            transition: all 0.2s ease;
        }

        .examples-header:hover {
            opacity: 0.8;
        }

        .examples-header i.fa-table {
            color: var(--primary-color);
        }

        .examples-header .toggle-icon {
            margin-left: auto;
            font-size: 0.9rem;
            transition: transform 0.3s ease;
        }

        .examples-content {
            max-height: 1000px;
            overflow: hidden;
            transition: max-height 0.3s ease, opacity 0.3s ease;
            opacity: 1;
        }

        .examples-content.collapsed {
            max-height: 0;
            opacity: 0;
        }

        .examples-tags {
            display: flex;
            flex-wrap: wrap;
            gap: 0.5rem;
            margin-bottom: 1rem;
        }

        .examples-table-container {
            margin: 1rem 0;
        }

        .examples-table-header {
            background: var(--card-bg);
            padding: 0.75rem 1rem;
            cursor: pointer;
            display: flex;
            align-items: center;
            gap: 0.5rem;
            font-weight: 600;
            color: var(--primary-color);
            transition: background 0.2s;
            user-select: none;
            border-radius: 6px 6px 0 0;
        }

        .examples-table-header:hover {
            background: var(--hover-bg);
        }

        .examples-table-header .toggle-icon {
            transition: transform 0.3s;
            font-size: 0.75rem;
        }

        .examples-table-header.collapsed .toggle-icon {
            transform: rotate(-90deg);
        }

        .table-wrapper {
            overflow-x: auto;
            overflow-y: visible;
            -webkit-overflow-scrolling: touch;
            border-radius: 0 0 6px 6px;
            max-height: 500px;
            transition: max-height 0.3s ease-out;
        }

        .table-wrapper.collapsed {
            max-height: 0;
            overflow: hidden;
        }

        .examples-table {
            width: 100%;
            min-width: 600px;
            border-collapse: collapse;
            background: var(--card-bg);
            box-shadow: 0 2px 8px var(--shadow-color), 0 1px 3px var(--shadow-color);
        }

        .examples-table th {
            background: var(--primary-color);
            color: white;
            padding: 0.875rem 1rem;
            text-align: left;
            font-weight: 600;
            font-size: 0.875rem;
            border-bottom: 2px solid var(--primary-color);
            white-space: nowrap;
            position: sticky;
            top: 0;
            z-index: 10;
            letter-spacing: 0.01em;
        }

        .examples-table thead {
            cursor: pointer;
            user-select: none;
        }

        .examples-table thead:hover th {
            background: var(--primary-color);
            opacity: 0.9;
        }

        .examples-table tbody.collapsed {
            display: none;
        }

        .examples-table td {
            padding: 0.875rem 1rem;
            border-bottom: 1px solid var(--border-color);
            line-height: 1.6;
        }
            font-size: 0.9rem;
            white-space: nowrap;
            color: var(--text-color);
        }

        .examples-table tbody tr:hover {
            background: var(--hover-bg);
        }

        .examples-table tbody tr:nth-child(even):not(.example-row) {
            background: rgba(0, 0, 0, 0.02);
        }

        .examples-table tbody tr:nth-child(even):not(.example-row):hover {
            background: var(--hover-bg);
        }

        .examples-table tbody tr:last-child td {
            border-bottom: none;
        }

        /* Example Row Status Styling */
        .example-row.passed {
            background-color: var(--hover-bg);
            border-left: 3px solid var(--success-color);
        }

        .example-row.failed {
            background-color: var(--hover-bg);
            border-left: 3px solid var(--danger-color);
        }

        .example-row.skipped {
            background-color: var(--hover-bg);
            border-left: 3px solid var(--warning-color);
        }

        .example-row.notexecuted {
            background-color: transparent;
        }

        /* Table Wrappers for Horizontal Scrolling */
        .table-wrapper, .examples-container {
            overflow-x: auto;
            margin: 1rem 0;
            border-radius: 8px;
            box-shadow: 0 1px 3px var(--shadow-color);
        }

        .table-wrapper::-webkit-scrollbar, .examples-container::-webkit-scrollbar {
            height: 8px;
        }

        .table-wrapper::-webkit-scrollbar-track, .examples-container::-webkit-scrollbar-track {
            background: var(--hover-bg);
            border-radius: 4px;
        }

        .table-wrapper::-webkit-scrollbar-thumb, .examples-container::-webkit-scrollbar-thumb {
            background: var(--primary-color);
            border-radius: 4px;
            opacity: 0.7;
        }

        .table-wrapper::-webkit-scrollbar-thumb:hover, .examples-container::-webkit-scrollbar-thumb:hover {
            background: var(--primary-color);
            opacity: 1;
        }

        /* Footer */
        footer {
            background: transparent;
            padding: 0.75rem;
            margin-top: 1.5rem;
            text-align: center;
            color: var(--text-secondary);
            font-size: 0.8rem;
            opacity: 0.6;
        }

        footer:hover {
            opacity: 0.9;
        }

        /* Scroll to Top Button */
        #scroll-to-top {
            position: fixed;
            bottom: 2rem;
            right: 2rem;
            width: 3rem;
            height: 3rem;
            background: var(--primary-color);
            color: white;
            border: none;
            border-radius: 50%;
            cursor: pointer;
            display: none;
            align-items: center;
            justify-content: center;
            font-size: 1.25rem;
            box-shadow: 0 4px 12px var(--shadow-color);
            transition: all 0.3s ease;
            z-index: 999;
        }

        #scroll-to-top:hover {
            transform: translateY(-4px);
            box-shadow: 0 6px 20px var(--shadow-color);
        }

        #scroll-to-top.visible {
            display: flex;
            animation: fadeInUp 0.3s ease-out;
        }

        @keyframes fadeInUp {
            from {
                opacity: 0;
                transform: translateY(20px);
            }
            to {
                opacity: 1;
                transform: translateY(0);
            }
        }

        /* Responsive */
        @media (max-width: 1200px) {
            #controls {
                grid-template-columns: 1fr;
                gap: 1rem;
            }
            
            .filter-group, .theme-group {
                justify-content: flex-start;
            }
        }
        
        @media (max-width: 768px) {
            header h1 {
                font-size: 1.75rem;
            }

            .container {
                padding: 0 1rem;
            }

            #controls {
                grid-template-columns: 1fr;
                padding: 1rem;
            }

            .theme-selector {
                width: 100%;
            }

            #stats {
                grid-template-columns: 1fr 1fr;
                gap: 1rem;
            }

            .feature-meta {
                flex-direction: column;
                align-items: flex-start;
                gap: 0.5rem;
            }

            .feature-header {
                flex-direction: column;
                align-items: flex-start;
                gap: 1rem;
            }

            .scenario-header {
                flex-direction: column;
                align-items: flex-start;
                gap: 0.5rem;
            }

            .scenario-title {
                flex-direction: column;
                align-items: flex-start;
            }

            .step {
                flex-direction: column;
                gap: 0.5rem;
            }

            .step-keyword {
                min-width: auto;
            }

            .filter-group {
                flex-direction: column;
                width: 100%;
            }

            .filter-btn {
                width: 100%;
                justify-content: center;
            }

            /* Ensure tables scroll on mobile */
            .table-wrapper,
            .data-table-wrapper,
            .examples-container {
                margin: 0 -1rem;
                padding: 0 1rem;
            }

            .examples-table,
            .data-table {
                font-size: 0.85rem;
            }

            .examples-table th,
            .examples-table td,
            .data-table th,
            .data-table td {
                padding: 0.5rem 0.75rem;
            }
        }

        @media (max-width: 480px) {
            header h1 {
                font-size: 1.5rem;
            }
            
            header {
                padding: 1.5rem 1rem;
            }

            #stats {
                grid-template-columns: 1fr;
            }

            .stat-card {
                padding: 1rem;
            }

            .stat-card .value {
                font-size: 1.5rem;
            }

            .examples-table,
            .data-table {
                font-size: 0.75rem;
                min-width: 500px;
            }

            .badge {
                font-size: 0.75rem;
                padding: 0.2rem 0.5rem;
            }
            
            .filter-group {
                flex-wrap: wrap;
            }
            
            .filter-btn {
                flex: 1 1 calc(50% - 0.25rem);
                min-width: 120px;
            }
            
            #floating-sidebar-toggle {
                width: 40px;
                height: 40px;
                font-size: 1rem;
            }
        }

        /* ============================================
           MASTER-DETAIL LAYOUT (VS Code Style)
           ============================================ */

        .layout-container {
            display: flex;
            height: calc(100vh - 400px);
            min-height:500px;
            max-width: 1400px;
            margin: 0 auto 2rem;
            position: relative;
            border-radius: 8px;
            overflow: hidden;
            box-shadow: 0 1px 3px var(--shadow-color);
        }

        .sidebar {
            width: 260px;
            min-width: 100px;
            max-width: 500px;
            background: var(--card-bg);
            border-right: 1px solid var(--border-color);
            display: flex;
            flex-direction: column;
            overflow: hidden;
            transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
            flex-shrink: 0;
        }

        .sidebar.collapsed {
            width: 0;
            min-width: 0;
            max-width: 0;
            border-right: none;
            padding: 0;
            opacity: 0;
            visibility: hidden;
        }

        .sidebar-header {
            padding: 0.875rem 1rem;
            border-bottom: 1px solid var(--border-color);
            display: flex;
            align-items: center;
            justify-content: space-between;
            background: var(--hover-bg);
        }

        .sidebar-header h3 {
            font-size: 0.9rem;
            font-weight: 600;
            color: var(--text-color);
            display: flex;
            align-items: center;
            gap: 0.5rem;
        }

        .sidebar-toggle {
            background: none;
            border: none;
            color: var(--text-secondary);
            cursor: pointer;
            padding: 0.5rem;
            border-radius: 4px;
            transition: all 0.2s;
        }

        .sidebar-toggle:hover {
            background: var(--hover-bg);
            color: var(--primary-color);
        }

        .sidebar-nav {
            flex: 1;
            overflow-y: auto;
            padding: 0.5rem;
        }

        .sidebar-nav::-webkit-scrollbar {
            width: 8px;
        }

        .sidebar-nav::-webkit-scrollbar-track {
            background: transparent;
        }

        .sidebar-nav::-webkit-scrollbar-thumb {
            background: var(--primary-color);
            border-radius: 4px;
            opacity: 0.6;
        }

        .sidebar-nav::-webkit-scrollbar-thumb:hover {
            background: var(--primary-color);
            opacity: 1;
        }

        .folder {
            margin-bottom: 0.25rem;
        }

        .folder-header {
            display: flex;
            align-items: center;
            gap: 0.5rem;
            padding: 0.4rem 0.75rem;
            cursor: pointer;
            border-radius: 6px;
            font-size: 0.875rem;
            color: var(--text-color);
            transition: all 0.2s cubic-bezier(0.4, 0, 0.2, 1);
        }

        .folder-header:hover {
            background: var(--hover-bg);
            transform: translateX(2px);
        }

        .folder-header:focus {
            outline: none;
            background: var(--hover-bg);
        }

        .folder-header:focus-visible {
            outline: 2px solid var(--focus-ring);
            outline-offset: -2px;
        }

        .folder-icon {
            color: var(--accent-color);
            font-size: 0.875rem;
        }

        .folder-name {
            flex: 1;
            font-weight: 600;
            font-size: 0.875rem;
        }

        .folder-count {
            font-size: 0.75rem;
            color: var(--text-secondary);
            background: var(--hover-bg);
            padding: 0.125rem 0.5rem;
            border-radius: 10px;
        }

        .folder-chevron {
            font-size: 0.75rem;
            color: var(--text-secondary);
            transition: transform 0.2s;
        }

        .folder.collapsed .folder-chevron {
            transform: rotate(-90deg);
        }

        .folder-content {
            padding-left: 0.5rem;
            max-height: 2000px;
            overflow: hidden;
            transition: max-height 0.3s ease;
        }

        .folder.collapsed .folder-content {
            max-height: 0;
        }

        .feature-item {
            display: flex;
            align-items: center;
            gap: 0.5rem;
            padding: 0.4rem 0.75rem 0.4rem 1.5rem;
            margin: 0.125rem 0;
            cursor: pointer;
            border-radius: 6px;
            font-size: 0.825rem;
            transition: all 0.2s cubic-bezier(0.4, 0, 0.2, 1);
            color: var(--text-color);
            border-left: 3px solid transparent;
        }

        .feature-item:hover {
            background: var(--hover-bg);
            transform: translateX(2px);
            border-left-color: var(--accent-color);
        }

        .feature-item:focus {
            outline: none;
            background: var(--hover-bg);
        }

        .feature-item:focus-visible {
            outline: 2px solid var(--focus-ring);
            outline-offset: -2px;
        }

        .feature-item.active {
            background: var(--primary-color);
            color: white;
            font-weight: 600;
            border-left-color: white;
            box-shadow: 0 2px 4px var(--shadow-color);
        }

        .feature-item.active .feature-status {
            color: white !important;
        }

        .feature-status {
            font-size: 0.9rem;
        }

        .feature-status.status-passed {
            color: var(--success-color);
        }

        .feature-status.status-failed {
            color: var(--danger-color);
        }

        .feature-status.status-skipped {
            color: var(--warning-color);
        }

        .feature-status.status-untested {
            color: var(--text-secondary);
        }

        .feature-name {
            flex: 1;
            overflow: hidden;
            text-overflow: ellipsis;
            white-space: nowrap;
        }

        .feature-badge {
            display: flex;
            gap: 0.25rem;
        }

        .badge-mini {
            padding: 0.125rem 0.375rem;
            border-radius: 10px;
            font-size: 0.7rem;
            font-weight: 600;
        }

        .resizer {
            width: 4px;
            background: var(--border-color);
            cursor: col-resize;
            position: relative;
            transition: all 0.3s ease;
            flex-shrink: 0;
            user-select: none;
        }

        .resizer:hover,
        .resizer.resizing {
            background: var(--primary-color);
        }

        .resizer::before {
            content: '';
            position: absolute;
            left: -4px;
            right: -4px;
            top: 0;
            bottom: 0;
        }
        
        .sidebar.collapsed + .resizer {
            width: 0;
            opacity: 0;
            visibility: hidden;
        }

        .main-content {
            flex: 1;
            overflow-y: auto;
            padding: 1.5rem;
            background: var(--bg-color);
            transition: all 0.3s ease;
            width: 100%;
        }

        .main-content::-webkit-scrollbar {
            width: 10px;
        }

        .main-content::-webkit-scrollbar-track {
            background: var(--bg-color);
        }

        .main-content::-webkit-scrollbar-thumb {
            background: var(--border-color);
            border-radius: 5px;
        }

        .main-content::-webkit-scrollbar-thumb:hover {
            background: var(--text-secondary);
        }

        .feature-hidden {
            display: none !important;
        }

        /* Floating Sidebar Toggle Button */
        #floating-sidebar-toggle {
            position: fixed;
            left: 1rem;
            top: 50%;
            transform: translateY(-50%);
            background: var(--primary-color);
            color: white;
            border: none;
            width: 44px;
            height: 44px;
            border-radius: 8px;
            cursor: pointer;
            display: none;
            align-items: center;
            justify-content: center;
            box-shadow: 0 2px 8px var(--shadow-color);
            z-index: 1000;
            transition: all 0.3s ease;
            font-size: 1.1rem;
            opacity: 0;
            pointer-events: none;
        }
        
        #floating-sidebar-toggle.visible {
            display: flex;
            opacity: 1;
            pointer-events: auto;
        }

        #floating-sidebar-toggle:hover {
            filter: brightness(1.1);
            transform: translateY(-50%) scale(1.05);
            box-shadow: 0 4px 12px var(--shadow-color);
        }

        #floating-sidebar-toggle:active {
            transform: translateY(-50%) scale(0.95);
        }
        
        @media (min-width: 1920px) {
            #floating-sidebar-toggle {
                left: calc((100vw - 1800px) / 2 + 1rem);
            }
        }

        /* Screen reader only content */
        .sr-only {
            position: absolute;
            width: 1px;
            height: 1px;
            padding: 0;
            margin: -1px;
            overflow: hidden;
            clip: rect(0, 0, 0, 0);
            white-space: nowrap;
            border-width: 0;
        }

        /* Large Desktop Screens */
        @media (min-width: 1920px) {
            .layout-container {
                max-width: 1800px;
                height: calc(100vh - 350px);
            }
            
            .sidebar {
                max-width: 450px;
            }
        }

        /* Desktop Screens */
        @media (min-width: 1440px) and (max-width: 1919px) {
            .layout-container {
                max-width: 1600px;
            }
        }

        /* Tablet Landscape */
        @media (max-width: 1024px) {
            .layout-container {
                height: calc(100vh - 450px);
                min-height: 500px;
            }
            
            .sidebar {
                width: 240px;
                max-width: 300px;
            }
            
            #controls {
                padding: 1rem;
            }
        }

        /* Tablet Portrait */
        @media (max-width: 768px) {
            .layout-container {
                flex-direction: column;
                height: auto;
                min-height: 400px;
            }
            
            .sidebar {
                width: 100%;
                max-width: none;
                height: 300px;
                min-height: 0;
                border-right: none;
                border-bottom: 1px solid var(--border-color);
                transition: height 0.3s ease;
            }
            
            .sidebar.collapsed {
                height: 0;
                min-height: 0;
                overflow: hidden;
                border: none;
            }
            
            .resizer {
                display: none;
            }
            
            .main-content {
                height: auto;
                max-height: none;
                min-height: 400px;
            }
            
            #floating-sidebar-toggle {
                top: 10px;
                left: 10px;
                transform: none;
            }
        }

        /* Print Styles */
        @media print {
            .feature-body {
                display: block !important;
            }

            .scenario-body {
                display: block !important;
            }

            #controls {
                display: none;
            }
            
            .sidebar,
            .resizer {
                display: none;
            }
            
            .layout-container {
                display: block;
            }
        }";
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
            <input type=""text"" 
                   id=""search-box"" 
                   placeholder=""Search features, scenarios, steps...""
                   aria-label=""Search documentation""
                   aria-describedby=""search-result-count"">
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
            <button type=""button"" 
                    id=""search-clear-btn"" 
                    class=""search-clear-btn"" 
                    aria-label=""Clear search""
                    title=""Clear search (Esc)"">
                <i class=""fas fa-times"" aria-hidden=""true""></i>
            </button>
            <span id=""search-result-count"" class=""search-result-count"" aria-live=""polite""></span>
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
        
        html.AppendLine(@"
    <aside id=""sidebar"" class=""sidebar"" role=""navigation"" aria-label=""Feature navigation"">
        <div class=""sidebar-header"">
            <h3><i class=""fas fa-folder-tree"" aria-hidden=""true""></i> Features</h3>
            <button id=""sidebar-toggle"" 
                    class=""sidebar-toggle"" 
                    title=""Toggle Sidebar (⌘B)""
                    aria-label=""Toggle sidebar navigation""
                    aria-expanded=""true"">
                <i class=""fas fa-angles-left""></i>
            </button>
        </div>
        
        <nav class=""sidebar-nav"" id=""sidebar-nav"" role=""tree"" aria-label=""Features tree"">");
        
        // Build folder tree structure
        var folderTree = BuildFolderTree(documentation.Features);
        html.AppendLine(GenerateFolderTree(folderTree, documentation.Features));
        
        html.AppendLine(@"
        </nav>
    </aside>");
        
        return html.ToString();
    }

    private Dictionary<string, List<EnrichedFeature>> BuildFolderTree(List<EnrichedFeature> features)
    {
        var tree = new Dictionary<string, List<EnrichedFeature>>();
        
        foreach (var feature in features)
        {
            // Extract folder from file path (if available)
            var filePath = feature.Feature.FilePath ?? "";
            var folder = string.IsNullOrEmpty(filePath) ? "Root" : 
                         Path.GetDirectoryName(filePath)?.Split(Path.DirectorySeparatorChar).LastOrDefault() ?? "Root";
            
            if (!tree.ContainsKey(folder))
            {
                tree[folder] = new List<EnrichedFeature>();
            }
            tree[folder].Add(feature);
        }
        
        return tree;
    }

    private string GenerateFolderTree(Dictionary<string, List<EnrichedFeature>> folderTree, List<EnrichedFeature> allFeatures)
    {
        var html = new StringBuilder();
        var featureIndex = 0;
        
        foreach (var folder in folderTree.Keys.OrderBy(k => k))
        {
            var features = folderTree[folder];
            var folderIconClass = folder == "Root" ? "fa-home" : "fa-folder";
            var folderId = $"folder-{folder.Replace(" ", "-").ToLower()}";
            
            // Folder header
            html.AppendLine($@"
            <div class=""folder"" role=""treeitem"" aria-expanded=""true"">
                <div class=""folder-header"" 
                     onclick=""toggleFolder('{folderId}')""
                     tabindex=""0""
                     onkeydown=""handleFolderKeydown(event, '{folderId}')""
                     role=""button""
                     aria-label=""Folder: {System.Web.HttpUtility.HtmlEncode(folder)}"">
                    <i class=""fas {folderIconClass} folder-icon""></i>
                    <span class=""folder-name"">{System.Web.HttpUtility.HtmlEncode(folder)}</span>
                    <span class=""folder-count"">({features.Count})</span>
                    <i class=""fas fa-chevron-down folder-chevron""></i>
                </div>
                <div class=""folder-content"" id=""{folderId}"">");
            
            // Features in folder
            foreach (var feature in features)
            {
                var statusClass = GetStatusClass(feature.OverallStatus);
                var statusIcon = GetStatusIcon(feature.OverallStatus);
                var featureId = $"feature-{featureIndex}";
                var isActive = featureIndex == 0 ? " active" : "";
                
                html.AppendLine($@"
                    <div class=""feature-item{isActive}"" 
                         data-feature-id=""{featureId}"" 
                         onclick=""selectFeature('{featureId}')""
                         tabindex=""0""
                         role=""treeitem""
                         onkeydown=""handleFeatureKeydown(event, '{featureId}')""
                         aria-label=""Feature: {System.Web.HttpUtility.HtmlEncode(feature.Feature.Name)}"">
                        <span class=""feature-status status-{statusClass}"">{statusIcon}</span>
                        <span class=""feature-name"">{System.Web.HttpUtility.HtmlEncode(feature.Feature.Name)}</span>
                    </div>");
                
                featureIndex++;
            }
            
            html.AppendLine(@"
                </div>
            </div>");
        }
        
        return html.ToString();
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

    private string GenerateJavaScript(LivingDocumentation documentation, bool useLazyRendering = false)
    {
        var featureCount = documentation.Features.Count;
        var hasLargeReport = featureCount > 100;
        
        return @"
    <script>
        // Performance optimizations for large reports (" + featureCount + @" features)
        const PERF_LARGE_REPORT = " + hasLargeReport.ToString().ToLower() + @";
        const FEATURE_COUNT = " + featureCount + @";
        const USE_LAZY_RENDERING = " + useLazyRendering.ToString().ToLower() + @";
        
        // Feature Navigation State
        let currentFeatureId = 'feature-0';
        
        // Performance: Use event delegation for toggle operations in large reports
        if (PERF_LARGE_REPORT) {
            console.log('⚡ Performance mode enabled for ' + FEATURE_COUNT + ' features');
        }
        
        if (USE_LAZY_RENDERING) {
            console.log('⚡ Lazy rendering enabled - content loads on scroll');
        }
        
        // ============================================
        // PHASE 1: ADAPTIVE HEADER & STATISTICS
        // ============================================
        
        // Adaptive Header: Shrink on scroll
        let lastScrollTop = 0;
        const header = document.querySelector('header');
        const scrollThreshold = 100;
        
        function handleHeaderScroll() {
            const scrollTop = window.pageYOffset || document.documentElement.scrollTop;
            
            if (scrollTop > scrollThreshold) {
                header.classList.add('shrunk');
            } else {
                header.classList.remove('shrunk');
            }
            
            lastScrollTop = scrollTop;
        }
        
        // Throttle scroll events for performance
        let ticking = false;
        window.addEventListener('scroll', function() {
            if (!ticking) {
                window.requestAnimationFrame(function() {
                    handleHeaderScroll();
                    ticking = false;
                });
                ticking = true;
            }
        });
        
        // Statistics Toggle Function
        function toggleStats() {
            const statsPanel = document.getElementById('stats');
            const statsToggle = document.getElementById('stats-toggle');
            const isCollapsed = statsPanel.classList.contains('collapsed');
            
            if (isCollapsed) {
                statsPanel.classList.remove('collapsed');
                statsToggle.classList.remove('collapsed');
                statsToggle.setAttribute('aria-expanded', 'true');
                localStorage.setItem('statsExpanded', 'true');
            } else {
                statsPanel.classList.add('collapsed');
                statsToggle.classList.add('collapsed');
                statsToggle.setAttribute('aria-expanded', 'false');
                localStorage.setItem('statsExpanded', 'false');
            }
        }
        
        // Restore stats state from localStorage
        document.addEventListener('DOMContentLoaded', function() {
            const statsExpanded = localStorage.getItem('statsExpanded');
            // Default to collapsed after first visit (unless explicitly set to true)
            if (statsExpanded === 'false') {
                const statsPanel = document.getElementById('stats');
                const statsToggle = document.getElementById('stats-toggle');
                if (statsPanel && statsToggle) {
                    statsPanel.classList.add('collapsed');
                    statsToggle.classList.add('collapsed');
                    statsToggle.setAttribute('aria-expanded', 'false');
                }
            }
        });
        
        // ============================================
        // SMART PROGRESSIVE DISCLOSURE
        // ============================================
        
        // Auto-expand failures and search hits
        function autoExpandFeature(featureId, reason) {
            const feature = document.getElementById(featureId);
            if (!feature) return;
            
            // Ensure feature is visible
            if (feature.classList.contains('feature-hidden')) {
                feature.classList.remove('feature-hidden');
            }
            
            // Auto-expand failed scenarios
            if (reason === 'failure') {
                const failedScenarios = feature.querySelectorAll('.scenario-header.failed');
                failedScenarios.forEach(header => {
                    const body = header.nextElementSibling;
                    if (body && !body.classList.contains('expanded')) {
                        body.classList.add('expanded');
                    }
                });
            }
            
            // Scroll feature into view
            feature.scrollIntoView({ behavior: 'smooth', block: 'start' });
        }
        
        // Auto-compact view for large datasets
        if (FEATURE_COUNT > 100) {
            console.log('📊 Large dataset detected - using compact view');
            // Compact mode is handled by lazy rendering
        }
        
        // ============================================
        // PERFORMANCE: UNIFIED EVENT DELEGATION
        // ============================================
        // Single event listener handles ALL toggle operations
        // Critical for 200+ features with 500+ scenarios
        
        document.addEventListener('click', function(e) {
            // Scenario toggle - most common operation
            const scenarioHeader = e.target.closest('.scenario-header');
            if (scenarioHeader && scenarioHeader.hasAttribute('data-toggle-scenario')) {
                e.preventDefault();
                const scenarioBody = scenarioHeader.nextElementSibling;
                if (scenarioBody && scenarioBody.classList.contains('scenario-body')) {
                    requestAnimationFrame(() => {
                        scenarioBody.classList.toggle('expanded');
                    });
                }
                return;
            }
            
            // Background toggle
            const backgroundHeader = e.target.closest('.background-header');
            if (backgroundHeader) {
                e.preventDefault();
                const body = backgroundHeader.nextElementSibling;
                const toggleIcon = backgroundHeader.querySelector('.toggle-icon');
                if (body) {
                    requestAnimationFrame(() => {
                        body.classList.toggle('expanded');
                        if (toggleIcon) {
                            if (body.classList.contains('expanded')) {
                                toggleIcon.classList.remove('fa-chevron-down');
                                toggleIcon.classList.add('fa-chevron-up');
                            } else {
                                toggleIcon.classList.remove('fa-chevron-up');
                                toggleIcon.classList.add('fa-chevron-down');
                            }
                        }
                    });
                }
                return;
            }
            
            // Rule toggle
            const ruleHeader = e.target.closest('.rule-header');
            if (ruleHeader) {
                e.preventDefault();
                const body = ruleHeader.nextElementSibling;
                const toggleIcon = ruleHeader.querySelector('.toggle-icon');
                if (body) {
                    requestAnimationFrame(() => {
                        body.classList.toggle('expanded');
                        if (toggleIcon) {
                            if (body.classList.contains('expanded')) {
                                toggleIcon.classList.remove('fa-chevron-down');
                                toggleIcon.classList.add('fa-chevron-up');
                            } else {
                                toggleIcon.classList.remove('fa-chevron-up');
                                toggleIcon.classList.add('fa-chevron-down');
                            }
                        }
                    });
                }
                return;
            }
            
            // Data table toggle (thead click)
            const tableHeader = e.target.closest('thead.data-table-header, thead.examples-table-header');
            if (tableHeader) {
                e.preventDefault();
                const tbody = tableHeader.parentElement.querySelector('tbody');
                if (tbody) {
                    requestAnimationFrame(() => {
                        tbody.classList.toggle('collapsed');
                        tableHeader.classList.toggle('collapsed');
                    });
                }
                return;
            }
            
            // DocString toggle
            const docStringHeader = e.target.closest('.doc-string-header');
            if (docStringHeader) {
                e.preventDefault();
                const content = docStringHeader.nextElementSibling;
                if (content) {
                    requestAnimationFrame(() => {
                        docStringHeader.classList.toggle('collapsed');
                        content.classList.toggle('collapsed');
                    });
                }
                return;
            }
            
            // Examples section toggle
            const examplesHeader = e.target.closest('.examples-header');
            if (examplesHeader) {
                e.preventDefault();
                const content = examplesHeader.nextElementSibling;
                const toggleIcon = examplesHeader.querySelector('.toggle-icon');
                if (content) {
                    requestAnimationFrame(() => {
                        content.classList.toggle('collapsed');
                        if (toggleIcon) {
                            if (content.classList.contains('collapsed')) {
                                toggleIcon.classList.remove('fa-chevron-up');
                                toggleIcon.classList.add('fa-chevron-down');
                            } else {
                                toggleIcon.classList.remove('fa-chevron-down');
                                toggleIcon.classList.add('fa-chevron-up');
                            }
                        }
                    });
                }
                return;
            }
        });
        
        // Legacy function for backward compatibility
        function toggleScenario(header) {
            const body = header.nextElementSibling;
            requestAnimationFrame(() => {
                body.classList.toggle('expanded');
            });
        }

        // Toggle data table
        function toggleDataTable(header) {
            // header is now thead, find tbody and toggle it
            const tbody = header.parentElement.querySelector('tbody');
            if (tbody) {
                tbody.classList.toggle('collapsed');
                header.classList.toggle('collapsed');
            }
        }

        // Toggle doc string
        function toggleDocString(header) {
            const content = header.nextElementSibling;
            header.classList.toggle('collapsed');
            content.classList.toggle('collapsed');
        }

        // Toggle examples table
        function toggleExamplesTable(header) {
            // header is now thead, find tbody and toggle it
            const tbody = header.parentElement.querySelector('tbody');
            if (tbody) {
                tbody.classList.toggle('collapsed');
                header.classList.toggle('collapsed');
            }
        }

        // Toggle background section
        function toggleBackground(header) {
            const body = header.nextElementSibling;
            const toggleIcon = header.querySelector('.toggle-icon');
            
            body.classList.toggle('expanded');
            
            // Update icon
            if (body.classList.contains('expanded')) {
                toggleIcon.classList.remove('fa-chevron-down');
                toggleIcon.classList.add('fa-chevron-up');
            } else {
                toggleIcon.classList.remove('fa-chevron-up');
                toggleIcon.classList.add('fa-chevron-down');
            }
        }

        // Toggle rule section
        function toggleRule(header) {
            const body = header.nextElementSibling;
            const toggleIcon = header.querySelector('.toggle-icon');
            
            body.classList.toggle('expanded');
            
            // Update icon
            if (body.classList.contains('expanded')) {
                toggleIcon.classList.remove('fa-chevron-down');
                toggleIcon.classList.add('fa-chevron-up');
            } else {
                toggleIcon.classList.remove('fa-chevron-up');
                toggleIcon.classList.add('fa-chevron-down');
            }
        }

        // Toggle examples section
        function toggleExamples(header) {
            const content = header.nextElementSibling;
            const toggleIcon = header.querySelector('.toggle-icon');
            
            content.classList.toggle('collapsed');
            
            // Update icon
            if (content.classList.contains('collapsed')) {
                toggleIcon.classList.remove('fa-chevron-up');
                toggleIcon.classList.add('fa-chevron-down');
            } else {
                toggleIcon.classList.remove('fa-chevron-down');
                toggleIcon.classList.add('fa-chevron-up');
            }
        }

        // Debounced search functionality with highlighting and result count
        let searchTimeout;
        let searchResults = [];
        let currentSearchIndex = 0;
        const searchBox = document.getElementById('search-box');
        const searchResultCount = document.getElementById('search-result-count');
        const searchClearBtn = document.getElementById('search-clear-btn');
        const searchPrevBtn = document.getElementById('search-prev-btn');
        const searchNextBtn = document.getElementById('search-next-btn');
        
        function removeHighlights() {
            document.querySelectorAll('.search-highlight').forEach(highlight => {
                const parent = highlight.parentNode;
                parent.replaceChild(document.createTextNode(highlight.textContent), highlight);
                parent.normalize();
            });
        }
        
        function highlightText(element, searchTerm) {
            if (!searchTerm || element.children.length > 0) return;
            
            const text = element.textContent;
            const index = text.toLowerCase().indexOf(searchTerm);
            
            if (index >= 0) {
                const beforeMatch = text.substring(0, index);
                const match = text.substring(index, index + searchTerm.length);
                const afterMatch = text.substring(index + searchTerm.length);
                
                element.innerHTML = '';
                element.appendChild(document.createTextNode(beforeMatch));
                
                const mark = document.createElement('mark');
                mark.className = 'search-highlight';
                mark.textContent = match;
                element.appendChild(mark);
                
                element.appendChild(document.createTextNode(afterMatch));
            }
        }
        
        function performSearch() {
            const searchTerm = searchBox.value.toLowerCase().trim();
            
            // Remove previous highlights
            removeHighlights();
            
            // Update global filter state
            activeFilters.searchTerm = searchTerm;
            
            // Render all lazy features if searching
            if (searchTerm && USE_LAZY_RENDERING) {
                document.querySelectorAll('.feature[data-lazy]').forEach(feature => {
                    renderFeatureContent(feature);
                });
            }
            
            // Apply all filters (includes search)
            applyAllFilters();
            
            // Highlight search matches
            if (searchTerm) {
                const features = document.querySelectorAll('.feature[data-feature-id]');
                features.forEach(feature => {
                    if (feature.style.display !== 'none') {
                        // Highlight in feature title
                        const title = feature.querySelector('.feature-title h2');
                        if (title) highlightText(title, searchTerm);
                        
                        // Highlight in scenario names
                        feature.querySelectorAll('.scenario-title strong').forEach(el => {
                            highlightText(el, searchTerm);
                        });
                        
                        // SMART PROGRESSIVE DISCLOSURE: Auto-expand matched scenarios
                        const scenarios = feature.querySelectorAll('.scenario');
                        scenarios.forEach(scenario => {
                            if (scenario.style.display !== 'none') {
                                const scenarioTitle = scenario.querySelector('.scenario-title strong');
                                if (scenarioTitle && scenarioTitle.textContent.toLowerCase().includes(searchTerm)) {
                                    const scenarioHeader = scenario.querySelector('.scenario-header');
                                    if (scenarioHeader) {
                                        const scenarioBody = scenarioHeader.nextElementSibling;
                                        if (scenarioBody && scenarioBody.classList.contains('scenario-body')) {
                                            scenarioBody.classList.add('expanded');
                                        }
                                    }
                                }
                            }
                        });
                    }
                });
            }
            
            // Update search UI (counts, navigation buttons)
            updateSearchNavigationUI();
        }
        
        function updateSearchNavigationUI() {
            const searchTerm = searchBox.value.trim();
            
            // Collect visible scenarios that match search
            searchResults = [];
            if (searchTerm) {
const visibleScenarios = document.querySelectorAll(
    '.scenario[style*=""display: block""], .scenario:not([style*=""display: none""])'
);
                visibleScenarios.forEach(scenario => {
                    const scenarioText = scenario.textContent.toLowerCase();
                    if (scenarioText.includes(searchTerm.toLowerCase())) {
                        searchResults.push(scenario);
                    }
                });
            }
            currentSearchIndex = 0;
            
            if (searchTerm && searchResults.length > 0) {
                searchResultCount.textContent = `${currentSearchIndex + 1} of ${searchResults.length}`;
                searchResultCount.classList.add('visible');
                searchClearBtn.classList.add('visible');
                searchPrevBtn.classList.add('visible');
                searchNextBtn.classList.add('visible');
                
                // Update button states
                searchPrevBtn.disabled = currentSearchIndex === 0;
                searchNextBtn.disabled = currentSearchIndex === searchResults.length - 1;
                
                // Scroll to top of current result
                const mainContent = document.getElementById('main-content');
                if (mainContent) {
                    mainContent.scrollTop = 0;
                }
            } else {
                searchResultCount.classList.remove('visible');
                searchClearBtn.classList.remove('visible');
                searchPrevBtn.classList.remove('visible');
                searchNextBtn.classList.remove('visible');
            }
        }
        
        function updateSearchUI() {
            const searchTerm = searchBox.value.trim();
            
            if (searchTerm && searchResults.length > 0) {
                searchResultCount.textContent = `${currentSearchIndex + 1} of ${searchResults.length}`;
                searchResultCount.classList.add('visible');
                searchClearBtn.classList.add('visible');
                searchPrevBtn.classList.add('visible');
                searchNextBtn.classList.add('visible');
                
                // Update button states
                searchPrevBtn.disabled = currentSearchIndex === 0;
                searchNextBtn.disabled = currentSearchIndex === searchResults.length - 1;
                
                // Scroll to top of current result
                const mainContent = document.getElementById('main-content');
                if (mainContent) {
                    mainContent.scrollTop = 0;
                }
            } else {
                searchResultCount.classList.remove('visible');
                searchClearBtn.classList.remove('visible');
                searchPrevBtn.classList.remove('visible');
                searchNextBtn.classList.remove('visible');
            }
        }
        
        function navigateSearchResults(direction) {
            if (searchResults.length === 0) return;
            
            // Hide current result
            searchResults[currentSearchIndex].classList.add('feature-hidden');
            
            // Update index
            if (direction === 'next') {
                currentSearchIndex = Math.min(currentSearchIndex + 1, searchResults.length - 1);
            } else {
                currentSearchIndex = Math.max(currentSearchIndex - 1, 0);
            }
            
            // Show new result
            const currentFeature = searchResults[currentSearchIndex];
            currentFeature.classList.remove('feature-hidden');
            
            // Update UI
            updateSearchUI();
            
            // Scroll to the feature in main content
            requestAnimationFrame(() => {
                const mainContent = document.getElementById('main-content');
                if (mainContent && currentFeature) {
                    // Find first highlighted element
                    const firstHighlight = currentFeature.querySelector('.highlight');
                    if (firstHighlight) {
                        // If highlight is in a step, we need to expand the scenario first
                        const stepElement = firstHighlight.closest('.step');
                        if (stepElement) {
                            const scenarioBody = stepElement.closest('.scenario-body');
                            if (scenarioBody && !scenarioBody.classList.contains('expanded')) {
                                // Expand the scenario to show the steps
                                const scenarioHeader = scenarioBody.previousElementSibling;
                                if (scenarioHeader && scenarioHeader.classList.contains('scenario-header')) {
                                    toggleScenario(scenarioHeader);
                                }
                            }
                        }
                        
                        // Wait for expansion animation, then scroll to highlight
                        setTimeout(() => {
                            // Calculate position relative to main content
                            const highlightRect = firstHighlight.getBoundingClientRect();
                            const mainContentRect = mainContent.getBoundingClientRect();
                            const relativeTop = highlightRect.top - mainContentRect.top + mainContent.scrollTop;
                            
                            // Scroll to center the highlight in the viewport
                            const centerOffset = mainContent.clientHeight / 2 - highlightRect.height / 2;
                            mainContent.scrollTo({
                                top: relativeTop - centerOffset,
                                behavior: 'smooth'
                            });
                        }, 200);
                    } else {
                        // No highlight found, just scroll to top of feature
                        mainContent.scrollTop = 0;
                    }
                }
                
                // Also update sidebar selection
                const featureId = currentFeature.getAttribute('data-feature-id');
                if (featureId) {
                    selectFeature(featureId);
                }
            });
        }
        
        searchBox.addEventListener('input', function(e) {
            clearTimeout(searchTimeout);
            // Performance: Increased debounce for large reports
            const debounceDelay = PERF_LARGE_REPORT ? 400 : 300;
            searchTimeout = setTimeout(() => {
                // Performance: Use requestAnimationFrame for smooth UI updates
                requestAnimationFrame(() => {
                    performSearch();
                });
            }, debounceDelay);
        });
        
        // Clear search functionality
        searchClearBtn.addEventListener('click', function() {
            searchBox.value = '';
            performSearch();
            searchBox.focus();
        });
        
        // Navigation buttons
        searchPrevBtn.addEventListener('click', function() {
            navigateSearchResults('prev');
        });
        
        searchNextBtn.addEventListener('click', function() {
            navigateSearchResults('next');
        });
        
        // Keyboard shortcuts for search
        searchBox.addEventListener('keydown', function(e) {
            if (e.key === 'Escape' && searchBox.value) {
                e.preventDefault();
                searchBox.value = '';
                performSearch();
            } else if (e.key === 'Enter' && searchBox.value) {
                e.preventDefault();
                if (e.shiftKey) {
                    navigateSearchResults('prev');
                } else {
                    navigateSearchResults('next');
                }
            }
        });

        // ============================================
        // GLOBAL FILTER STATE
        // ============================================
        let activeFilters = {
            status: 'all',      // 'all', 'passed', 'failed', 'skipped', 'untested'
            tags: [],           // Array of tag strings
            searchTerm: ''      // Current search text
        };

        // ============================================
        // HELPER FUNCTIONS FOR FILTERING
        // ============================================
        
        function updateSidebarForFilter(visibleFeatureIds) {
            const sidebarItems = document.querySelectorAll('.feature-item');
            let matchCount = 0;
            
            sidebarItems.forEach(item => {
                const featureId = item.getAttribute('data-feature-id');
                if (visibleFeatureIds.has(featureId)) {
                    item.style.display = 'flex';
                    item.style.opacity = '1';
                    matchCount++;
                } else {
                    // Hide non-matching features completely
                    item.style.display = 'none';
                    item.style.opacity = '1';
                }
            });
            
            // Update folder visibility and expansion
            document.querySelectorAll('.folder').forEach(folder => {
                const visibleItems = Array.from(folder.querySelectorAll('.feature-item'))
                    .filter(item => item.style.display !== 'none');
                
                if (visibleItems.length > 0) {
                    folder.style.display = 'block';
                    folder.style.opacity = '1';
                    folder.classList.remove('collapsed'); // Auto-expand folders with matches
                } else {
                    // Hide empty folders completely
                    folder.style.display = 'none';
                    folder.style.opacity = '1';
                }
            });
        }
        
        function showEmptyStateIfNeeded(visibleCount, filter) {
    const mainContent = document.getElementById('main-content');
    let emptyState = document.getElementById('empty-state-message');

    if (visibleCount === 0) {
        if (!emptyState) {
            emptyState = document.createElement('div');
            emptyState.id = 'empty-state-message';
            emptyState.className = 'empty-state';
            emptyState.style.cssText = `
                text-align: center;
                padding: 60px 20px;
                color: var(--text-secondary);
            `;
            mainContent.insertBefore(emptyState, mainContent.firstChild);
        }

        const filterText = filter === 'all' ? 'scenarios' :
                           filter === 'passed' ? 'passed scenarios' :
                           filter === 'failed' ? 'failed scenarios' :
                           filter === 'skipped' ? 'skipped scenarios' :
                           filter === 'untested' ? 'untested scenarios' : 'scenarios';

        emptyState.innerHTML =
            '<div style=""font-size: 48px; margin-bottom: 16px;"">&#128269;</div>' +
            '<h2 style=""margin: 0 0 8px 0;"">No ' + filterText + ' found</h2>' +
            '<p style=""margin: 0 0 24px 0;"">Try clearing all filters to view all scenarios.</p>' +
            '<button onclick=""clearAllFilters();"" style=""' +
                'padding: 10px 24px;' +
                'background: var(--primary-color);' +
                'color: white;' +
                'border: none;' +
                'border-radius: 6px;' +
                'cursor: pointer;' +
                'font-size: 14px;' +
            '"">Clear All Filters</button>';

        emptyState.style.display = 'block';
    } else if (emptyState) {
        emptyState.style.display = 'none';
    }
}

        
        function announceToScreenReader(message) {
            let announcer = document.getElementById('screen-reader-announcer');
            
            if (!announcer) {
                announcer = document.createElement('div');
                announcer.id = 'screen-reader-announcer';
                announcer.setAttribute('role', 'status');
                announcer.setAttribute('aria-live', 'polite');
                announcer.setAttribute('aria-atomic', 'true');
                announcer.style.cssText = `
                    position: absolute;
                    left: -10000px;
                    width: 1px;
                    height: 1px;
                    overflow: hidden;
                `;
                document.body.appendChild(announcer);
            }
            
            announcer.textContent = message;
            
            setTimeout(() => {
                announcer.textContent = '';
            }, 100);
        }

        // ============================================
        // FILTER BY STATUS (INTEGRATED WITH ALL FILTERS)
        // ============================================
        function filterByStatus(filter) {
            // Update global filter state
            activeFilters.status = filter;
            
            // Update active state on filter buttons
            document.querySelectorAll('.filter-btn[data-filter]').forEach(b => {
                if (b.dataset.filter === filter) {
                    b.classList.add('active');
                    b.setAttribute('aria-pressed', 'true');
                } else {
                    b.classList.remove('active');
                    b.setAttribute('aria-pressed', 'false');
                }
            });

            // Apply all filters (this respects search, tags, and status together)
            applyAllFilters();
            
            // Scroll to top
            window.scrollTo({ top: 0, behavior: 'smooth' });
        }

        // ============================================
        // CLEAR ALL FILTERS
        // ============================================
        function clearAllFilters() {
            // Reset filter state
            activeFilters.status = 'all';
            activeFilters.tags = [];
            activeFilters.searchTerm = '';
            
            // Clear search box
            if (searchBox) searchBox.value = '';
            
            // Reset filter buttons
            document.querySelectorAll('.filter-btn[data-filter]').forEach(btn => {
                if (btn.dataset.filter === 'all') {
                    btn.classList.add('active');
                    btn.setAttribute('aria-pressed', 'true');
                } else {
                    btn.classList.remove('active');
                    btn.setAttribute('aria-pressed', 'false');
                }
            });
            
            // Reset tag dropdown
            const tagFilter = document.getElementById('tag-filter');
            if (tagFilter) tagFilter.value = 'all';
            
            // Remove highlights
            removeHighlights();
            
            // Apply filters (will show all)
            applyAllFilters();
            
            // Update search UI
            updateSearchNavigationUI();
            
            // Announce to screen reader
            announceToScreenReader('All filters cleared. Showing all scenarios.');
        }
        
        // ============================================
        // MASTER FILTER FUNCTION (COMBINED FILTERS)
        // ============================================
        function applyAllFilters() {
            const features = document.querySelectorAll('.feature[data-feature-id]');
            let totalVisibleScenarios = 0;
            const visibleFeatureIds = new Set();
            
            features.forEach(feature => {
                const scenarios = feature.querySelectorAll('.scenario');
                let visibleInFeature = 0;
                
                scenarios.forEach(scenario => {
                    // Check status filter
                    const status = scenario.dataset.status;
                    const matchesStatus = activeFilters.status === 'all' || status === activeFilters.status;
                    
                    // Check tag filters (AND logic: scenario must have ALL selected tags)
                    // Get scenario-level tags (from scenario's own tags div)
                    const scenarioTagsDiv = scenario.querySelector('.tags');
                    const scenarioTags = scenarioTagsDiv ? Array.from(scenarioTagsDiv.querySelectorAll('.tag'))
                        .map(t => {
                            const clone = t.cloneNode(true);
                            const icon = clone.querySelector('i');
                            if (icon) icon.remove();
                            return clone.textContent.trim();
                        }) : [];
                    
                    // Get feature-level tags (from feature body's tags div, not from scenarios)
                    const featureBody = feature.querySelector('.feature-body');
                    const featureTagsDiv = featureBody ? featureBody.querySelector(':scope > .tags') : null;
                    const featureHeaderTags = featureTagsDiv ? Array.from(featureTagsDiv.querySelectorAll('.tag'))
                        .map(t => {
                            const clone = t.cloneNode(true);
                            const icon = clone.querySelector('i');
                            if (icon) icon.remove();
                            return clone.textContent.trim();
                        }) : [];
                    
                    const allTags = [...new Set([...scenarioTags, ...featureHeaderTags])];
                    
                    const matchesTags = activeFilters.tags.length === 0 || 
                        activeFilters.tags.every(filterTag => 
                            allTags.some(scenarioTag => 
                                scenarioTag.toLowerCase().includes(filterTag.toLowerCase())
                            )
                        );
                    
                    // Check search term (search in feature title and scenario names only)
                    let matchesSearch = !activeFilters.searchTerm;
                    if (activeFilters.searchTerm && !matchesSearch) {
                        const searchLower = activeFilters.searchTerm.toLowerCase();
                        // Check feature title
                        const featureTitle = feature.querySelector('.feature-title h2');
                        if (featureTitle && featureTitle.textContent.toLowerCase().includes(searchLower)) {
                            matchesSearch = true;
                        }
                        // Check scenario name
                        if (!matchesSearch) {
                            const scenarioTitle = scenario.querySelector('.scenario-title strong');
                            if (scenarioTitle && scenarioTitle.textContent.toLowerCase().includes(searchLower)) {
                                matchesSearch = true;
                            }
                        }
                    }
                    
                    // Apply AND logic: scenario must match ALL active filters
                    const matchesAllFilters = matchesStatus && matchesTags && matchesSearch;
                    
                    if (matchesAllFilters) {
                        scenario.style.display = 'block';
                        visibleInFeature++;
                        totalVisibleScenarios++;
                    } else {
                        scenario.style.display = 'none';
                    }
                });
                
                // Hide feature if no scenarios match
                if (visibleInFeature > 0) {
                    feature.style.display = 'block';
                    const featureId = feature.getAttribute('data-feature-id');
                    if (featureId) visibleFeatureIds.add(featureId);
                } else {
                    feature.style.display = 'none';
                }
            });
            
            // Update UI elements
            updateSidebarForFilter(visibleFeatureIds);
            showEmptyStateIfNeeded(totalVisibleScenarios, activeFilters.status);
            
            // Announce results
            const announcement = `Filter applied. Showing ${totalVisibleScenarios} scenario${totalVisibleScenarios !== 1 ? 's' : ''} in ${visibleFeatureIds.size} feature${visibleFeatureIds.size !== 1 ? 's' : ''}`;
            announceToScreenReader(announcement);
        }

        // Filter by tag
        function filterByTag(tag) {
            // Update global filter state
            if (tag === 'all') {
                activeFilters.tags = [];
            } else {
                activeFilters.tags = [tag];
            }
            
            // Render all lazy features if filtering by tag
            if (tag !== 'all' && USE_LAZY_RENDERING) {
                document.querySelectorAll('.feature[data-lazy]').forEach(feature => {
                    renderFeatureContent(feature);
                });
            }
            
            // Apply all filters (this respects search, status, and tags together)
            applyAllFilters();
            
            // Scroll to top
            window.scrollTo({ top: 0, behavior: 'smooth' });
        }

        // Screen reader announcement helper
        function announceToScreenReader(message) {
            const announcement = document.createElement('div');
            announcement.setAttribute('role', 'status');
            announcement.setAttribute('aria-live', 'polite');
            announcement.className = 'sr-only';
            announcement.textContent = message;
            document.body.appendChild(announcement);
            setTimeout(() => announcement.remove(), 1000);
        }
        
        // Keyboard navigation for feature items
        function handleFeatureKeydown(event, featureId) {
            if (event.key === 'Enter' || event.key === ' ') {
                event.preventDefault();
                selectFeature(featureId);
            }
        }
        
        // Keyboard navigation for folder headers
        function handleFolderKeydown(event, folderId) {
            if (event.key === 'Enter' || event.key === ' ') {
                event.preventDefault();
                toggleFolder(folderId);
            }
        }
        
        // Keyboard shortcuts
        document.addEventListener('keydown', function(e) {
            // Ctrl/Cmd + K for search focus
            if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
                e.preventDefault();
                document.getElementById('search-box').focus();
            }
            // Ctrl/Cmd + E for toggle expand/collapse all
            if ((e.ctrlKey || e.metaKey) && e.key === 'e') {
                e.preventDefault();
                toggleExpandAll();
            }
            // Ctrl/Cmd + B for toggle sidebar
            if ((e.ctrlKey || e.metaKey) && e.key === 'b') {
                e.preventDefault();
                toggleSidebar();
            }
        });

        // Theme Configuration - Generated from ThemeConfig.cs
        const themes = " + GenerateJavaScriptThemes() + @";

        // Change theme function
        function changeTheme(themeName) {
            const theme = themes[themeName];
            if (!theme) return;

            const root = document.documentElement;
            root.style.setProperty('--primary-color', theme.primaryColor);
            root.style.setProperty('--primary-gradient', theme.primaryGradient);
            root.style.setProperty('--success-color', theme.successColor);
            root.style.setProperty('--danger-color', theme.dangerColor);
            root.style.setProperty('--warning-color', theme.warningColor);
            root.style.setProperty('--info-color', theme.infoColor);
            root.style.setProperty('--bg-color', theme.bgColor);
            root.style.setProperty('--card-bg', theme.cardBg);
            root.style.setProperty('--text-color', theme.textColor);
            root.style.setProperty('--text-secondary', theme.textSecondary);
            root.style.setProperty('--border-color', theme.borderColor);
            root.style.setProperty('--hover-bg', theme.hoverBg);
            root.style.setProperty('--accent-color', theme.accentColor);
            root.style.setProperty('--focus-ring', theme.focusRing);
            root.style.setProperty('--shadow-color', theme.shadowColor);
            root.style.setProperty('--code-bg', theme.codeBg);

            // Save theme preference
            localStorage.setItem('bdd-theme', themeName);
        }

        // ============================================
        // LAZY RENDERING SYSTEM
        // ============================================
        let featureDataCache = null;
        let renderedFeatures = new Set();
        
        function loadFeatureData() {
            if (!USE_LAZY_RENDERING) return null;
            if (featureDataCache) return featureDataCache;
            
            const dataElement = document.getElementById('feature-data');
            if (dataElement) {
                try {
                    featureDataCache = JSON.parse(dataElement.textContent);
                    console.log('✓ Loaded data for ' + featureDataCache.features.length + ' features');
                } catch (e) {
                    console.error('Failed to parse feature data:', e);
                    featureDataCache = { features: [] };
                }
            }
            return featureDataCache;
        }
        
        function renderFeatureContent(featureElement) {
            const featureIndex = parseInt(featureElement.getAttribute('data-feature-index'));
            if (renderedFeatures.has(featureIndex)) return;
            
            const data = loadFeatureData();
            if (!data || !data.features[featureIndex]) return;
            
            const featureHtml = data.features[featureIndex].html;
            
            // Create a temporary container to parse the HTML
            const temp = document.createElement('div');
            temp.innerHTML = featureHtml;
            const newFeatureElement = temp.firstElementChild;
            
            // Replace the placeholder with the actual feature content
            if (newFeatureElement && featureElement.parentNode) {
                // Remove feature-hidden class to make the feature visible
                newFeatureElement.classList.remove('feature-hidden');
                featureElement.parentNode.replaceChild(newFeatureElement, featureElement);
            }
            
            renderedFeatures.add(featureIndex);
        }
        
        function initLazyRendering() {
            if (!USE_LAZY_RENDERING) return;
            
            const lazyFeatures = document.querySelectorAll('.lazy-feature[data-lazy]');
            if (lazyFeatures.length === 0) return;
            
            console.log('⚡ Initializing lazy rendering for ' + lazyFeatures.length + ' features');
            
            // Render first 10 features immediately for instant visibility
            const initialRenderCount = Math.min(10, lazyFeatures.length);
            for (let i = 0; i < initialRenderCount; i++) {
                renderFeatureContent(lazyFeatures[i]);
            }
            
            // Use IntersectionObserver for remaining features
            const observerOptions = {
                root: null,
                rootMargin: '500px 0px', // Load 500px before entering viewport
                threshold: 0
            };
            
            const lazyObserver = new IntersectionObserver(function(entries) {
                entries.forEach(entry => {
                    if (entry.isIntersecting) {
                        const feature = entry.target;
                        if (feature.hasAttribute('data-lazy')) {
                            renderFeatureContent(feature);
                            lazyObserver.unobserve(feature); // Stop observing after rendering
                        }
                    }
                });
            }, observerOptions);
            
            // Observe features that weren't initially rendered
            for (let i = initialRenderCount; i < lazyFeatures.length; i++) {
                lazyObserver.observe(lazyFeatures[i]);
            }
        }
        
        // Load saved theme on page load
        document.addEventListener('DOMContentLoaded', function() {
            // Initialize lazy rendering first (if enabled)
            initLazyRendering();
            
            // Attach filter event listeners after lazy rendering is initialized
            document.querySelectorAll('.filter-btn[data-filter]').forEach(btn => {
                btn.addEventListener('click', function() {
                    const filter = this.dataset.filter;
                    // Use clearAllFilters for 'all' button, filterByStatus for specific filters
                    if (filter === 'all') {
                        clearAllFilters();
                    } else {
                        filterByStatus(filter);
                    }
                });
            });
            
            // Clear All button
            const clearAllBtn = document.getElementById('clear-all-filters-btn');
            if (clearAllBtn) {
                clearAllBtn.addEventListener('click', function() {
                    clearAllFilters();
                });
            }
            
            const tagFilterDropdown = document.getElementById('tag-filter');
            if (tagFilterDropdown) {
                tagFilterDropdown.addEventListener('change', function() {
                    const selectedTag = this.value;
                    filterByTag(selectedTag);
                });
            }
            
            const savedTheme = localStorage.getItem('bdd-theme') || 'purple';
            const themeSelector = document.getElementById('theme-selector');
            themeSelector.value = savedTheme;
            changeTheme(savedTheme);
            
            // Populate tag filter dropdown
            const tagFilter = document.getElementById('tag-filter');
            const allTags = new Set();
            
            document.querySelectorAll('.tag').forEach(tag => {
                // Extract tag text, excluding icon by cloning and removing it
                const clone = tag.cloneNode(true);
                const icon = clone.querySelector('i');
                if (icon) {
                    icon.remove();
                }
                const tagText = clone.textContent.trim();
                if (tagText) allTags.add(tagText);
            });
            
            const sortedTags = Array.from(allTags).sort();
            sortedTags.forEach(tag => {
                const option = document.createElement('option');
                option.value = tag;
                option.textContent = tag;
                tagFilter.appendChild(option);
            });
            
            // Scroll to top button
            const scrollToTopBtn = document.getElementById('scroll-to-top');
            
            window.addEventListener('scroll', function() {
                if (window.pageYOffset > 300) {
                    scrollToTopBtn.classList.add('visible');
                } else {
                    scrollToTopBtn.classList.remove('visible');
                }
            });
            
            scrollToTopBtn.addEventListener('click', function() {
                window.scrollTo({
                    top: 0,
                    behavior: 'smooth'
                });
            });
            
            // Setup IntersectionObserver to update sidebar active state (optimized)
            if (!USE_LAZY_RENDERING) {
                // Only use scenario observer for non-lazy reports
                setupScenarioObserver();
            } else {
                // For lazy reports, use simpler feature-level tracking
                setupFeatureLevelObserver();
            }
        });

        // ============================================
        // SIDEBAR NAVIGATION & MASTER-DETAIL
        // ============================================
        
        // Optimized observer for lazy-rendered reports (feature-level only)
        function setupFeatureLevelObserver() {
            const options = {
                root: null,
                rootMargin: '-20% 0px -60% 0px',
                threshold: 0
            };
            
            const observer = new IntersectionObserver(function(entries) {
                entries.forEach(entry => {
                    if (entry.isIntersecting) {
                        const feature = entry.target;
                        const featureId = feature.getAttribute('data-feature-id');
                        
                        if (featureId) {
                            updateSidebarActive(featureId);
                        }
                    }
                });
            }, options);
            
            // Only observe feature containers (much lighter than all scenarios)
            document.querySelectorAll('.feature[data-feature-id]').forEach(feature => {
                observer.observe(feature);
            });
        }
        
        // Original scenario-level observer for smaller reports
        function setupScenarioObserver() {
            const options = {
                root: null,
                rootMargin: '-20% 0px -60% 0px',
                threshold: 0
            };
            
            const observer = new IntersectionObserver(function(entries) {
                entries.forEach(entry => {
                    if (entry.isIntersecting) {
                        const scenario = entry.target;
                        const featureId = scenario.getAttribute('data-feature-id');
                        
                        if (featureId) {
                            updateSidebarActive(featureId);
                        }
                    }
                });
            }, options);
            
            // Observe all scenarios
            document.querySelectorAll('.scenario[data-feature-id]').forEach(scenario => {
                observer.observe(scenario);
            });
        }
        
        // Shared function to update sidebar active state
        function updateSidebarActive(featureId) {
            // Update sidebar active state without hiding other features
            document.querySelectorAll('.feature-item').forEach(item => {
                item.classList.remove('active');
            });
            
            const activeItem = document.querySelector('.feature-item[data-feature-id=""' + featureId + '""]');
            if (activeItem) {
                activeItem.classList.add('active');
                
                // Scroll sidebar to show active item if needed
                const sidebar = document.getElementById('sidebar');
                const sidebarNav = sidebar?.querySelector('nav');
                if (sidebarNav && activeItem) {
                    const itemTop = activeItem.offsetTop;
                    const itemBottom = itemTop + activeItem.offsetHeight;
                    const sidebarTop = sidebarNav.scrollTop;
                    const sidebarBottom = sidebarTop + sidebarNav.clientHeight;
                    
                    if (itemTop < sidebarTop || itemBottom > sidebarBottom) {
                        activeItem.scrollIntoView({ behavior: 'smooth', block: 'center' });
                    }
                }
            }
        }
        
        // Select Feature
        function selectFeature(featureId) {
            // Hide all features
            document.querySelectorAll('.feature[data-feature-id]').forEach(feature => {
                feature.classList.add('feature-hidden');
            });
            
            // Show selected feature
            let selectedFeature = document.getElementById(featureId);
            if (selectedFeature) {
                // Render lazy-loaded feature content if needed
                if (USE_LAZY_RENDERING && selectedFeature.hasAttribute('data-lazy')) {
                    renderFeatureContent(selectedFeature);
                    // Get the element again after rendering (it was replaced)
                    selectedFeature = document.getElementById(featureId);
                }
                
                // Remove hidden class from the (possibly new) element
                if (selectedFeature) {
                    selectedFeature.classList.remove('feature-hidden');
                    currentFeatureId = featureId;
                }
                
                // Update active state in sidebar
                document.querySelectorAll('.feature-item').forEach(item => {
                    item.classList.remove('active');
                });
                const activeItem = document.querySelector('.feature-item[data-feature-id=""' + featureId + '""]');
                if (activeItem) {
                    activeItem.classList.add('active');
                }
                
                // Scroll to top of content
                const mainContent = document.getElementById('main-content');
                if (mainContent) {
                    mainContent.scrollTop = 0;
                }
                
                // Save last viewed feature
                localStorage.setItem('bdd-last-feature', featureId);
            }
        }
        
        // Toggle Folder
        function toggleFolder(folderId) {
            const folder = document.getElementById(folderId).closest('.folder');
            if (folder) {
                folder.classList.toggle('collapsed');
            }
        }
        
        // Sidebar Toggle Function
        function toggleSidebar() {
            const sidebar = document.getElementById('sidebar');
            const floatingToggle = document.getElementById('floating-sidebar-toggle');
            const sidebarToggle = document.getElementById('sidebar-toggle');
            const isCollapsed = sidebar.classList.toggle('collapsed');
            
            // Update ARIA attributes
            sidebar.setAttribute('aria-hidden', isCollapsed);
            if (sidebarToggle) {
                sidebarToggle.setAttribute('aria-expanded', !isCollapsed);
            }
            
            // Update icon in sidebar toggle button
            const icon = document.querySelector('#sidebar-toggle i');
            if (icon) {
                if (isCollapsed) {
                    icon.className = 'fas fa-angles-right';
                } else {
                    icon.className = 'fas fa-angles-left';
                }
            }
            
            // Toggle floating button visibility
            if (floatingToggle) {
                if (isCollapsed) {
                    floatingToggle.classList.add('visible');
                    floatingToggle.setAttribute('aria-hidden', 'false');
                } else {
                    floatingToggle.classList.remove('visible');
                    floatingToggle.setAttribute('aria-hidden', 'true');
                }
            }
            
            // Save state
            localStorage.setItem('bdd-sidebar-collapsed', isCollapsed);
        }
        
        // Sidebar Toggle (inside sidebar)
        document.getElementById('sidebar-toggle')?.addEventListener('click', toggleSidebar);
        
        // Floating Toggle Button (visible when collapsed)
        document.getElementById('floating-sidebar-toggle')?.addEventListener('click', toggleSidebar);
        
        // Sidebar Resize
        const resizer = document.querySelector('.resizer');
        const sidebar = document.getElementById('sidebar');
        let isResizing = false;
        
        if (resizer && sidebar) {
            resizer.addEventListener('mousedown', function(e) {
                isResizing = true;
                resizer.classList.add('resizing');
                document.body.style.cursor = 'col-resize';
                document.body.style.userSelect = 'none';
                e.preventDefault();
            });
            
            document.addEventListener('mousemove', function(e) {
                if (!isResizing) return;
                
                const layoutContainer = document.querySelector('.layout-container');
                if (!layoutContainer) return;
                
                const containerRect = layoutContainer.getBoundingClientRect();
                const newWidth = e.clientX - containerRect.left;
                
                // Dynamic max width based on container size (max 40% of container)
                const minWidth = 200;
                const maxWidth = Math.min(500, containerRect.width * 0.4);
                
                if (newWidth >= minWidth && newWidth <= maxWidth) {
                    sidebar.style.width = newWidth + 'px';
                }
            });
            
            document.addEventListener('mouseup', function() {
                if (isResizing) {
                    isResizing = false;
                    resizer.classList.remove('resizing');
                    document.body.style.cursor = '';
                    document.body.style.userSelect = '';
                    
                    // Save width
                    localStorage.setItem('bdd-sidebar-width', sidebar.style.width);
                }
            });
        }
        
        // Enhanced Keyboard Shortcuts
        document.addEventListener('keydown', function(e) {
            // Cmd/Ctrl + B: Toggle sidebar
            if ((e.ctrlKey || e.metaKey) && e.key === 'b') {
                e.preventDefault();
                toggleSidebar();
            }
        });
        
        // Load Saved State on Page Load
        document.addEventListener('DOMContentLoaded', function() {
            // Restore sidebar width (only on desktop)
            if (window.innerWidth > 768) {
                const savedWidth = localStorage.getItem('bdd-sidebar-width');
                if (savedWidth && sidebar) {
                    const width = parseInt(savedWidth);
                    const layoutContainer = document.querySelector('.layout-container');
                    if (layoutContainer) {
                        const maxWidth = Math.min(500, layoutContainer.offsetWidth * 0.4);
                        // Ensure saved width is within valid range
                        if (width >= 200 && width <= maxWidth) {
                            sidebar.style.width = savedWidth;
                        }
                    }
                }
            }
            
            // Restore sidebar collapsed state
            const isCollapsed = localStorage.getItem('bdd-sidebar-collapsed') === 'true';
            const floatingToggle = document.getElementById('floating-sidebar-toggle');
            
            if (isCollapsed && sidebar) {
                sidebar.classList.add('collapsed');
                const icon = document.querySelector('#sidebar-toggle i');
                if (icon) {
                    icon.className = 'fas fa-angles-right';
                }
                // Show floating toggle button when sidebar is collapsed
                if (floatingToggle) {
                    floatingToggle.classList.add('visible');
                }
            }
            
            // Restore last viewed feature
            const lastFeature = localStorage.getItem('bdd-last-feature');
            if (lastFeature) {
                selectFeature(lastFeature);
            }
        });
        
        // Handle window resize
        let resizeTimeout;
        window.addEventListener('resize', function() {
            clearTimeout(resizeTimeout);
            resizeTimeout = setTimeout(function() {
                if (sidebar && window.innerWidth > 768) {
                    const layoutContainer = document.querySelector('.layout-container');
                    if (layoutContainer) {
                        const currentWidth = parseInt(sidebar.style.width || '280');
                        const maxWidth = Math.min(500, layoutContainer.offsetWidth * 0.4);
                        
                        // Adjust sidebar width if it exceeds new maximum
                        if (currentWidth > maxWidth) {
                            sidebar.style.width = maxWidth + 'px';
                            localStorage.setItem('bdd-sidebar-width', sidebar.style.width);
                        }
                    }
                } else if (window.innerWidth <= 768 && sidebar) {
                    // Reset inline width on mobile
                    sidebar.style.width = '';
                }
            }, 250);
        });
    </script>";
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

    /// <summary>
    /// Generates JavaScript object literal from ThemeConfig.cs themes
    /// </summary>
    private static string GenerateJavaScriptThemes()
    {
        var js = new StringBuilder();
        js.AppendLine("{");
        
        var themes = ThemeConfig.Themes;
        var themeNames = themes.Keys.ToList();
        
        for (int i = 0; i < themeNames.Count; i++)
        {
            var themeName = themeNames[i];
            var theme = themes[themeName];
            
            js.AppendLine($"            {themeName}: {{");
            js.AppendLine($"                primaryColor: '{theme.PrimaryColor}',");
            js.AppendLine($"                primaryGradient: '{theme.PrimaryGradient}',");
            js.AppendLine($"                successColor: '{theme.SuccessColor}',");
            js.AppendLine($"                dangerColor: '{theme.DangerColor}',");
            js.AppendLine($"                warningColor: '{theme.WarningColor}',");
            js.AppendLine($"                infoColor: '{theme.InfoColor}',");
            js.AppendLine($"                bgColor: '{theme.BgColor}',");
            js.AppendLine($"                cardBg: '{theme.CardBg}',");
            js.AppendLine($"                textColor: '{theme.TextColor}',");
            js.AppendLine($"                textSecondary: '{theme.TextSecondary}',");
            js.AppendLine($"                borderColor: '{theme.BorderColor}',");
            js.AppendLine($"                hoverBg: '{theme.HoverBg}',");
            js.AppendLine($"                accentColor: '{theme.AccentColor}',");
            js.AppendLine($"                focusRing: '{theme.FocusRing}',");
            js.AppendLine($"                shadowColor: '{theme.ShadowColor}',");
            js.AppendLine($"                codeBg: '{theme.CodeBg}'");
            js.Append($"            }}");
            
            if (i < themeNames.Count - 1)
                js.AppendLine(",");
            else
                js.AppendLine();
        }
        
        js.Append("        }");
        return js.ToString();
    }
    
    /// <summary>
    /// Generates JSON data for lazy-loaded features
    /// </summary>
    private string GenerateFeatureDataJson(LivingDocumentation documentation)
    {
        var json = new StringBuilder();
        json.AppendLine("{");
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
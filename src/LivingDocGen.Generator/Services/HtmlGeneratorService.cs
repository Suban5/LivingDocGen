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
/// RECOMMENDED LIMITS:
/// - Optimal performance: < 50 features
/// - Good performance: 50-200 features  
/// - Acceptable: 200-500 features
/// - Large report mode: > 500 features (consider pagination or filtering)
/// </summary>
public class HtmlGeneratorService : IHtmlGeneratorService
{
    // HTML encoding cache to avoid redundant encoding operations
    private readonly Dictionary<string, string> _encodingCache = new Dictionary<string, string>(StringComparer.Ordinal);
    private const int MaxCacheSize = 5000; // Prevent unbounded growth
    
    // CSS theme cache to avoid regenerating CSS for each theme
    private static readonly Dictionary<string, string> _cssCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    private static readonly object _cssLock = new object();
    
    /// <summary>
    /// Cached HTML encoding with LRU-like behavior
    /// </summary>
    private string HtmlEncode(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;
            
        if (_encodingCache.TryGetValue(text, out var encoded))
            return encoded;
            
        // Clear cache if it gets too large
        if (_encodingCache.Count >= MaxCacheSize)
            _encodingCache.Clear();
            
        encoded = System.Web.HttpUtility.HtmlEncode(text);
        _encodingCache[text] = encoded;
        return encoded;
    }
    
    public string GenerateHtml(LivingDocumentation documentation, HtmlGenerationOptions? options = null)
    {
        options ??= new HtmlGenerationOptions();
        
        // Pre-calculate approximate capacity for better performance
        // Estimate: ~10KB base + ~5KB per feature
        var estimatedCapacity = 10240 + (documentation.Features.Count * 5120);
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
        
        for (int i = 0; i < documentation.Features.Count; i++)
        {
            html.AppendLine(GenerateFeature(documentation.Features[i], i));
        }
        
        html.AppendLine("</main>");
        html.AppendLine("</div>"); // End layout-container
        
        // Scroll to Top Button
        html.AppendLine(@"
    <button id=""scroll-to-top"" title=""Back to top"" aria-label=""Scroll to top"">
        <i class=""fas fa-arrow-up""></i>
    </button>");
        
        // Footer
        html.AppendLine(GenerateFooter(documentation));
        
        // Embedded JavaScript
        html.AppendLine(GenerateJavaScript(documentation));
        
        html.AppendLine("</body>");
        html.AppendLine("</html>");
        
        return html.ToString();
    }

    private string GenerateHead(LivingDocumentation documentation, HtmlGenerationOptions options)
    {
        var head = new StringBuilder(4096); // CSS is large, allocate enough space
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
        
        // Cache the result
        lock (_cssLock)
        {
            if (!_cssCache.ContainsKey(themeName))
            {
                _cssCache[themeName] = css;
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
        }

        header h1 {
            font-size: 1.75rem;
            margin-bottom: 0.25rem;
            font-weight: 700;
            letter-spacing: -0.02em;
        }

        header .subtitle {
            opacity: 0.9;
            font-size: 0.95rem;
            font-weight: 300;
            letter-spacing: 0.01em;
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

        /* Controls Section */
        #controls {
            background: var(--card-bg);
            padding: 1.5rem;
            margin: 2rem auto;
            max-width: 1400px;
            border-radius: 12px;
            box-shadow: 0 2px 8px rgba(0,0,0,0.08), 0 1px 3px rgba(0,0,0,0.06);
            display: grid;
            grid-template-columns: 1fr auto auto auto;
            gap: 1rem;
            align-items: center;
        }

        #search-box {
            padding: 0.75rem 3rem 0.75rem 3rem;
            border: 2px solid var(--border-color);
            border-radius: 8px;
            font-size: 1rem;
            width: 100%;
            background-color: var(--card-bg);
            background-image: url('data:image/svg+xml,%3Csvg xmlns=%22http://www.w3.org/2000/svg%22 width=%2220%22 height=%2220%22 viewBox=%220 0 24 24%22 fill=%22none%22 stroke=%22%236b7280%22 stroke-width=%222%22%3E%3Ccircle cx=%2211%22 cy=%2211%22 r=%228%22%3E%3C/circle%3E%3Cpath d=%22m21 21-4.35-4.35%22%3E%3C/path%3E%3C/svg%3E');
            background-repeat: no-repeat;
            background-position: 10px center;
            color: var(--text-color);
        }

        #search-box:focus {
            outline: none;
            border-color: var(--primary-color);
        }

        #search-box:focus-visible {
            outline: 3px solid var(--primary-color);
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
            outline: 2px solid var(--primary-color);
            outline-offset: 2px;
        }

        .search-clear-btn:active {
            transform: translateY(-50%) scale(0.9);
        }

        /* Search result count */
        .search-result-count {
            position: absolute;
            right: 3rem;
            top: 50%;
            transform: translateY(-50%);
            font-size: 0.85rem;
            color: var(--text-secondary);
            background: var(--hover-bg);
            padding: 0.25rem 0.75rem;
            border-radius: 12px;
            font-weight: 500;
            pointer-events: none;
        }

        #controls {
            position: relative;
        }

        .search-wrapper {
            position: relative;
        }

        .filter-group, .theme-group {
            display: flex;
            gap: 0.5rem;
        }

        .theme-selector {
            padding: 0.75rem 1rem;
            border: 2px solid var(--border-color);
            background: var(--card-bg);
            color: var(--text-color);
            border-radius: 8px;
            cursor: pointer;
            font-size: 0.95rem;
            font-weight: 500;
            transition: all 0.2s;
            min-width: 180px;
        }

        .theme-selector:hover {
            background: var(--hover-bg);
            border-color: var(--primary-color);
        }

        .theme-selector:focus {
            outline: none;
            border-color: var(--primary-color);
            box-shadow: 0 0 0 3px rgba(99, 102, 241, 0.1);
        }

        .theme-selector:focus-visible {
            outline: 3px solid var(--primary-color);
            outline-offset: 2px;
        }

        .filter-btn {
            padding: 0.75rem 1.25rem;
            border: 2px solid var(--border-color);
            background: var(--card-bg);
            color: var(--text-color);
            border-radius: 8px;
            cursor: pointer;
            font-size: 0.95rem;
            font-weight: 500;
            transition: all 0.2s;
            display: flex;
            align-items: center;
            gap: 0.5rem;
        }

        .filter-btn:hover {
            background: var(--hover-bg);
        }

        .filter-btn:focus-visible {
            outline: 3px solid var(--primary-color);
            outline-offset: 2px;
        }

        .filter-btn.active {
            border-color: var(--primary-color);
            background: var(--primary-color);
            color: white;
        }

        /* Statistics Dashboard */
        #stats {
            max-width: 1400px;
            margin: 1.25rem auto;
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
            gap: 1rem;
        }

        .stat-card {
            background: var(--card-bg);
            padding: 1rem 1.25rem;
            border-radius: 10px;
            box-shadow: 0 2px 6px rgba(0,0,0,0.06), 0 1px 2px rgba(0,0,0,0.04);
            transition: transform 0.2s ease, box-shadow 0.2s ease;
        }

        .stat-card:hover {
            transform: translateY(-3px);
            box-shadow: 0 4px 12px rgba(0,0,0,0.1), 0 2px 4px rgba(0,0,0,0.06);
        }

        .stat-card .icon {
            font-size: 1.5rem;
            margin-bottom: 0.25rem;
        }

        .stat-card .value {
            font-size: 1.75rem;
            font-weight: 700;
            margin: 0.25rem 0;
            letter-spacing: -0.02em;
        }

        .stat-card .label {
            color: var(--text-secondary);
            font-size: 0.9rem;
            font-weight: 500;
            letter-spacing: 0.01em;
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
            box-shadow: 0 2px 8px rgba(0,0,0,0.08), 0 1px 3px rgba(0,0,0,0.06);
            overflow: hidden;
            transition: box-shadow 0.3s ease, transform 0.2s ease;
            animation: fadeIn 0.3s ease-out;
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
            box-shadow: 0 4px 16px rgba(0,0,0,0.12), 0 2px 6px rgba(0,0,0,0.08);
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
            display: none;
        }

        .feature-body.expanded {
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
            padding: 1rem;
            max-height: 0;
            overflow: hidden;
            transition: max-height 0.3s ease-out;
            background: transparent;
        }

        .background-body.expanded {
            max-height: 5000px;
            transition: max-height 0.5s ease-in;
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
            padding: 1.5rem;
            background: var(--hover-bg);
            max-height: 0;
            overflow: hidden;
            transition: max-height 0.3s ease-out;
        }

        .rule-body.expanded {
            max-height: 10000px;
            transition: max-height 0.5s ease-in;
        }

        .rule-description {
            margin-bottom: 1.5rem;
            color: var(--text-secondary);
        }
            font-style: italic;
            padding: 0.75rem;
            background: white;
            border-radius: 6px;
            border-left: 3px solid #8b5cf6;
        }

        /* Scenarios */
        .scenario {
            background: var(--bg-color);
            margin-bottom: 1rem;
            border-radius: 8px;
            border-left: 3px solid var(--border-color);
            overflow: hidden;
            box-shadow: 0 1px 4px rgba(0,0,0,0.06);
            transition: box-shadow 0.2s ease, transform 0.2s ease;
        }

        .scenario:hover {
            box-shadow: 0 2px 8px rgba(0,0,0,0.1);
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
            display: none;
        }

        .scenario-body.expanded {
            display: block;
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
            box-shadow: 0 1px 3px rgba(0,0,0,0.04);
            transition: box-shadow 0.2s ease;
        }

        .step:hover {
            box-shadow: 0 2px 6px rgba(0,0,0,0.08);
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
            box-shadow: 0 2px 8px rgba(0,0,0,0.08), 0 1px 3px rgba(0,0,0,0.06);
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
            box-shadow: 0 1px 3px rgba(0,0,0,0.1);
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
            padding: 1rem;
            margin-top: 2rem;
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
            box-shadow: 0 4px 12px rgba(0,0,0,0.15);
            transition: all 0.3s ease;
            z-index: 999;
        }

        #scroll-to-top:hover {
            transform: translateY(-4px);
            box-shadow: 0 6px 20px rgba(0,0,0,0.2);
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
            min-height: 600px;
            max-width: 1400px;
            margin: 0 auto 2rem;
            position: relative;
            border-radius: 8px;
            overflow: hidden;
            box-shadow: 0 1px 3px rgba(0,0,0,0.1);
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

        .sidebar-search {
            padding: 0.75rem;
            border-bottom: 1px solid var(--border-color);
        }

        .sidebar-search input {
            width: 100%;
            padding: 0.5rem;
            border: 1px solid var(--border-color);
            border-radius: 4px;
            background: var(--bg-color);
            color: var(--text-color);
            font-size: 0.9rem;
        }

        .sidebar-search input:focus {
            outline: none;
            border-color: var(--primary-color);
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
            outline: 2px solid var(--primary-color);
            outline-offset: -2px;
        }

        .folder-icon {
            color: var(--primary-color);
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
            border-left-color: var(--primary-color);
        }

        .feature-item:focus {
            outline: none;
            background: var(--hover-bg);
        }

        .feature-item:focus-visible {
            outline: 2px solid var(--primary-color);
            outline-offset: -2px;
        }

        .feature-item.active {
            background: var(--primary-color);
            color: white;
            font-weight: 600;
            border-left-color: white;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
        }

        .feature-item.active .feature-status {
            color: white !important;
        }

        .feature-status {
            font-size: 0.9rem;
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
            box-shadow: 0 2px 8px rgba(0,0,0,0.2);
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
            box-shadow: 0 4px 12px rgba(0,0,0,0.3);
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
        <div class=""search-wrapper"">
            <input type=""text"" 
                   id=""search-box"" 
                   placeholder=""Search features, scenarios, steps...""
                   aria-label=""Search documentation""
                   aria-describedby=""search-result-count"">
            <button type=""button"" 
                    id=""search-clear-btn"" 
                    class=""search-clear-btn"" 
                    aria-label=""Clear search""
                    title=""Clear search (Esc)"">
                <i class=""fas fa-times"" aria-hidden=""true""></i>
            </button>
            <span id=""search-result-count"" class=""search-result-count"" aria-live=""polite""></span>
        </div>
        <div class=""filter-group"" role=""group"" aria-label=""Status filters"">
            <button class=""filter-btn"" data-filter=""all"" aria-label=""Show all scenarios"">
                <i class=""fas fa-list"" aria-hidden=""true""></i> All
            </button>
            <button class=""filter-btn"" data-filter=""passed"" aria-label=""Show only passed scenarios"">
                <i class=""fas fa-check-circle"" aria-hidden=""true""></i> Passed
            </button>
            <button class=""filter-btn"" data-filter=""failed"" aria-label=""Show only failed scenarios"">
                <i class=""fas fa-times-circle"" aria-hidden=""true""></i> Failed
            </button>
            <button class=""filter-btn"" data-filter=""skipped"" aria-label=""Show only skipped scenarios"">
                <i class=""fas fa-minus-circle"" aria-hidden=""true""></i> Skipped
            </button>
        </div>
        <div class=""theme-group"" role=""group"" aria-label=""Theme selection"">
            <select id=""theme-selector"" 
                    class=""theme-selector"" 
                    onchange=""changeTheme(this.value)""
                    aria-label=""Select theme"">
                <option value=""purple"">🎨 Purple</option>
                <option value=""blue"">🌊 Ocean Blue</option>
                <option value=""green"">🌲 Forest Green</option>
                <option value=""dark"">🌙 Dark Mode</option>
                <option value=""light"">☀️ Clean Light</option>
                <option value=""pickles"">🥒 Pickles Classic</option>
            </select>
        </div>
        <button id=""toggle-expand-btn"" 
                class=""filter-btn"" 
                onclick=""toggleExpandAll()""
                aria-label=""Expand or collapse all sections""
                aria-expanded=""false"">
            <i class=""fas fa-expand"" aria-hidden=""true""></i> <span id=""expand-btn-text"">Expand All</span>
        </button>
    </div>";
    }

    private string GenerateStatistics(LivingDocumentation documentation)
    {
        var stats = documentation.Statistics;
        return $@"
    <div id=""stats"">
        <div class=""stat-card stat-info"">
            <div class=""icon""><i class=""fas fa-file-alt""></i></div>
            <div class=""value"">{stats.TotalFeatures}</div>
            <div class=""label"">Features</div>
        </div>
        <div class=""stat-card stat-info"">
            <div class=""icon""><i class=""fas fa-list-check""></i></div>
            <div class=""value"">{stats.TotalScenarios}</div>
            <div class=""label"">Scenarios</div>
        </div>
        <div class=""stat-card stat-passed"">
            <div class=""icon""><i class=""fas fa-check-circle""></i></div>
            <div class=""value"">{stats.PassedScenarios}</div>
            <div class=""label"">Passed ({stats.PassRate:F1}%)</div>
        </div>
        <div class=""stat-card stat-failed"">
            <div class=""icon""><i class=""fas fa-times-circle""></i></div>
            <div class=""value"">{stats.FailedScenarios}</div>
            <div class=""label"">Failed</div>
        </div>
        <div class=""stat-card stat-skipped"">
            <div class=""icon""><i class=""fas fa-minus-circle""></i></div>
            <div class=""value"">{stats.SkippedScenarios}</div>
            <div class=""label"">Skipped</div>
        </div>
        <div class=""stat-card stat-info"">
            <div class=""icon""><i class=""fas fa-chart-line""></i></div>
            <div class=""value"">{stats.Coverage:F1}%</div>
            <div class=""label"">Coverage</div>
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
        
        <div class=""sidebar-search"">
            <input type=""text"" 
                   id=""sidebar-search"" 
                   placeholder=""Search features...""
                   aria-label=""Search features in sidebar"" />
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

    private string GenerateFeature(EnrichedFeature feature, int index = 0)
    {
        var statusClass = GetStatusClass(feature.OverallStatus);
        var statusIcon = GetStatusIcon(feature.OverallStatus);
        var featureId = $"feature-{index}";
        var isFirstFeature = index == 0;
        
        var html = new StringBuilder();
        html.AppendLine($@"
    <div class=""feature{(isFirstFeature ? "" : " feature-hidden")}"" data-status=""{statusClass}"" id=""{featureId}"" data-feature-id=""{featureId}"">
        <div class=""feature-header status-{statusClass}"" onclick=""toggleFeature(this)"">
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
                <div class=""background-header"" onclick=""toggleBackground(this)"">
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
                            <span class=""step-keyword"">{System.Web.HttpUtility.HtmlEncode(step.Keyword)}</span>
                            <span class=""step-text"">{System.Web.HttpUtility.HtmlEncode(step.Text)}</span>
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
                html.AppendLine($@"                            <div class=""data-table-header"" onclick=""toggleDataTable(this)"">
                                <i class=""fas fa-chevron-down toggle-icon""></i>
                                <i class=""fas fa-table""></i>
                                <span>Data Table ({rowCount} rows × {colCount} columns)</span>
                            </div>");
                html.AppendLine(@"                            <div class=""data-table-wrapper"">");
                html.AppendLine(@"                                <table class=""data-table"">");
                
                // First row as headers
                var headers = step.DataTable.Rows[0];
                html.AppendLine(@"                                    <thead><tr>");
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
                <div class=""rule-header"" onclick=""toggleRule(this)"">
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
                <div class=""scenario-header"" onclick=""toggleScenario(this)"">
                    <div class=""scenario-title"">
                        <span class=""status-icon {statusClass}"">{statusIcon}</span>
                        <span class=""scenario-type"">{(isOutline ? "Scenario Outline:" : "Scenario:")}</span>
                        <strong>{System.Web.HttpUtility.HtmlEncode(scenario.Scenario.Name)}</strong>");

        if (isOutline)
        {
            html.AppendLine($@"                        <span class=""badge badge-outline""><i class=""fas fa-layer-group""></i> Outline</span>");
        }

        if (scenario.Duration.HasValue)
        {
            html.AppendLine($@"                        <span class=""step-duration"">({scenario.Duration.Value.TotalSeconds:F2}s)</span>");
        }

        html.AppendLine(@"                    </div>
                </div>
                <div class=""scenario-body"">");

        if (!string.IsNullOrEmpty(scenario.ErrorMessage))
        {
            html.AppendLine($@"
                    <div class=""error-message"">
                        <strong><i class=""fas fa-exclamation-triangle""></i> Error{(scenario.FailedAtLine.HasValue ? $" at line {scenario.FailedAtLine}" : "")}</strong>
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
        html.AppendLine($@"                            <span class=""step-keyword"">{System.Web.HttpUtility.HtmlEncode(step.Step.Keyword)}</span>");
        html.AppendLine($@"                            <div class=""step-text"">");
        html.AppendLine($@"                                <div>{System.Web.HttpUtility.HtmlEncode(step.Step.Text)}");
        
        if (step.Duration.HasValue && step.Duration.Value.TotalMilliseconds > 0)
        {
            html.AppendLine($@" <span class=""step-duration"">({step.Duration.Value.TotalMilliseconds:F0}ms)</span>");
        }
        
        html.AppendLine(@"</div>");

        // Add error message if failed
        if (!string.IsNullOrEmpty(step.ErrorMessage))
        {
            html.AppendLine($@"                                <div style=""color: var(--danger-color); margin-top: 0.5rem;""><small>{System.Web.HttpUtility.HtmlEncode(step.ErrorMessage)}</small></div>");
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

        html.AppendLine(@"                            </div>
                        </li>");

        return html.ToString();
    }

    private string GenerateDataTable(UniversalDataTable dataTable)
    {
        var html = new StringBuilder();
        var rowCount = dataTable.Rows.Count > 1 ? dataTable.Rows.Count - 1 : 0;
        var colCount = dataTable.Rows.Any() ? dataTable.Rows[0].Count : 0;
        
        html.AppendLine(@"                                <div class=""data-table-container"">");
        html.AppendLine($@"                                    <div class=""data-table-header"" onclick=""toggleDataTable(this)"">
                                        <i class=""fas fa-chevron-down toggle-icon""></i>
                                        <i class=""fas fa-table""></i>
                                        <span>Data Table ({rowCount} rows × {colCount} columns)</span>
                                    </div>");
        html.AppendLine(@"                                    <div class=""data-table-wrapper"">");
        html.AppendLine(@"                                        <table class=""data-table"">");
        
        if (dataTable.Rows.Any())
        {
            // First row as headers
            html.AppendLine(@"                                            <thead><tr>");
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
        html.AppendLine($@"                        <h4 class=""examples-header"" onclick=""toggleExamples(this)"">");
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
            html.AppendLine($@"                                <div class=""examples-table-header"" onclick=""toggleExamplesTable(this)"">
                                    <i class=""fas fa-chevron-down toggle-icon""></i>
                                    <i class=""fas fa-layer-group""></i>
                                    <span>Examples Table ({rowCount} rows × {colCount} columns)</span>
                                </div>");
            html.AppendLine(@"                                <div class=""table-wrapper"">");
            html.AppendLine(@"                                    <table class=""examples-table"">");
            
            // Headers - Add Status column at the beginning
            html.AppendLine(@"                                        <thead><tr>");
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

    private string GenerateFooter(LivingDocumentation documentation)
    {
        return $@"
    <footer>
        <small>Generated by BDD Living Documentation Generator • {documentation.Features.Count} features • {documentation.Statistics.TotalScenarios} scenarios • {documentation.Statistics.TotalSteps} steps</small>
    </footer>";
    }

    private string GenerateJavaScript(LivingDocumentation documentation)
    {
        var featureCount = documentation.Features.Count;
        var hasLargeReport = featureCount > 100;
        
        return @"
    <script>
        // Performance optimizations for large reports (" + featureCount + @" features)
        const PERF_LARGE_REPORT = " + hasLargeReport.ToString().ToLower() + @";
        const FEATURE_COUNT = " + featureCount + @";
        
        // Feature Navigation State
        let currentFeatureId = 'feature-0';
        
        // Performance: Use event delegation for toggle operations in large reports
        if (PERF_LARGE_REPORT) {
            console.log('⚡ Performance mode enabled for large report');
        }
        
        // Toggle feature expansion
        function toggleFeature(header) {
            const body = header.nextElementSibling;
            body.classList.toggle('expanded');
        }

        // Toggle scenario expansion
        function toggleScenario(header) {
            const body = header.nextElementSibling;
            body.classList.toggle('expanded');
        }

        // Toggle data table
        function toggleDataTable(header) {
            const wrapper = header.nextElementSibling;
            header.classList.toggle('collapsed');
            wrapper.classList.toggle('collapsed');
        }

        // Toggle doc string
        function toggleDocString(header) {
            const content = header.nextElementSibling;
            header.classList.toggle('collapsed');
            content.classList.toggle('collapsed');
        }

        // Toggle examples table
        function toggleExamplesTable(header) {
            const wrapper = header.nextElementSibling;
            header.classList.toggle('collapsed');
            wrapper.classList.toggle('collapsed');
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

        // Toggle expand/collapse all features and scenarios
        let isAllExpanded = false;
        
        function toggleExpandAll() {
            const featureBodies = document.querySelectorAll('.feature-body');
            const scenarioBodies = document.querySelectorAll('.scenario-body');
            const btn = document.getElementById('toggle-expand-btn');
            const btnText = document.getElementById('expand-btn-text');
            const icon = btn.querySelector('i');
            
            if (isAllExpanded) {
                // Collapse all
                featureBodies.forEach(el => el.classList.remove('expanded'));
                scenarioBodies.forEach(el => el.classList.remove('expanded'));
                icon.className = 'fas fa-expand';
                btnText.textContent = 'Expand All';
                btn.setAttribute('aria-expanded', 'false');
                isAllExpanded = false;
            } else {
                // Expand all
                featureBodies.forEach(el => el.classList.add('expanded'));
                scenarioBodies.forEach(el => el.classList.add('expanded'));
                icon.className = 'fas fa-compress';
                btnText.textContent = 'Collapse All';
                btn.setAttribute('aria-expanded', 'true');
                isAllExpanded = true;
            }
        }
        
        // Legacy function for backward compatibility
        function expandAll() {
            if (!isAllExpanded) {
                toggleExpandAll();
            }
        }

        // Debounced search functionality with result count and clear button
        let searchTimeout;
        const searchBox = document.getElementById('search-box');
        const searchResultCount = document.getElementById('search-result-count');
        const searchClearBtn = document.getElementById('search-clear-btn');
        
        function performSearch() {
            const searchTerm = searchBox.value.toLowerCase().trim();
            const features = document.querySelectorAll('.feature');
            let visibleCount = 0;
            let totalCount = features.length;

            features.forEach(feature => {
                const text = feature.textContent.toLowerCase();
                const isVisible = !searchTerm || text.includes(searchTerm);
                feature.style.display = isVisible ? 'block' : 'none';
                if (isVisible) visibleCount++;
            });
            
            // Update result count and clear button visibility
            if (searchTerm) {
                searchResultCount.textContent = `${visibleCount} of ${totalCount}`;
                searchResultCount.style.display = 'block';
                searchClearBtn.classList.add('visible');
            } else {
                searchResultCount.style.display = 'none';
                searchClearBtn.classList.remove('visible');
            }
        }
        
        searchBox.addEventListener('input', function(e) {
            clearTimeout(searchTimeout);
            searchTimeout = setTimeout(() => {
                performSearch();
            }, 300); // 300ms debounce delay
        });
        
        // Clear search functionality
        searchClearBtn.addEventListener('click', function() {
            searchBox.value = '';
            performSearch();
            searchBox.focus();
        });
        
        // ESC key to clear search
        searchBox.addEventListener('keydown', function(e) {
            if (e.key === 'Escape' && searchBox.value) {
                e.preventDefault();
                searchBox.value = '';
                performSearch();
            }
        });

        // Filter by status with result count
        document.querySelectorAll('.filter-btn[data-filter]').forEach(btn => {
            btn.addEventListener('click', function() {
                const filter = this.dataset.filter;
                
                // Update active state
                document.querySelectorAll('.filter-btn[data-filter]').forEach(b => {
                    b.classList.remove('active');
                    b.setAttribute('aria-pressed', 'false');
                });
                this.classList.add('active');
                this.setAttribute('aria-pressed', 'true');

                // Filter scenarios
                const scenarios = document.querySelectorAll('.scenario');
                let visibleScenarios = 0;
                
                scenarios.forEach(scenario => {
                    if (filter === 'all') {
                        scenario.style.display = 'block';
                        visibleScenarios++;
                    } else {
                        const isVisible = scenario.dataset.status === filter;
                        scenario.style.display = isVisible ? 'block' : 'none';
                        if (isVisible) visibleScenarios++;
                    }
                });

                // Hide features with no visible scenarios
                let visibleFeatures = 0;
                document.querySelectorAll('.feature').forEach(feature => {
                    const visibleInFeature = Array.from(feature.querySelectorAll('.scenario'))
                        .filter(s => s.style.display !== 'none');
                    feature.style.display = visibleInFeature.length > 0 ? 'block' : 'none';
                    if (visibleInFeature.length > 0) visibleFeatures++;
                });
                
                // Announce filter results to screen readers
                const announcement = `Showing ${visibleScenarios} scenario${visibleScenarios !== 1 ? 's' : ''} in ${visibleFeatures} feature${visibleFeatures !== 1 ? 's' : ''}`;
                announceToScreenReader(announcement);
            });
        });

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

        // Theme Configuration
        const themes = {
            purple: {
                primaryColor: '#6366f1',
                primaryGradient: 'linear-gradient(135deg, #6366f1 0%, #7c3aed 100%)',
                successColor: '#10b981',
                dangerColor: '#ef4444',
                warningColor: '#f59e0b',
                infoColor: '#3b82f6',
                bgColor: '#f9fafb',
                cardBg: '#ffffff',
                textColor: '#1f2937',
                textSecondary: '#6b7280',
                borderColor: '#e5e7eb',
                hoverBg: '#f3f4f6'
            },
            blue: {
                primaryColor: '#0284c7',
                primaryGradient: 'linear-gradient(135deg, #0284c7 0%, #0369a1 100%)',
                successColor: '#10b981',
                dangerColor: '#ef4444',
                warningColor: '#f59e0b',
                infoColor: '#06b6d4',
                bgColor: '#f0f9ff',
                cardBg: '#ffffff',
                textColor: '#0c4a6e',
                textSecondary: '#475569',
                borderColor: '#bae6fd',
                hoverBg: '#e0f2fe'
            },
            green: {
                primaryColor: '#059669',
                primaryGradient: 'linear-gradient(135deg, #059669 0%, #047857 100%)',
                successColor: '#22c55e',
                dangerColor: '#ef4444',
                warningColor: '#f59e0b',
                infoColor: '#14b8a6',
                bgColor: '#f0fdf4',
                cardBg: '#ffffff',
                textColor: '#064e3b',
                textSecondary: '#475569',
                borderColor: '#bbf7d0',
                hoverBg: '#dcfce7'
            },
            dark: {
                primaryColor: '#8b5cf6',
                primaryGradient: 'linear-gradient(135deg, #8b5cf6 0%, #7c3aed 100%)',
                successColor: '#22c55e',
                dangerColor: '#f87171',
                warningColor: '#fbbf24',
                infoColor: '#60a5fa',
                bgColor: '#0f172a',
                cardBg: '#1e293b',
                textColor: '#f1f5f9',
                textSecondary: '#94a3b8',
                borderColor: '#334155',
                hoverBg: '#334155'
            },
            light: {
                primaryColor: '#4f46e5',
                primaryGradient: 'linear-gradient(135deg, #4f46e5 0%, #4338ca 100%)',
                successColor: '#16a34a',
                dangerColor: '#dc2626',
                warningColor: '#ea580c',
                infoColor: '#2563eb',
                bgColor: '#ffffff',
                cardBg: '#f8fafc',
                textColor: '#0f172a',
                textSecondary: '#64748b',
                borderColor: '#cbd5e1',
                hoverBg: '#f1f5f9'
            },
            pickles: {
                primaryColor: '#f59e0b',
                primaryGradient: 'linear-gradient(135deg, #f59e0b 0%, #d97706 100%)',
                successColor: '#10b981',
                dangerColor: '#ef4444',
                warningColor: '#f59e0b',
                infoColor: '#f59e0b',
                bgColor: '#fffbeb',
                cardBg: '#ffffff',
                textColor: '#78350f',
                textSecondary: '#92400e',
                borderColor: '#fde68a',
                hoverBg: '#fef3c7'
            }
        };

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

            // Save theme preference
            localStorage.setItem('bdd-theme', themeName);
        }

        // Load saved theme on page load
        document.addEventListener('DOMContentLoaded', function() {
            const savedTheme = localStorage.getItem('bdd-theme') || 'purple';
            const themeSelector = document.getElementById('theme-selector');
            themeSelector.value = savedTheme;
            changeTheme(savedTheme);
            
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
            
            // Setup IntersectionObserver to update sidebar active state
            setupScenarioObserver();
        });

        // ============================================
        // SIDEBAR NAVIGATION & MASTER-DETAIL
        // ============================================
        
        // Observe scenarios to update sidebar active state
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
                    }
                });
            }, options);
            
            // Observe all scenarios
            document.querySelectorAll('.scenario[data-feature-id]').forEach(scenario => {
                observer.observe(scenario);
            });
        }
        
        // Select Feature
        function selectFeature(featureId) {
            // Hide all features
            document.querySelectorAll('.feature[data-feature-id]').forEach(feature => {
                feature.classList.add('feature-hidden');
            });
            
            // Show selected feature
            const selectedFeature = document.getElementById(featureId);
            if (selectedFeature) {
                selectedFeature.classList.remove('feature-hidden');
                currentFeatureId = featureId;
                
                // Update active state in sidebar
                document.querySelectorAll('.feature-item').forEach(item => {
                    item.classList.remove('active');
                });
                const activeItem = document.querySelector('.feature-item[data-feature-id=""' + featureId + '""]');
                if (activeItem) {
                    activeItem.classList.add('active');
                }
                
                // Scroll to top of content
                document.getElementById('content').scrollTop = 0;
                
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
        
        // Sidebar Search
        document.getElementById('sidebar-search')?.addEventListener('input', function(e) {
            const searchTerm = e.target.value.toLowerCase();
            const featureItems = document.querySelectorAll('.feature-item');
            
            featureItems.forEach(item => {
                const text = item.textContent.toLowerCase();
                const folder = item.closest('.folder');
                
                if (text.includes(searchTerm)) {
                    item.style.display = 'flex';
                    // Expand parent folder
                    if (folder) {
                        folder.classList.remove('collapsed');
                    }
                } else {
                    item.style.display = 'none';
                }
            });
            
            // Hide empty folders
            document.querySelectorAll('.folder').forEach(folder => {
                const visibleItems = Array.from(folder.querySelectorAll('.feature-item'))
                    .filter(item => item.style.display !== 'none');
                folder.style.display = visibleItems.length > 0 ? 'block' : 'none';
            });
        });
        
        // Enhanced Keyboard Shortcuts
        document.addEventListener('keydown', function(e) {
            // Cmd/Ctrl + B: Toggle sidebar
            if ((e.ctrlKey || e.metaKey) && e.key === 'b') {
                e.preventDefault();
                toggleSidebar();
            }
            
            // Cmd/Ctrl + P: Focus sidebar search
            if ((e.ctrlKey || e.metaKey) && e.key === 'p') {
                e.preventDefault();
                const sidebar = document.getElementById('sidebar');
                // If sidebar is collapsed, open it first
                if (sidebar && sidebar.classList.contains('collapsed')) {
                    toggleSidebar();
                }
                // Small delay to ensure sidebar is visible before focusing
                setTimeout(() => {
                    document.getElementById('sidebar-search')?.focus();
                }, 100);
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
}

/// <summary>
/// Options for HTML generation
/// </summary>
public class HtmlGenerationOptions
{
    public string PrimaryColor { get; set; } = "#6366f1";
    public string? Theme { get; set; } = "purple";
    public bool IncludeComments { get; set; } = true;
    public bool SyntaxHighlighting { get; set; } = true;
}
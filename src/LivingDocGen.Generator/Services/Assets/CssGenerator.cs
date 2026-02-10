using System.Collections.Generic;
using LivingDocGen.Generator.Models;

namespace LivingDocGen.Generator.Services.Assets;

/// <summary>
/// Generates CSS stylesheets for HTML living documentation.
/// Handles theme-based styling with caching for performance.
/// </summary>
public class CssGenerator : ICssGenerator
{
    // CSS theme cache to avoid regenerating CSS for each theme
    private const int MaxCssThemes = 20;
    private static readonly Dictionary<string, string> _cssCache = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);
    private static readonly Queue<string> _cssCacheOrder = new Queue<string>();
    private static readonly object _cssLock = new object();
    
    /// <inheritdoc/>
    public string GetCSS(HtmlGenerationOptions options)
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

        /* Global Loading Spinner (Phase 3 optimization) */
        .global-loader {
            position: fixed;
            top: 50%;
            left: 50%;
            transform: translate(-50%, -50%);
            z-index: 10000;
            display: none;
            text-align: center;
            background: rgba(0, 0, 0, 0.7);
            padding: 2rem 3rem;
            border-radius: 12px;
            box-shadow: 0 4px 20px rgba(0, 0, 0, 0.3);
        }

        .global-loader.active {
            display: block;
            animation: fadeIn 0.2s ease-out;
        }

        .spinner {
            width: 48px;
            height: 48px;
            border: 4px solid rgba(255, 255, 255, 0.3);
            border-top-color: var(--primary-color);
            border-radius: 50%;
            animation: spin 0.8s linear infinite;
            margin: 0 auto 1rem;
        }

        @keyframes spin {
            to { transform: rotate(360deg); }
        }

        .loader-text {
            color: white;
            font-size: 0.95rem;
            font-weight: 500;
            margin: 0;
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
            padding: 0.625rem 2.5rem 0.625rem 2.5rem;
            border: 2px solid var(--border-color);
            border-radius: 8px;
            font-size: 0.95rem;
            flex: 1 1 250px;
            min-width: 200px;
            max-width: 350px;
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
            background: transparent;
            border: none;
            color: var(--text-secondary);
            cursor: pointer;
            padding: 0.4rem 0.5rem;
            border-radius: 4px;
            display: none;
            align-items: center;
            justify-content: center;
            font-size: 0.9rem;
            transition: all 0.2s;
            min-width: 28px;
            height: 28px;
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

        /* Search result count */
        .search-result-count {
            font-size: 0.8rem;
            color: var(--text-secondary);
            background: var(--hover-bg);
            padding: 0.2rem 0.6rem;
            border-radius: 10px;
            font-weight: 500;
            display: none;
            white-space: nowrap;
        }

        .search-result-count.visible {
            display: flex;
            align-items: center;
        }

        .search-wrapper {
            display: flex;
            align-items: center;
            gap: 0.5rem;
            flex: 1 1 250px;
            min-width: 200px;
            max-width: 500px;
        }

        .search-input-container {
            position: relative;
            flex: 1;
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
            max-width: 200px;
        }

        /* Tag filter specific styles for handling many tags */
        #tag-filter {
            max-width: 140px;
            text-overflow: ellipsis;
        }

        #tag-filter option {
            max-width: 300px;
            overflow: hidden;
            text-overflow: ellipsis;
            white-space: nowrap;
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
            /* Phase 3 optimization: Browser-native lazy rendering */
            content-visibility: auto;
            contain-intrinsic-size: auto 300px;
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

        /* Scenario-level tags - slightly smaller and styled differently */
        .tags.scenario-tags {
            margin-bottom: 1rem;
            margin-top: 0.5rem;
        }

        .tags.scenario-tags .tag {
            font-size: 0.8rem;
            padding: 0.2rem 0.6rem;
        }

        /* Rule-level tags */
        .tags.rule-tags {
            margin-bottom: 1rem;
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

        .sidebar-header .feature-total {
            font-size: 0.75rem;
            color: var(--text-secondary);
            font-weight: 500;
            background: var(--border-color);
            padding: 0.125rem 0.5rem;
            border-radius: 10px;
        }

        .sidebar-actions {
            display: flex;
            align-items: center;
            gap: 0.25rem;
        }

        .sidebar-action-btn {
            background: none;
            border: none;
            color: var(--text-secondary);
            cursor: pointer;
            padding: 0.375rem;
            border-radius: 4px;
            transition: all 0.2s;
            font-size: 0.75rem;
        }

        .sidebar-action-btn:hover {
            background: var(--primary-color);
            color: white;
        }

        .sidebar-action-btn:focus {
            outline: 2px solid var(--focus-ring);
            outline-offset: 1px;
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
            max-height: 10000px;  /* Increased to handle large feature counts */
            overflow: hidden;
            transition: max-height 0.3s ease;
        }

        .folder.collapsed .folder-content {
            max-height: 0;
        }

        /* Nested folder level styling for hierarchical navigation */
        .folder-level-0 { margin-left: 0; }
        .folder-level-1 { margin-left: 0.75rem; }
        .folder-level-2 { margin-left: 0.75rem; }
        .folder-level-3 { margin-left: 0.75rem; }
        .folder-level-4 { margin-left: 0.75rem; }
        .folder-level-5 { margin-left: 0.75rem; }

        /* Nested folder visual indicators */
        .folder-level-1 > .folder-header,
        .folder-level-2 > .folder-header,
        .folder-level-3 > .folder-header,
        .folder-level-4 > .folder-header,
        .folder-level-5 > .folder-header {
            border-left: 2px solid var(--border-color);
            margin-left: 0.25rem;
        }

        .folder-level-1 > .folder-header:hover,
        .folder-level-2 > .folder-header:hover,
        .folder-level-3 > .folder-header:hover,
        .folder-level-4 > .folder-header:hover,
        .folder-level-5 > .folder-header:hover {
            border-left-color: var(--primary-color);
        }

        /* Nested feature item indentation */
        .feature-level-1 { padding-left: 1.25rem; }
        .feature-level-2 { padding-left: 1.5rem; }
        .feature-level-3 { padding-left: 1.75rem; }
        .feature-level-4 { padding-left: 2rem; }
        .feature-level-5 { padding-left: 2.25rem; }

        /* Folder tree connector lines */
        .folder .folder-content {
            position: relative;
        }

        .folder-level-1 > .folder-content::before,
        .folder-level-2 > .folder-content::before,
        .folder-level-3 > .folder-content::before,
        .folder-level-4 > .folder-content::before,
        .folder-level-5 > .folder-content::before {
            content: '';
            position: absolute;
            left: 0.5rem;
            top: 0;
            bottom: 0.5rem;
            width: 1px;
            background: var(--border-color);
            opacity: 0.5;
        }

        /* Subfolder icon differentiation */
        .folder-level-1 .folder-icon,
        .folder-level-2 .folder-icon,
        .folder-level-3 .folder-icon,
        .folder-level-4 .folder-icon,
        .folder-level-5 .folder-icon {
            font-size: 0.8rem;
            opacity: 0.9;
        }

        /* Note: Removed max-height limit for deeply nested folders as it was clipping content */

        /* SCROLL OPTIMIZATION: Disable transitions during scroll to prevent flickering */
        .scrolling .feature-item,
        .scrolling .feature-item.active {
            transition: none !important;
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
            /* OPTIMIZED: Only transition specific properties instead of 'all' */
            transition: background-color 0.2s cubic-bezier(0.4, 0, 0.2, 1),
                        border-left-color 0.2s cubic-bezier(0.4, 0, 0.2, 1),
                        transform 0.2s cubic-bezier(0.4, 0, 0.2, 1),
                        box-shadow 0.2s cubic-bezier(0.4, 0, 0.2, 1),
                        color 0.2s cubic-bezier(0.4, 0, 0.2, 1);
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
            .feature {
                content-visibility: visible !important;
            }
            
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
            
            .global-loader {
                display: none !important;
            }
        }";
    }


}

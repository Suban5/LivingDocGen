using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using LivingDocGen.Generator.Models;
using LivingDocGen.TestReporter.Models;

namespace LivingDocGen.Generator.Services.Rendering;

/// <summary>
/// Renders sidebar navigation with folder tree structure for HTML living documentation.
/// </summary>
public class SidebarRenderer : ISidebarRenderer
{
    // Feature index counter for sidebar generation
    private int _sidebarFeatureIndex = 0;
    
    /// <inheritdoc/>
    public string Render(LivingDocumentation documentation)
    {
        return GenerateSidebar(documentation);
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

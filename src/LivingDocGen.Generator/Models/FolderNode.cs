using System;
using System.Collections.Generic;
using System.Linq;

namespace LivingDocGen.Generator.Models;

/// <summary>
/// Represents a node in the folder tree hierarchy for sidebar navigation.
/// Used to build a nested tree structure from feature file paths.
/// </summary>
public class FolderNode
{
    /// <summary>
    /// The display name of this folder (e.g., "Features", "Login", etc.)
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// The full path to this folder relative to the features root
    /// </summary>
    public string FullPath { get; set; } = string.Empty;
    
    /// <summary>
    /// Child folders within this folder
    /// </summary>
    public Dictionary<string, FolderNode> SubFolders { get; } = new Dictionary<string, FolderNode>(StringComparer.OrdinalIgnoreCase);
    
    /// <summary>
    /// Feature files directly within this folder (not in subfolders)
    /// </summary>
    public List<EnrichedFeature> Features { get; } = new List<EnrichedFeature>();
    
    /// <summary>
    /// Total count of features in this folder and all subfolders (recursive)
    /// </summary>
    public int TotalFeatureCount => Features.Count + SubFolders.Values.Sum(sf => sf.TotalFeatureCount);
}

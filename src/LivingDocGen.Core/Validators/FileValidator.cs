using System;
using System.IO;

namespace LivingDocGen.Core.Validators;

using LivingDocGen.Core.Exceptions;

/// <summary>
/// Validates file and directory paths
/// </summary>
public static class FileValidator
{
    /// <summary>
    /// Validates that a file exists and has the correct extension
    /// </summary>
    public static void ValidateFileExists(string filePath, string expectedExtension = null)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ValidationException("File path cannot be null or empty");
        }

        if (!File.Exists(filePath))
        {
            throw new ValidationException($"File not found: {filePath}");
        }

        if (!string.IsNullOrEmpty(expectedExtension))
        {
            var actualExtension = Path.GetExtension(filePath);
            if (!actualExtension.Equals(expectedExtension, StringComparison.OrdinalIgnoreCase))
            {
                throw new ValidationException(
                    $"Invalid file extension. Expected: {expectedExtension}, Actual: {actualExtension}");
            }
        }
    }

    /// <summary>
    /// Validates that a directory exists
    /// </summary>
    public static void ValidateDirectoryExists(string directoryPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            throw new ValidationException("Directory path cannot be null or empty");
        }

        if (!Directory.Exists(directoryPath))
        {
            throw new ValidationException($"Directory not found: {directoryPath}");
        }
    }

    /// <summary>
    /// Gets all files matching a pattern with validation
    /// </summary>
    public static string[] GetFiles(string directoryPath, string searchPattern = "*.*", 
        SearchOption searchOption = SearchOption.AllDirectories)
    {
        ValidateDirectoryExists(directoryPath);
        
        try
        {
            return Directory.GetFiles(directoryPath, searchPattern, searchOption);
        }
        catch (Exception ex)
        {
            throw new ValidationException($"Error accessing directory: {directoryPath}", ex);
        }
    }
}

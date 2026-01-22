using System.Text.Json;
using System.Text.RegularExpressions;
using LivingDocGen.CLI.Models;

namespace LivingDocGen.CLI.Services;

/// <summary>
/// Service for reading and processing BDD configuration files
/// </summary>
public class ConfigurationService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    /// <summary>
    /// Loads configuration from a JSON file
    /// </summary>
    public static BddConfiguration? LoadConfiguration(string configPath)
    {
        if (!File.Exists(configPath))
        {
            return null;
        }

        try
        {
            var jsonContent = File.ReadAllText(configPath);
            
            // Expand environment variables
            jsonContent = ExpandEnvironmentVariables(jsonContent);

            var config = JsonSerializer.Deserialize<BddConfiguration>(jsonContent, JsonOptions);
            
            if (config != null)
            {
                // Resolve relative paths to absolute paths
                ResolveRelativePaths(config, Path.GetDirectoryName(configPath)!);
            }

            return config;
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"⚠️  Warning: Failed to parse config file: {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️  Warning: Error reading config file: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Finds configuration file in standard locations
    /// </summary>
    public static string? FindConfigurationFile(string? searchDirectory = null)
    {
        var directory = searchDirectory ?? Directory.GetCurrentDirectory();
        
        // Check for both bdd-livingdoc.json and .bdd-livingdoc.json
        var configFiles = new[]
        {
            Path.Combine(directory, "bdd-livingdoc.json"),
            Path.Combine(directory, ".bdd-livingdoc.json")
        };

        foreach (var configFile in configFiles)
        {
            if (File.Exists(configFile))
            {
                return configFile;
            }
        }

        return null;
    }

    /// <summary>
    /// Expands environment variables in format ${VAR_NAME}
    /// </summary>
    private static string ExpandEnvironmentVariables(string input)
    {
        // Match ${VAR_NAME} pattern
        var pattern = @"\$\{([^}]+)\}";
        
        return Regex.Replace(input, pattern, match =>
        {
            var varName = match.Groups[1].Value;
            var value = Environment.GetEnvironmentVariable(varName);
            
            if (value == null)
            {
                Console.WriteLine($"⚠️  Warning: Environment variable '{varName}' not found");
                return match.Value; // Keep original if not found
            }
            
            return value;
        });
    }

    /// <summary>
    /// Resolves relative paths to absolute paths based on config file location
    /// </summary>
    private static void ResolveRelativePaths(BddConfiguration config, string baseDirectory)
    {
        if (config.Paths == null) return;

        if (!string.IsNullOrEmpty(config.Paths.Features))
        {
            config.Paths.Features = ResolvePathIfRelative(config.Paths.Features, baseDirectory);
        }

        if (!string.IsNullOrEmpty(config.Paths.TestResults))
        {
            config.Paths.TestResults = ResolvePathIfRelative(config.Paths.TestResults, baseDirectory);
        }

        if (!string.IsNullOrEmpty(config.Paths.Output))
        {
            config.Paths.Output = ResolvePathIfRelative(config.Paths.Output, baseDirectory);
        }
    }

    /// <summary>
    /// Resolves a path to absolute if it's relative
    /// </summary>
    private static string ResolvePathIfRelative(string path, string baseDirectory)
    {
        if (Path.IsPathRooted(path))
        {
            return path; // Already absolute
        }

        // Combine with base directory and normalize
        var absolutePath = Path.Combine(baseDirectory, path);
        return Path.GetFullPath(absolutePath);
    }

    /// <summary>
    /// Validates configuration paths exist and are accessible
    /// </summary>
    public static void ValidateConfiguration(string featuresPath, string? testResultsPath, string outputPath)
    {
        // Validate features path
        if (string.IsNullOrWhiteSpace(featuresPath))
        {
            throw new Core.Exceptions.ConfigurationException("Features path cannot be empty");
        }

        if (!File.Exists(featuresPath) && !Directory.Exists(featuresPath))
        {
            throw new Core.Exceptions.ConfigurationException($"Features path not found: {featuresPath}");
        }

        // Validate test results path if provided
        if (!string.IsNullOrWhiteSpace(testResultsPath))
        {
            if (!File.Exists(testResultsPath) && !Directory.Exists(testResultsPath))
            {
                throw new Core.Exceptions.ConfigurationException($"Test results path not found: {testResultsPath}");
            }
        }

        // Validate output path
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            throw new Core.Exceptions.ConfigurationException("Output path cannot be empty");
        }

        // Ensure output directory exists or can be created
        var outputDir = Path.GetDirectoryName(Path.GetFullPath(outputPath));
        if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
        {
            try
            {
                Directory.CreateDirectory(outputDir);
            }
            catch (Exception ex)
            {
                throw new Core.Exceptions.ConfigurationException(
                    $"Cannot create output directory: {outputDir}", ex);
            }
        }
    }

    /// <summary>
    /// Merges configuration with command-line arguments
    /// Priority: CLI args > Config file > Defaults
    /// </summary>
    public static (string features, string? testResults, string output, string? title, string color, string theme, bool verbose) 
        MergeConfiguration(
            BddConfiguration? config,
            string? cliFeatures,
            string? cliTestResults,
            string? cliOutput,
            string? cliTitle,
            string? cliColor,
            string? cliTheme,
            bool? cliVerbose)
    {
        // Defaults
        var features = cliFeatures ?? config?.Paths?.Features ?? "./Features";
        var testResults = cliTestResults ?? config?.Paths?.TestResults;
        var output = cliOutput ?? config?.Paths?.Output ?? "./living-documentation.html";
        var title = cliTitle ?? config?.Documentation?.Title;
        var color = cliColor ?? config?.Documentation?.PrimaryColor ?? "#6366f1";
        var theme = cliTheme ?? config?.Documentation?.Theme ?? "purple";
        var verbose = cliVerbose ?? config?.Advanced?.Verbose ?? false;

        return (features, testResults, output, title, color, theme, verbose);
    }
}

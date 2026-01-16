using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LivingDocGen.Generator.Models;

public record ThemeConfig
{
    // Display
    public string DisplayName { get; init; } = string.Empty;
    
    // Primary Colors
    public string PrimaryColor { get; init; } = string.Empty;
    public string PrimaryGradient { get; init; } = string.Empty;
    
    // Semantic Colors
    public string SuccessColor { get; init; } = string.Empty;
    public string DangerColor { get; init; } = string.Empty;
    public string WarningColor { get; init; } = string.Empty;
    public string InfoColor { get; init; } = string.Empty;
    
    // Backgrounds
    public string BgColor { get; init; } = string.Empty;
    public string CardBg { get; init; } = string.Empty;
    public string HoverBg { get; init; } = string.Empty;
    
    // Text
    public string TextColor { get; init; } = string.Empty;
    public string TextSecondary { get; init; } = string.Empty;
    
    // Borders
    public string BorderColor { get; init; } = string.Empty;
    
    // Interactive Elements
    public string AccentColor { get; init; } = string.Empty;
    public string FocusRing { get; init; } = string.Empty;
    
    // Depth & Code
    public string ShadowColor { get; init; } = string.Empty;
    public string CodeBg { get; init; } = string.Empty;

    public string ToCssVariables()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"    --primary-color: {PrimaryColor};");
        sb.AppendLine($"    --primary-gradient: {PrimaryGradient};");
        sb.AppendLine($"    --success-color: {SuccessColor};");
        sb.AppendLine($"    --danger-color: {DangerColor};");
        sb.AppendLine($"    --warning-color: {WarningColor};");
        sb.AppendLine($"    --info-color: {InfoColor};");
        sb.AppendLine($"    --bg-color: {BgColor};");
        sb.AppendLine($"    --card-bg: {CardBg};");
        sb.AppendLine($"    --hover-bg: {HoverBg};");
        sb.AppendLine($"    --text-color: {TextColor};");
        sb.AppendLine($"    --text-secondary: {TextSecondary};");
        sb.AppendLine($"    --border-color: {BorderColor};");
        sb.AppendLine($"    --accent-color: {AccentColor};");
        sb.AppendLine($"    --focus-ring: {FocusRing};");
        sb.AppendLine($"    --shadow-color: {ShadowColor};");
        sb.AppendLine($"    --code-bg: {CodeBg};");
        return sb.ToString().TrimEnd();
    }

    public static readonly Dictionary<string, ThemeConfig> Themes = new()
    {
        ["purple"] = new ThemeConfig
        {
            DisplayName = "Purple (Default)",
            PrimaryColor = "#7c3aed",
            PrimaryGradient = "linear-gradient(135deg, #8b5cf6 0%, #6d28d9 100%)",
            SuccessColor = "#059669",
            DangerColor = "#dc2626",
            WarningColor = "#d97706",
            InfoColor = "#0284c7",
            BgColor = "#faf5ff",
            CardBg = "#ffffff",
            TextColor = "#1e1b4b",
            TextSecondary = "#6b7280",
            BorderColor = "#e9d5ff",
            HoverBg = "#f3e8ff",
            AccentColor = "#a78bfa",
            FocusRing = "#8b5cf6",
            ShadowColor = "rgba(124, 58, 237, 0.1)",
            CodeBg = "#f5f3ff"
        },
        ["blue"] = new ThemeConfig
        {
            DisplayName = "Ocean Blue",
            PrimaryColor = "#0369a1",
            PrimaryGradient = "linear-gradient(135deg, #0ea5e9 0%, #0369a1 100%)",
            SuccessColor = "#059669",
            DangerColor = "#dc2626",
            WarningColor = "#d97706",
            InfoColor = "#06b6d4",
            BgColor = "#f0f9ff",
            CardBg = "#ffffff",
            TextColor = "#0c4a6e",
            TextSecondary = "#64748b",
            BorderColor = "#bae6fd",
            HoverBg = "#e0f2fe",
            AccentColor = "#0ea5e9",
            FocusRing = "#0369a1",
            ShadowColor = "rgba(3, 105, 161, 0.1)",
            CodeBg = "#f0f9ff"
        },
        ["green"] = new ThemeConfig
        {
            DisplayName = "Forest Green",
            PrimaryColor = "#047857",
            PrimaryGradient = "linear-gradient(135deg, #10b981 0%, #047857 100%)",
            SuccessColor = "#16a34a",
            DangerColor = "#dc2626",
            WarningColor = "#d97706",
            InfoColor = "#0891b2",
            BgColor = "#f0fdf4",
            CardBg = "#ffffff",
            TextColor = "#064e3b",
            TextSecondary = "#65a30d",
            BorderColor = "#bbf7d0",
            HoverBg = "#dcfce7",
            AccentColor = "#10b981",
            FocusRing = "#047857",
            ShadowColor = "rgba(4, 120, 87, 0.1)",
            CodeBg = "#f0fdf4"
        },
        ["dark"] = new ThemeConfig
        {
            DisplayName = "Dark Mode",
            PrimaryColor = "#a78bfa",
            PrimaryGradient = "linear-gradient(135deg, #a78bfa 0%, #8b5cf6 100%)",
            SuccessColor = "#34d399",
            DangerColor = "#f87171",
            WarningColor = "#fbbf24",
            InfoColor = "#60a5fa",
            BgColor = "#0f172a",
            CardBg = "#1e293b",
            TextColor = "#f1f5f9",
            TextSecondary = "#94a3b8",
            BorderColor = "#334155",
            HoverBg = "#293548",
            AccentColor = "#c4b5fd",
            FocusRing = "#a78bfa",
            ShadowColor = "rgba(0, 0, 0, 0.3)",
            CodeBg = "#0f172a"
        },
        ["light"] = new ThemeConfig
        {
            DisplayName = "Clean Light",
            PrimaryColor = "#4338ca",
            PrimaryGradient = "linear-gradient(135deg, #6366f1 0%, #4338ca 100%)",
            SuccessColor = "#16a34a",
            DangerColor = "#dc2626",
            WarningColor = "#ea580c",
            InfoColor = "#2563eb",
            BgColor = "#ffffff",
            CardBg = "#f8fafc",
            TextColor = "#0f172a",
            TextSecondary = "#64748b",
            BorderColor = "#cbd5e1",
            HoverBg = "#f1f5f9",
            AccentColor = "#6366f1",
            FocusRing = "#4338ca",
            ShadowColor = "rgba(15, 23, 42, 0.08)",
            CodeBg = "#f8fafc"
        },
        ["pickles"] = new ThemeConfig
        {
            DisplayName = "Pickles Classic",
            PrimaryColor = "#d97706",
            PrimaryGradient = "linear-gradient(135deg, #f59e0b 0%, #b45309 100%)",
            SuccessColor = "#16a34a",
            DangerColor = "#dc2626",
            WarningColor = "#ea580c",
            InfoColor = "#0891b2",
            BgColor = "#fffbeb",
            CardBg = "#ffffff",
            TextColor = "#78350f",
            TextSecondary = "#92400e",
            BorderColor = "#fde68a",
            HoverBg = "#fef3c7",
            AccentColor = "#f59e0b",
            FocusRing = "#d97706",
            ShadowColor = "rgba(217, 119, 6, 0.1)",
            CodeBg = "#fffbeb"
        }
    };

    public static ThemeConfig GetTheme(string themeName)
    {
        return Themes.TryGetValue(themeName.ToLower(), out var theme) 
            ? theme 
            : Themes["purple"];
    }

    public static string[] GetAvailableThemes()
    {
        return Themes.Keys.ToArray();
    }
}

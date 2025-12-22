namespace LivingDocGen.Generator.Models;

public class ThemeConfig
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string PrimaryColor { get; set; } = string.Empty;
    public string PrimaryGradient { get; set; } = string.Empty;
    public string SuccessColor { get; set; } = string.Empty;
    public string DangerColor { get; set; } = string.Empty;
    public string WarningColor { get; set; } = string.Empty;
    public string InfoColor { get; set; } = string.Empty;
    public string BgColor { get; set; } = string.Empty;
    public string CardBg { get; set; } = string.Empty;
    public string TextColor { get; set; } = string.Empty;
    public string TextSecondary { get; set; } = string.Empty;
    public string BorderColor { get; set; } = string.Empty;
    public string HoverBg { get; set; } = string.Empty;

    public static readonly Dictionary<string, ThemeConfig> Themes = new()
    {
        ["purple"] = new ThemeConfig
        {
            Name = "purple",
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
            HoverBg = "#f3e8ff"
        },
        ["blue"] = new ThemeConfig
        {
            Name = "blue",
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
            HoverBg = "#e0f2fe"
        },
        ["green"] = new ThemeConfig
        {
            Name = "green",
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
            HoverBg = "#dcfce7"
        },
        ["dark"] = new ThemeConfig
        {
            Name = "dark",
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
            HoverBg = "#293548"
        },
        ["light"] = new ThemeConfig
        {
            Name = "light",
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
            HoverBg = "#f1f5f9"
        },
        ["pickles"] = new ThemeConfig
        {
            Name = "pickles",
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
            HoverBg = "#fef3c7"
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

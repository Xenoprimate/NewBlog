namespace BenBowenBlog.Web.Theming;

public enum ThemeMode
{
    System,
    Light,
    Dark
}

// Drawn behind the page by app.css (.pattern-{name}).
public enum BackgroundPattern
{
    None,
    Waves,
    Flecks,
    Lines,
    Grid
}

public sealed record ThemeSelection(BlogTheme Theme, ThemeMode Mode, bool IsDarkMode, BackgroundPattern Pattern);

// The reader's theme choices live in cookies, set by ThemeEndpoints and read by MainLayout on every request.
public static class ThemePreferences
{
    public const string PaletteCookie = "theme";
    public const string ModeCookie = "theme-mode";
    public const string PatternCookie = "theme-pattern";

    // Chromium browsers send this client hint once asked to via Accept-CH (see ThemeEndpoints); others never do, so
    // "System" falls back to light there.
    public const string ColorSchemeHint = "Sec-CH-Prefers-Color-Scheme";

    public static ThemeSelection Resolve(HttpContext? context)
    {
        var theme = BlogThemes.Get(context?.Request.Cookies[PaletteCookie]);
        var mode = Enum.TryParse<ThemeMode>(context?.Request.Cookies[ModeCookie], ignoreCase: true, out var parsed) ? parsed : ThemeMode.System;

        var isDark = mode switch
        {
            ThemeMode.Dark => true,
            ThemeMode.Light => false,
            _ => string.Equals(context?.Request.Headers[ColorSchemeHint].ToString(), "dark", StringComparison.OrdinalIgnoreCase)
        };

        // Until the reader picks a pattern, each palette uses its own.
        var pattern = Enum.TryParse<BackgroundPattern>(context?.Request.Cookies[PatternCookie], ignoreCase: true, out var chosen)
            ? chosen
            : theme.DefaultPattern;

        return new ThemeSelection(theme, mode, isDark, pattern);
    }
}

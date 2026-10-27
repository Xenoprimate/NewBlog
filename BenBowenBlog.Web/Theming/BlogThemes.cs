using MudBlazor;

namespace BenBowenBlog.Web.Theming;

// Where each palette colour goes. Text and accent colours are adjusted for contrast by ThemeBuilder, so these are
// starting points rather than exact values.
public sealed record PaletteSpec(
	string Key,
	string Name,
	string AppBar,
	string AppBarText,
	string Primary,
	string Secondary,
	string Tertiary,
	string BackgroundSeed,
	string TextSeed,
	string[] Swatches,
	BackgroundPattern DefaultPattern
);

public sealed record BlogTheme(string Key, string Name, MudTheme Theme, string[] Swatches, BackgroundPattern DefaultPattern);

public static class BlogThemes {
	public const string DefaultKey = "lagoon";

	static readonly PaletteSpec[] Specs = [
		// https://coolors.co/bce784-5dd39e-348aa7-525174-513b56
		new("lagoon", "Lagoon", "#513b56", ColorMath.White, "#348aa7", "#5dd39e", "#bce784", "#bce784", "#525174",
			["#513b56", "#348aa7", "#5dd39e"], BackgroundPattern.Waves),
		// https://coolors.co/989788-51344d-6f5060-a78682-e7ebc5
		new("heather", "Heather", "#51344d", ColorMath.White, "#6f5060", "#a78682", "#989788", "#e7ebc5", "#51344d",
			["#51344d", "#a78682", "#e7ebc5"], BackgroundPattern.Flecks),
		// https://coolors.co/c6878f-b79d94-969696-67697c-253d5b
		new("dusk", "Dusk", "#253d5b", ColorMath.White, "#c6878f", "#67697c", "#b79d94", "#b79d94", "#253d5b",
			["#253d5b", "#c6878f", "#b79d94"], BackgroundPattern.Lines),
		// https://coolors.co/343434-2f3061-c3f73a-5f5980-dfdfdf
		new("neon", "Neon", "#343434", "#c3f73a", "#2f3061", "#5f5980", "#c3f73a", "#dfdfdf", "#343434",
			["#343434", "#2f3061", "#c3f73a"], BackgroundPattern.Grid)
	];

	public static IReadOnlyList<BlogTheme> All { get; } = Specs.Select(spec => new BlogTheme(spec.Key, spec.Name, ThemeBuilder.Build(spec), spec.Swatches, spec.DefaultPattern)).ToArray();

	public static bool Contains(string? key) => All.Any(theme => theme.Key == key);

	public static BlogTheme Get(string? key) => All.FirstOrDefault(theme => theme.Key == key) ?? All.First(theme => theme.Key == DefaultKey);
}

// Builds a MudTheme from a palette: the light palette from the colours as given (adjusted for contrast), and the dark
// palette derived by moving lightness in HSL.
public static class ThemeBuilder {
	const double TextContrast = 4.5;

	public static MudTheme Build(PaletteSpec spec) => new() {
		PaletteLight = BuildLight(spec), PaletteDark = BuildDark(spec)
	};

	static PaletteLight BuildLight(PaletteSpec spec) {
		var tint = ColorMath.ToHsl(spec.BackgroundSeed);
		var background = ColorMath.ToHex(tint.WithS(Math.Min(tint.S, 0.35)).WithL(0.97));
		var backgroundGray = ColorMath.ToHex(tint.WithS(Math.Min(tint.S, 0.25)).WithL(0.93));
		const string surface = ColorMath.White;

		var textSeed = ColorMath.ToHsl(spec.TextSeed);
		var text = ColorMath.ToHex(textSeed.WithS(Math.Min(textSeed.S, 0.25)).WithL(Math.Min(textSeed.L, 0.2)));
		var textSecondary = ColorMath.EnsureContrast(ColorMath.ToHex(textSeed.WithS(Math.Min(textSeed.S, 0.2)).WithL(0.42)), background, TextContrast);

		// Accents must read on both the tinted page and white cards; the background is the darker of the two.
		var primary = ColorMath.EnsureContrast(spec.Primary, background, TextContrast);
		var secondary = ColorMath.EnsureContrast(spec.Secondary, background, TextContrast);
		var tertiary = ColorMath.EnsureContrast(spec.Tertiary, background, TextContrast);

		return new PaletteLight {
			Primary = primary, PrimaryContrastText = ColorMath.ReadableOn(primary), Secondary = secondary, SecondaryContrastText = ColorMath.ReadableOn(secondary),
			Tertiary = tertiary, TertiaryContrastText = ColorMath.ReadableOn(tertiary), AppbarBackground = spec.AppBar, AppbarText = spec.AppBarText,
			Background = background, BackgroundGray = backgroundGray, Surface = surface, DrawerBackground = surface,
			DrawerText = text, TextPrimary = text, TextSecondary = textSecondary, ActionDefault = ColorMath.Rgba(text, 0.6),
			LinesDefault = ColorMath.Rgba(text, 0.12), LinesInputs = ColorMath.Rgba(text, 0.42), TableLines = ColorMath.Rgba(text, 0.12), Divider = ColorMath.Rgba(text, 0.12),
			DividerLight = ColorMath.Rgba(text, 0.06)
		};
	}

	static PaletteDark BuildDark(PaletteSpec spec) {
		// Everything dark is tinted with the app bar's hue, so each theme keeps its character in dark mode.
		var baseHsl = ColorMath.ToHsl(spec.AppBar);
		var background = ColorMath.ToHex(baseHsl.WithS(Math.Min(baseHsl.S, 0.25)).WithL(0.09));
		var surface = ColorMath.ToHex(baseHsl.WithS(Math.Min(baseHsl.S, 0.25)).WithL(0.13));
		var appBar = ColorMath.ToHex(baseHsl.WithS(Math.Min(baseHsl.S, 0.3)).WithL(0.15));
		var backgroundGray = ColorMath.ToHex(baseHsl.WithS(Math.Min(baseHsl.S, 0.25)).WithL(0.17));

		var text = ColorMath.ToHex(baseHsl.WithS(Math.Min(baseHsl.S, 0.12)).WithL(0.92));
		var textSecondary = ColorMath.EnsureContrast(ColorMath.ToHex(baseHsl.WithS(Math.Min(baseHsl.S, 0.12)).WithL(0.7)), surface, TextContrast);

		// Accents are lightened until they read on cards (the lighter of the two dark backgrounds).
		var primary = DarkAccent(spec.Primary, surface);
		var secondary = DarkAccent(spec.Secondary, surface);
		var tertiary = DarkAccent(spec.Tertiary, surface);

		return new PaletteDark {
			Primary = primary, PrimaryContrastText = ColorMath.ReadableOn(primary), Secondary = secondary, SecondaryContrastText = ColorMath.ReadableOn(secondary),
			Tertiary = tertiary, TertiaryContrastText = ColorMath.ReadableOn(tertiary), AppbarBackground = appBar, AppbarText = spec.AppBarText,
			Background = background, BackgroundGray = backgroundGray, Surface = surface, DrawerBackground = surface,
			DrawerText = text, TextPrimary = text, TextSecondary = textSecondary, ActionDefault = ColorMath.Rgba(text, 0.7),
			LinesDefault = ColorMath.Rgba(text, 0.12), LinesInputs = ColorMath.Rgba(text, 0.3), TableLines = ColorMath.Rgba(text, 0.12), Divider = ColorMath.Rgba(text, 0.12),
			DividerLight = ColorMath.Rgba(text, 0.06)
		};
	}

	static string DarkAccent(string hex, string surface) {
		var hsl = ColorMath.ToHsl(hex);
		return ColorMath.EnsureContrast(ColorMath.ToHex(hsl.WithS(Math.Min(hsl.S, 0.7))), surface, TextContrast);
	}
}
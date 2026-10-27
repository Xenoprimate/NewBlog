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
	BackgroundPattern DefaultPattern,
	DarkPaletteSpec? Dark = null
);

// Exact dark-mode colours for a palette that shouldn't be derived (e.g. one recreating an existing dark site). Anything
// left null is derived as usual; accents are still lightened if needed to stay readable.
public sealed record DarkPaletteSpec(
	string? AppBar = null,
	string? AppBarText = null,
	string? Background = null,
	string? Surface = null,
	string? Text = null,
	string? Primary = null,
	string? Secondary = null,
	string? Tertiary = null
);

public sealed record BlogTheme(string Key, string Name, MudTheme Theme, string[] Swatches, BackgroundPattern DefaultPattern);

public static class BlogThemes {
	public const string DefaultKey = "classic";

	static readonly PaletteSpec[] Specs = [
		// Recreates benbowen.blog: a cream header with dusty-maroon title and rust links by day; by night the old site's warm
		// near-black page, light grey text and gold links.
		new("classic", "Classic", "#fff8ea", "#755353", "#875127", "#915e41", "#9fc659", "#f5ede1", "#332f29",
			["#211f1d", "#ebc17e", "#c26c29"], BackgroundPattern.Lines,
			new DarkPaletteSpec(AppBar: "#2b2723", AppBarText: "#dabca3", Background: "#171513", Surface: "#2a2724", Text: "#dededb",
				Primary: "#d0bda0", Secondary: "#b99966", Tertiary: "#9fc659")),

		// https://coolors.co/87bfc4-475b5a-8d8e8e-a3a9aa-bbbbbf
		new("harbour", "Harbour", "#475b5a", ColorMath.White, "#87bfc4", "#8d8e8e", "#475b5a", "#bbbbbf", "#475b5a",
			["#475b5a", "#87bfc4", "#bbbbbf"], BackgroundPattern.Lines),

		// https://coolors.co/67597a-544e61-6e8894-9ab87a-f8f991
		new("twilight", "Twilight", "#544e61", "#f8f991", "#67597a", "#6e8894", "#9ab87a", "#f8f991", "#544e61",
			["#544e61", "#9ab87a", "#f8f991"], BackgroundPattern.Lines),

		// https://coolors.co/443742-846c5b-cea07e-edd9a3-e2e8c0
		new("sandstone", "Sandstone", "#443742", "#edd9a3", "#846c5b", "#cea07e", "#443742", "#edd9a3", "#443742",
			["#443742", "#cea07e", "#edd9a3"], BackgroundPattern.Lines)
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
			Tertiary = tertiary, TertiaryContrastText = ColorMath.ReadableOn(tertiary), AppbarBackground = spec.AppBar, AppbarText = ColorMath.EnsureContrast(spec.AppBarText, spec.AppBar, TextContrast),
			Background = background, BackgroundGray = backgroundGray, Surface = surface, DrawerBackground = surface,
			DrawerText = text, TextPrimary = text, TextSecondary = textSecondary, ActionDefault = ColorMath.Rgba(text, 0.6),
			LinesDefault = ColorMath.Rgba(text, 0.12), LinesInputs = ColorMath.Rgba(text, 0.42), TableLines = ColorMath.Rgba(text, 0.12), Divider = ColorMath.Rgba(text, 0.12),
			DividerLight = ColorMath.Rgba(text, 0.06)
		};
	}

	static PaletteDark BuildDark(PaletteSpec spec) {
		var dark = spec.Dark ?? new DarkPaletteSpec();

		// Everything dark is tinted with the app bar's hue, so each theme keeps its character in dark mode, unless the palette
		// gives exact dark colours.
		var baseHsl = ColorMath.ToHsl(spec.AppBar);
		var background = dark.Background ?? ColorMath.ToHex(baseHsl.WithS(Math.Min(baseHsl.S, 0.25)).WithL(0.09));
		var surface = dark.Surface ?? ColorMath.ToHex(baseHsl.WithS(Math.Min(baseHsl.S, 0.25)).WithL(0.13));
		var appBar = dark.AppBar ?? ColorMath.ToHex(baseHsl.WithS(Math.Min(baseHsl.S, 0.3)).WithL(0.15));
		var appBarText = ColorMath.EnsureContrast(dark.AppBarText ?? spec.AppBarText, appBar, TextContrast);
		var surfaceHsl = ColorMath.ToHsl(surface);
		var backgroundGray = ColorMath.ToHex(surfaceHsl.WithL(Math.Min(1, surfaceHsl.L + 0.04)));

		var text = dark.Text ?? ColorMath.ToHex(baseHsl.WithS(Math.Min(baseHsl.S, 0.12)).WithL(0.92));
		var textHsl = ColorMath.ToHsl(text);
		var textSecondary = ColorMath.EnsureContrast(ColorMath.ToHex(textHsl.WithL(0.7)), surface, TextContrast);

		// Accents are lightened until they read on cards (the lighter of the two dark backgrounds).
		var primary = DarkAccent(dark.Primary ?? spec.Primary, surface);
		var secondary = DarkAccent(dark.Secondary ?? spec.Secondary, surface);
		var tertiary = DarkAccent(dark.Tertiary ?? spec.Tertiary, surface);

		return new PaletteDark {
			Primary = primary, PrimaryContrastText = ColorMath.ReadableOn(primary), Secondary = secondary, SecondaryContrastText = ColorMath.ReadableOn(secondary),
			Tertiary = tertiary, TertiaryContrastText = ColorMath.ReadableOn(tertiary), AppbarBackground = appBar, AppbarText = appBarText,
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
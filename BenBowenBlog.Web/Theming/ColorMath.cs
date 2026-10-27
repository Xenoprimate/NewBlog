using System.Globalization;

namespace BenBowenBlog.Web.Theming;

// HSL with H in degrees [0, 360) and S/L in [0, 1].
public readonly record struct Hsl(double H, double S, double L) {
	public Hsl WithS(double s) => this with {
		S = Math.Clamp(s, 0, 1)
	};

	public Hsl WithL(double l) => this with {
		L = Math.Clamp(l, 0, 1)
	};
}

public static class ColorMath {
	public const string White = "#ffffff";
	public const string Black = "#000000";

	public static Hsl ToHsl(string hex) {
		var (r, g, b) = ToRgb(hex);
		var max = Math.Max(r, Math.Max(g, b));
		var min = Math.Min(r, Math.Min(g, b));
		var l = (max + min) / 2;
		var delta = max - min;

		if (delta == 0) {
			return new Hsl(0, 0, l);
		}

		var s = l > 0.5 ? delta / (2 - max - min) : delta / (max + min);
		var h = max == r ? (g - b) / delta + (g < b ? 6 : 0)
			: max == g ? (b - r) / delta + 2
			: (r - g) / delta + 4;

		return new Hsl(h * 60, s, l);
	}

	public static string ToHex(Hsl hsl) {
		var (h, s, l) = (hsl.H / 360, hsl.S, hsl.L);

		if (s == 0) {
			return FromRgb(l, l, l);
		}

		var q = l < 0.5 ? l * (1 + s) : l + s - l * s;
		var p = 2 * l - q;

		return FromRgb(HueToRgb(p, q, h + 1d / 3), HueToRgb(p, q, h), HueToRgb(p, q, h - 1d / 3));
	}

	// WCAG 2 relative luminance and contrast ratio.
	public static double Luminance(string hex) {
		var (r, g, b) = ToRgb(hex);
		return 0.2126 * Linear(r) + 0.7152 * Linear(g) + 0.0722 * Linear(b);
	}

	public static double Contrast(string a, string b) {
		var (la, lb) = (Luminance(a), Luminance(b));
		return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
	}

	// Moves lightness away from the background (darker on light backgrounds, lighter on dark ones) until the colour
	// reaches the target contrast, keeping hue and saturation. Returns the colour unchanged if it already passes.
	public static string EnsureContrast(string hex, string background, double target) {
		var hsl = ToHsl(hex);
		var step = Luminance(background) > 0.5 ? -0.005 : 0.005;

		while (Contrast(ToHex(hsl), background) < target && hsl.L is > 0 and < 1) {
			hsl = hsl.WithL(hsl.L + step);
		}

		return ToHex(hsl);
	}

	// Black or white, whichever reads better on the given colour.
	public static string ReadableOn(string hex) => Contrast(hex, White) >= Contrast(hex, Black) ? White : Black;

	public static string Rgba(string hex, double alpha) {
		var (r, g, b) = ToRgb(hex);
		return String.Create(CultureInfo.InvariantCulture, $"rgba({(int) Math.Round(r * 255)},{(int) Math.Round(g * 255)},{(int) Math.Round(b * 255)},{alpha})");
	}

	static (double R, double G, double B) ToRgb(string hex) {
		var value = hex.TrimStart('#');
		return (Channel(value, 0), Channel(value, 2), Channel(value, 4));

		static double Channel(string value, int index) => Int32.Parse(value.AsSpan(index, 2), NumberStyles.HexNumber) / 255d;
	}

	static string FromRgb(double r, double g, double b) =>
		$"#{(int) Math.Round(r * 255):x2}{(int) Math.Round(g * 255):x2}{(int) Math.Round(b * 255):x2}";

	static double HueToRgb(double p, double q, double t) {
		if (t < 0) t += 1;
		if (t > 1) t -= 1;
		if (t < 1d / 6) return p + (q - p) * 6 * t;
		if (t < 1d / 2) return q;
		if (t < 2d / 3) return p + (q - p) * (2d / 3 - t) * 6;
		return p;
	}

	static double Linear(double channel) =>
		channel <= 0.03928 ? channel / 12.92 : Math.Pow((channel + 0.055) / 1.055, 2.4);
}
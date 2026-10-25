using System.Reflection;
using MudBlazor;

namespace BenBowenBlog.Web.Admin;

// Every MudBlazor icon, keyed "Style.Name" (e.g. "Filled.Code", "Brands.GitHub"). Built once by reflection over
// the Icons constants, so categories can store a short key instead of the SVG markup.
public sealed class IconCatalog
{
    public const string FallbackKey = "Filled.Category";

    private readonly Dictionary<string, string> icons = new(StringComparer.Ordinal);

    public IconCatalog()
    {
        foreach (var style in typeof(Icons.Material).GetNestedTypes())
        {
            AddConstants(style.Name, style);
        }

        AddConstants("Brands", typeof(Icons.Custom.Brands));

        Styles = icons.Keys.Select(StyleOf).Distinct().Order().ToArray();
    }

    public IReadOnlyList<string> Styles { get; }

    public bool Contains(string key) => icons.ContainsKey(key);

    public string Get(string? key) =>
        key is not null && icons.TryGetValue(key, out var svg) ? svg : icons[FallbackKey];

    public IEnumerable<string> Search(string style, string? text) =>
        icons.Keys
            .Where(key => StyleOf(key) == style
                && (string.IsNullOrWhiteSpace(text) || NameOf(key).Contains(text.Trim(), StringComparison.OrdinalIgnoreCase)))
            .Order();

    public static string StyleOf(string key) => key[..key.IndexOf('.')];

    public static string NameOf(string key) => key[(key.IndexOf('.') + 1)..];

    private void AddConstants(string style, Type type)
    {
        foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (field.IsLiteral && field.GetRawConstantValue() is string svg)
            {
                icons[$"{style}.{field.Name}"] = svg;
            }
        }
    }
}

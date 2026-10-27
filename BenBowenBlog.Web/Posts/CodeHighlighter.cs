using System.Collections.Concurrent;
using System.Net;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using TextMateSharp.Grammars;
using TextMateSharp.Registry;

namespace BenBowenBlog.Web.Posts;

public sealed record HighlightedCode(string Html, string? LanguageId, string? DisplayName);

// Server-side syntax highlighting with VS Code's TextMate grammars (TextMateSharp). Tokens become <span class="tok-*">
// elements coloured by app.css from the current theme, so no JavaScript or colour theme is involved here.
public static class CodeHighlighter {
	static readonly TimeSpan LineTimeLimit = TimeSpan.FromMilliseconds(50);

	static readonly RegistryOptions Options = new(ThemeName.DarkPlus);
	static readonly Registry Registry = new(Options);

	static readonly Dictionary<string, Language> LanguagesById = Options.GetAvailableLanguages()
		.GroupBy(language => language.Id, StringComparer.OrdinalIgnoreCase)
		.ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

	// Grammars are loaded on first use and kept; null means the language has no usable grammar.
	static readonly ConcurrentDictionary<string, IGrammar?> Grammars = new(StringComparer.OrdinalIgnoreCase);

	static readonly MemoryCache Cache = new(new MemoryCacheOptions {
		SizeLimit = 8 * 1024 * 1024
	});

	// Common code-fence names that aren't VS Code language ids or file extensions.
	static readonly Dictionary<string, string> FenceAliases = new(StringComparer.OrdinalIgnoreCase) {
		["c#"] = "csharp", ["cs"] = "csharp", ["f#"] = "fsharp", ["fs"] = "fsharp",
		["c++"] = "cpp", ["sh"] = "shellscript", ["bash"] = "shellscript", ["shell"] = "shellscript",
		["zsh"] = "shellscript", ["console"] = "shellscript", ["yml"] = "yaml", ["js"] = "javascript",
		["ts"] = "typescript", ["py"] = "python", ["rs"] = "rust", ["golang"] = "go",
		["ps1"] = "powershell", ["ps"] = "powershell", ["pwsh"] = "powershell", ["docker"] = "dockerfile",
		["cshtml"] = "razor", ["xaml"] = "xml", ["csproj"] = "xml", ["md"] = "markdown",
		["toml"] = "ini",
		// No GLSL grammar is bundled; HLSL is close enough for keywords, types and comments.
		["glsl"] = "hlsl"
	};

	// Labels where the grammar's own display name would mislead (the fence asked for something it only approximates).
	static readonly Dictionary<string, string> LabelOverrides = new(StringComparer.OrdinalIgnoreCase) {
		["glsl"] = "GLSL", ["toml"] = "TOML", ["console"] = "Console"
	};

	// Checked from the most specific scope outwards; the first scope with a matching rule decides the class.
	// A null class means "leave uncoloured" (operators would otherwise pick up the keyword colour).
	static readonly (string Scope, string? Class)[] ScopeRules = [
		("comment", "tok-comment"),
		("string", "tok-string"),
		("constant.character", "tok-string"),
		("keyword.operator", null),
		("keyword.control.directive", "tok-preproc"),
		("meta.preprocessor", "tok-preproc"),
		("keyword", "tok-keyword"),
		("storage", "tok-keyword"),
		("entity.name.type", "tok-type"),
		("entity.name.class", "tok-type"),
		("entity.name.namespace", "tok-type"),
		("entity.other.inherited-class", "tok-type"),
		("support.type", "tok-type"),
		("support.class", "tok-type"),
		("entity.name.function", "tok-function"),
		("support.function", "tok-function"),
		("constant.numeric", "tok-number"),
		("constant.language", "tok-number"),
		("entity.name.tag", "tok-tag"),
		("entity.other.attribute-name", "tok-attr"),
		("variable", "tok-var")
	];

	public static HighlightedCode Highlight(string code, string? fenceInfo) {
		var info = fenceInfo?.Trim();
		var language = Resolve(info);

		if (language is null) {
			return new HighlightedCode(Escape(code), null, String.IsNullOrEmpty(info) ? null : info);
		}

		var label = info is not null && LabelOverrides.TryGetValue(info, out var overridden)
			? overridden
			: language.Aliases?.FirstOrDefault() ?? language.Id;

		var cacheKey = language.Id + "\n" + code;

		if (!Cache.TryGetValue(cacheKey, out string? html) || html is null) {
			html = Grammars.GetOrAdd(language.Id, LoadGrammar) is { } grammar ? Tokenize(code, grammar) : Escape(code);
			Cache.Set(cacheKey, html, new MemoryCacheEntryOptions {
				Size = html.Length, SlidingExpiration = TimeSpan.FromHours(6)
			});
		}

		return new HighlightedCode(html, language.Id, label);
	}

	static Language? Resolve(string? info) {
		if (String.IsNullOrEmpty(info)) {
			return null;
		}

		var id = FenceAliases.GetValueOrDefault(info, info);

		return LanguagesById.TryGetValue(id, out var language) ? language : Options.GetLanguageByExtension("." + info);
	}

	static IGrammar? LoadGrammar(string languageId) {
		try {
			return Options.GetScopeByLanguageId(languageId) is { } scope ? Registry.LoadGrammar(scope) : null;
		}
		catch (Exception) {
			// A broken or missing grammar just means plain text for that language.
			return null;
		}
	}

	static string Tokenize(string code, IGrammar grammar) {
		var lines = code.Replace("\r\n", "\n").Split('\n');
		var html = new StringBuilder(code.Length * 2);

		try {
			// TextMate grammars aren't documented as thread-safe, so each grammar tokenizes one block at a time.
			lock (grammar) {
				IStateStack? state = null;

				for (var i = 0; i < lines.Length; i++) {
					var line = lines[i];
					var result = grammar.TokenizeLine(line, state, LineTimeLimit);
					state = result.RuleStack;
					var position = 0;

					foreach (var token in result.Tokens) {
						var start = Math.Clamp(token.StartIndex, position, line.Length);
						var end = Math.Clamp(token.EndIndex, start, line.Length);

						if (start > position) {
							html.Append(Escape(line[position..start]));
						}

						if (end > start) {
							AppendToken(html, line[start..end], ClassFor(token.Scopes));
						}

						position = end;
					}

					// A line that hit the time limit may not be fully covered by tokens.
					if (position < line.Length) {
						html.Append(Escape(line[position..]));
					}

					if (i < lines.Length - 1) {
						html.Append('\n');
					}
				}
			}

			return html.ToString();
		}
		catch (Exception) {
			return Escape(code);
		}
	}

	static void AppendToken(StringBuilder html, string text, string? cssClass) {
		if (cssClass is null) {
			html.Append(Escape(text));
			return;
		}

		html.Append("<span class=\"").Append(cssClass).Append("\">").Append(Escape(text)).Append("</span>");
	}

	static string? ClassFor(List<string> scopes) {
		for (var i = scopes.Count - 1; i >= 0; i--) {
			foreach (var (scope, cssClass) in ScopeRules) {
				if (scopes[i] == scope || scopes[i].StartsWith(scope + ".", StringComparison.Ordinal)) {
					return cssClass;
				}
			}
		}

		return null;
	}

	static string Escape(string text) => WebUtility.HtmlEncode(text);
}
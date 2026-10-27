using Markdig;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using System.Net;
using System.Text.RegularExpressions;
using Markdig.Syntax;

namespace BenBowenBlog.Web.Posts;

// Replaces Markdig's code block rendering with highlighted code plus a "View fullscreen" button. The fullscreen view uses
// the browser's Popover API (popovertarget), so it opens and closes (Close, Escape, clicking outside) without JavaScript.
public sealed class CodeBlockExtension : IMarkdownExtension {
	public void Setup(MarkdownPipelineBuilder pipeline) { }

	public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer) {
		if (renderer is HtmlRenderer html) {
			html.ObjectRenderers.ReplaceOrAdd<CodeBlockRenderer>(new HighlightedCodeBlockRenderer());
		}
	}
}

// Markdig pools and reuses renderers, so the counter keeps rising across renders: ids are unique within a page (all
// the popovers need), just not numbered from 1.
// Maths blocks are code blocks too, but Markdig registers their renderer ahead of this one, so they keep it.
public sealed partial class HighlightedCodeBlockRenderer : HtmlObjectRenderer<CodeBlock> {
	int count;

	protected override void Write(HtmlRenderer renderer, CodeBlock block) {
		var highlighted = CodeHighlighter.Highlight(block.Lines.ToString(), (block as FencedCodeBlock)?.Info);
		var id = $"codeblock-{Interlocked.Increment(ref count)}";
		var codeClass = highlighted.LanguageId is { } languageId ? $" class=\"language-{languageId}\"" : "";
		var label = highlighted.DisplayName is { } name ? WebUtility.HtmlEncode(name) : "";
		var title = TitleFrom((block as FencedCodeBlock)?.Arguments) is { } text ? WebUtility.HtmlEncode(text) : null;
		var titleHtml = title is null ? "" : $"<span class=\"code-block-title\"><span aria-hidden=\"true\">&bull;</span> &ldquo;{title}&rdquo;</span>";
		var code = $"<pre><code{codeClass}>{highlighted.Html}</code></pre>";

		renderer.EnsureLine();
		renderer.Write("<div class=\"code-block\">");
		renderer.Write(code);
		renderer.Write("<div class=\"code-block-footer\">");
		renderer.Write($"<button type=\"button\" class=\"code-block-expand\" popovertarget=\"{id}\">View Code Fullscreen</button>");
		renderer.Write(titleHtml);
		renderer.Write($"<span class=\"code-block-lang\">{label}</span>");
		renderer.Write("</div>");
		renderer.Write($"<div id=\"{id}\" class=\"code-popover\" popover>");
		renderer.Write("<div class=\"code-popover-header\">");
		renderer.Write($"<span class=\"code-popover-title\">{title ?? label}</span>");
		renderer.Write($"<button type=\"button\" class=\"code-popover-close\" popovertarget=\"{id}\" popovertargetaction=\"hide\">Close</button>");
		renderer.Write("</div>");
		renderer.Write(code);
		renderer.Write("</div>");
		renderer.WriteLine("</div>");
	}

	// The title comes from the rest of the fence line: ```csharp title="The Script" (or just ```csharp "The Script",
	// or unquoted text).
	static string? TitleFrom(string? arguments) {
		var text = arguments?.Trim();

		if (String.IsNullOrEmpty(text)) {
			return null;
		}

		var match = TitlePattern().Match(text);
		var title = match.Success ? match.Groups["title"].Value : text;

		return String.IsNullOrWhiteSpace(title) ? null : title.Trim();
	}

	[GeneratedRegex("""^(?:title\s*=\s*)?"(?<title>[^"]*)"$""")]
	private static partial Regex TitlePattern();
}
using System.Text;
using Markdig;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Microsoft.AspNetCore.Components;

namespace BenBowenBlog.Web.Posts;

public sealed record TocEntry(string Id, string Text, int Level);
public sealed record RenderedPost(MarkupString Html, IReadOnlyList<TocEntry> TableOfContents);

public static class PostMarkdown {
	const int TocMaxHeadingLevel = 3;

	// Raw HTML in posts is allowed (Markdig's default): only the admin-key holder can write posts.
	// UseAdvancedExtensions includes auto-identifiers, so every heading gets an id the table of contents can link to.
	static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
		.UseAdvancedExtensions()
		.Use<CodeBlockExtension>()
		.Use<FigureExtension>()
		.Build();

	public static MarkupString ToHtml(string markdown) => new(Markdown.ToHtml(markdown, Pipeline));

	// Renders a one-line snippet (e.g. a footer line) without the <p> wrapper, so it can sit inside other markup. Line
	// breaks are flattened first, so the snippet can never become several paragraphs.
	public static MarkupString ToInlineHtml(string markdown) {
		var document = Markdown.Parse(markdown.ReplaceLineEndings(" "), Pipeline);

		using var writer = new StringWriter();
		var renderer = new HtmlRenderer(writer) {
			ImplicitParagraph = true
		};
		Pipeline.Setup(renderer);
		renderer.Render(document);

		return new MarkupString(writer.ToString().Trim());
	}

	// Parses once and renders from the same document, so the table of contents' ids match the rendered headings.
	public static RenderedPost Render(string markdown) {
		var document = Markdown.Parse(markdown, Pipeline);

		var tableOfContents = document.Descendants<HeadingBlock>()
			.Where(heading => heading.Level <= TocMaxHeadingLevel)
			.Select(heading => (Heading: heading, Id: heading.GetAttributes().Id))
			.Where(entry => !String.IsNullOrEmpty(entry.Id))
			.Select(entry => new TocEntry(entry.Id!, PlainText(entry.Heading.Inline), entry.Heading.Level))
			.ToList();

		return new RenderedPost(new MarkupString(Markdown.ToHtml(document, Pipeline)), tableOfContents);
	}

	// The visible text of some inline Markdown (used for heading text in the table of contents and image alt text).
	internal static string PlainText(ContainerInline? inline) {
		var text = new StringBuilder();

		foreach (var node in inline?.Descendants() ?? []) {
			switch (node) {
				case LiteralInline literal:
					text.Append(literal.Content);
					break;
				case CodeInline code:
					text.Append(code.Content);
					break;
			}
		}

		return text.ToString();
	}
}
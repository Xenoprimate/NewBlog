using System.Net;
using Markdig;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace BenBowenBlog.Web.Posts;

// Image captions from plain Markdown: an image with a title, alone in its paragraph, becomes a <figure> with the title as
// its <figcaption>:  ![alt text](/media/x.png "Caption")
// (Markdig's own ^^^ figure syntax, for captions with formatting, still works and is styled the same.)
public sealed class FigureExtension : IMarkdownExtension {
	public void Setup(MarkdownPipelineBuilder pipeline) { }

	public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer) {
		if (renderer is HtmlRenderer html) {
			html.ObjectRenderers.ReplaceOrAdd<ParagraphRenderer>(new FigureParagraphRenderer());
		}
	}
}

public sealed class FigureParagraphRenderer : ParagraphRenderer {
	protected override void Write(HtmlRenderer renderer, ParagraphBlock paragraph) {
		if (CaptionedImage(paragraph) is not { } image) {
			base.Write(renderer, paragraph);
			return;
		}

		var src = WebUtility.HtmlEncode(image.GetDynamicUrl?.Invoke() ?? image.Url ?? "");
		var alt = WebUtility.HtmlEncode(PostMarkdown.PlainText(image));
		var caption = WebUtility.HtmlEncode(image.Title!.Trim());

		renderer.EnsureLine();
		renderer.Write($"<figure class=\"post-figure\"><img src=\"{src}\" alt=\"{alt}\" loading=\"lazy\" /><figcaption>{caption}</figcaption></figure>");
		renderer.WriteLine();
	}

	// The paragraph's only content (ignoring whitespace and line breaks) is a single image that has a title.
	static LinkInline? CaptionedImage(ParagraphBlock paragraph) {
		LinkInline? image = null;

		foreach (var inline in paragraph.Inline ?? []) {
			switch (inline) {
				case LineBreakInline:
				case LiteralInline literal when literal.Content.IsEmptyOrWhitespace():
					continue;
				case LinkInline {IsImage: true} link when image is null && !String.IsNullOrWhiteSpace(link.Title):
					image = link;
					break;
				default:
					return null;
			}
		}

		return image;
	}
}
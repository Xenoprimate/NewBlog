using System.Text;
using Markdig;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Microsoft.AspNetCore.Components;

namespace BenBowenBlog.Web.Posts;

public sealed record TocEntry(string Id, string Text, int Level);

public sealed record RenderedPost(MarkupString Html, IReadOnlyList<TocEntry> TableOfContents);

public static class PostMarkdown
{
    private const int TocMaxHeadingLevel = 3;

    // Raw HTML in posts is allowed (Markdig's default): only the admin-key holder can write posts.
    // UseAdvancedExtensions includes auto-identifiers, so every heading gets an id the table of contents can link to.
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    public static MarkupString ToHtml(string markdown) => new(Markdown.ToHtml(markdown, Pipeline));

    // Parses once and renders from the same document, so the table of contents' ids match the rendered headings.
    public static RenderedPost Render(string markdown)
    {
        var document = Markdown.Parse(markdown, Pipeline);

        var tableOfContents = document.Descendants<HeadingBlock>()
            .Where(heading => heading.Level <= TocMaxHeadingLevel)
            .Select(heading => (Heading: heading, Id: heading.GetAttributes().Id))
            .Where(entry => !string.IsNullOrEmpty(entry.Id))
            .Select(entry => new TocEntry(entry.Id!, PlainText(entry.Heading.Inline), entry.Heading.Level))
            .ToList();

        return new RenderedPost(new MarkupString(Markdown.ToHtml(document, Pipeline)), tableOfContents);
    }

    private static string PlainText(ContainerInline? inline)
    {
        var text = new StringBuilder();

        foreach (var node in inline?.Descendants() ?? [])
        {
            switch (node)
            {
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

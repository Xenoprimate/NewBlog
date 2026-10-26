namespace BenBowenBlog.Data;

// A correction noted against a post, shown on the post page between its details and its body.
public class Erratum
{
    public int Id { get; set; }

    public int PostId { get; set; }

    public DateOnly Date { get; set; }

    // Markdown.
    public required string Text { get; set; }
}

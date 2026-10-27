namespace BenBowenBlog.Data;

// One of the lines shown at random under the footer's copyright notice. Text is inline Markdown.
public class FooterLine {
	public int Id { get; set; }

	public required string Text { get; set; }

	public DateTimeOffset CreatedAt { get; set; }
}

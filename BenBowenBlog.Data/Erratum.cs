namespace BenBowenBlog.Data;

public class Erratum {
	public const int TextMaxLength = 2000;

	public int Id { get; set; }

	public int PostId { get; set; }

	public DateOnly Date { get; set; }

	public required string Text { get; set; }
}
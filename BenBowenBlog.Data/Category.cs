namespace BenBowenBlog.Data;

public class Category {
	public const int NameMaxLength = 100;
	public const int IconMaxLength = 100;

	public int Id { get; set; }

	public required string Name { get; set; }

	// A MudBlazor icon key such as "Filled.Code" (style + icon name), not the SVG markup itself.
	public required string Icon { get; set; }

	public List<Post> Posts { get; set; } = [];
}
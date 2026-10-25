namespace BenBowenBlog.Data;

public class Category
{
    public int Id { get; set; }

    public required string Name { get; set; }

    // A MudBlazor icon key such as "Filled.Code" (style + icon name), not the SVG markup itself.
    public required string Icon { get; set; }

    public List<Post> Posts { get; set; } = [];
}

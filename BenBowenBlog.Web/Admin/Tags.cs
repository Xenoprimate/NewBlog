namespace BenBowenBlog.Web.Admin;

public static class Tags
{
    // "Hello World, ASP.NET" -> ["hello-world", "asp-net"]: comma- or newline-separated, slugified, de-duplicated.
    public static List<string> Normalize(string? input) =>
        (input ?? "")
            .Split([',', '\n'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(Slugs.FromTitle)
            .Where(tag => tag.Length > 0)
            .Distinct()
            .ToList();
}

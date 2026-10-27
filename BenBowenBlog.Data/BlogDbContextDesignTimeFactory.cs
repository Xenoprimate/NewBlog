using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BenBowenBlog.Data;

// Used only by the EF Core tools (e.g. `dotnet ef migrations add`). At runtime Aspire supplies the real
// connection string, but the tools can't get it, so they use this placeholder instead. Creating migrations
// never opens a connection.
public class BlogDbContextDesignTimeFactory : IDesignTimeDbContextFactory<BlogDbContext> {
	public BlogDbContext CreateDbContext(string[] args) {
		var options = new DbContextOptionsBuilder<BlogDbContext>()
			.UseNpgsql("Host=localhost;Database=blogdb")
			.Options;

		return new BlogDbContext(options);
	}
}
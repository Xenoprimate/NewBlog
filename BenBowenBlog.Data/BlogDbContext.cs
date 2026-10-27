using Microsoft.EntityFrameworkCore;

namespace BenBowenBlog.Data;

public class BlogDbContext(DbContextOptions<BlogDbContext> options) : DbContext(options) {
	public DbSet<Post> Posts => Set<Post>();

	public DbSet<Category> Categories => Set<Category>();

	public DbSet<Erratum> Errata => Set<Erratum>();

	public DbSet<PostVisit> PostVisits => Set<PostVisit>();

	protected override void OnModelCreating(ModelBuilder modelBuilder) {
		modelBuilder.Entity<Post>(post => {
			post.Property(p => p.Slug).HasMaxLength(Post.SlugMaxLength);
			post.HasIndex(p => p.Slug).IsUnique();

			post.Property(p => p.Title).HasMaxLength(Post.TitleMaxLength);
			post.Property(p => p.Summary).HasMaxLength(Post.SummaryMaxLength);
			post.Property(p => p.Body).HasColumnType("text");

			post.HasIndex(p => p.PublishedAt);

			post.HasOne(p => p.Category)
				.WithMany(c => c.Posts)
				.HasForeignKey(p => p.CategoryId)
				.IsRequired()
				.OnDelete(DeleteBehavior.Restrict);

			post.HasIndex(p => p.Tags).HasMethod("gin");

			post.Property(p => p.CoverImage).HasMaxLength(Post.CoverImageNameMaxLength);
			post.Property(p => p.CoverImageAlt).HasMaxLength(Post.CoverImageAltMaxLength);

			post.HasOne(p => p.PreviousPost)
				.WithMany()
				.HasForeignKey(p => p.PreviousPostId)
				.OnDelete(DeleteBehavior.SetNull);
			post.HasIndex(p => p.PreviousPostId).IsUnique();

			post.HasMany(p => p.Errata)
				.WithOne()
				.HasForeignKey(e => e.PostId)
				.OnDelete(DeleteBehavior.Cascade);
		});

		modelBuilder.Entity<Erratum>(erratum => {
			erratum.Property(e => e.Text).HasMaxLength(Erratum.TextMaxLength);
		});

		modelBuilder.Entity<PostVisit>(visit => {
			visit.HasOne<Post>()
				.WithMany()
				.HasForeignKey(v => v.PostId)
				.OnDelete(DeleteBehavior.Cascade);

			visit.HasIndex(v => new {
				v.PostId, v.Date, v.VisitorHash
			}).IsUnique();

			visit.Property(v => v.VisitorHash).HasMaxLength(PostVisit.VisitorHashLength);
			visit.Property(v => v.Source).HasMaxLength(PostVisit.SourceMaxLength);
		});

		modelBuilder.Entity<Category>(category => {
			category.Property(c => c.Name).HasMaxLength(Category.NameMaxLength);
			category.HasIndex(c => c.Name).IsUnique();

			category.Property(c => c.Icon).HasMaxLength(Category.IconMaxLength);
		});
	}
}
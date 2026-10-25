using Microsoft.EntityFrameworkCore;

namespace BenBowenBlog.Data;

public class BlogDbContext(DbContextOptions<BlogDbContext> options) : DbContext(options)
{
    public DbSet<Post> Posts => Set<Post>();

    public DbSet<Category> Categories => Set<Category>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Post>(post =>
        {
            post.Property(p => p.Slug).HasMaxLength(PostLimits.SlugMaxLength);
            post.HasIndex(p => p.Slug).IsUnique();

            post.Property(p => p.Title).HasMaxLength(PostLimits.TitleMaxLength);
            post.Property(p => p.Summary).HasMaxLength(PostLimits.SummaryMaxLength);
            post.Property(p => p.Body).HasColumnType("text");

            post.HasIndex(p => p.PublishedAt);

            // A category that still has posts can't be deleted.
            post.HasOne(p => p.Category)
                .WithMany(c => c.Posts)
                .HasForeignKey(p => p.CategoryId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            // Stored as a Postgres text[]; the GIN index makes "posts with tag X" queries fast.
            post.HasIndex(p => p.Tags).HasMethod("gin");

            post.Property(p => p.CoverImage).HasMaxLength(PostLimits.CoverImageNameMaxLength);
            post.Property(p => p.CoverImageAlt).HasMaxLength(PostLimits.CoverImageAltMaxLength);

            // Series chains: unique, so at most one post can follow any given post (Postgres allows many nulls).
            // Deleting a post just unlinks whichever post followed it.
            post.HasOne(p => p.PreviousPost)
                .WithMany()
                .HasForeignKey(p => p.PreviousPostId)
                .OnDelete(DeleteBehavior.SetNull);
            post.HasIndex(p => p.PreviousPostId).IsUnique();
        });

        modelBuilder.Entity<Category>(category =>
        {
            category.Property(c => c.Name).HasMaxLength(CategoryLimits.NameMaxLength);
            category.HasIndex(c => c.Name).IsUnique();

            category.Property(c => c.Icon).HasMaxLength(CategoryLimits.IconMaxLength);
        });
    }
}

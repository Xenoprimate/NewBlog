using System.Text;
using System.Xml;
using BenBowenBlog.Data;
using Microsoft.EntityFrameworkCore;

namespace BenBowenBlog.Web.Rss;

public static class RssEndpointExtensions {
	const int MaxItems = 20;
	const int ExcerptLength = 300;

	// RSS 2.0 feed of the newest published posts, linked from the menu's "Subscribe" item.
	public static IEndpointRouteBuilder MapRssEndpoints(this IEndpointRouteBuilder endpoints) {
		endpoints.MapGet("/feed.xml", async (BlogDbContext db, HttpContext context) => {
				var posts = await db.Posts
					.AsNoTracking()
					.Where(p => p.PublishedAt != null)
					.OrderByDescending(p => p.PublishedAt)
					.Take(MaxItems)
					.Select(p => new FeedItem(p.Slug, p.Title, p.Summary, p.Body, p.PublishedAt!.Value, p.Category!.Name, p.Tags))
					.ToListAsync(context.RequestAborted);

				// Behind a proxy (e.g. Azure App Service), set ASPNETCORE_FORWARDEDHEADERS_ENABLED=true so this is https.
				var site = $"{context.Request.Scheme}://{context.Request.Host}";

				using var buffer = new MemoryStream();
				using (var xml = XmlWriter.Create(buffer, new XmlWriterSettings { Encoding = new UTF8Encoding(false), Indent = true })) {
					xml.WriteStartDocument();
					xml.WriteStartElement("rss");
					xml.WriteAttributeString("version", "2.0");
					xml.WriteAttributeString("xmlns", "atom", null, "http://www.w3.org/2005/Atom");

					xml.WriteStartElement("channel");
					xml.WriteElementString("title", "BenBowenBlog");
					xml.WriteElementString("link", site + "/");
					xml.WriteElementString("description", "Posts from BenBowenBlog");
					xml.WriteElementString("language", "en");
					xml.WriteElementString("lastBuildDate", (posts.Count > 0 ? posts[0].PublishedAt : DateTimeOffset.UtcNow).UtcDateTime.ToString("r"));

					xml.WriteStartElement("atom", "link", "http://www.w3.org/2005/Atom");
					xml.WriteAttributeString("href", site + "/feed.xml");
					xml.WriteAttributeString("rel", "self");
					xml.WriteAttributeString("type", "application/rss+xml");
					xml.WriteEndElement();

					foreach (var post in posts) {
						var url = $"{site}/post/{post.Slug}";

						xml.WriteStartElement("item");
						xml.WriteElementString("title", post.Title);
						xml.WriteElementString("link", url);
						xml.WriteStartElement("guid");
						xml.WriteAttributeString("isPermaLink", "true");
						xml.WriteString(url);
						xml.WriteEndElement();
						xml.WriteElementString("pubDate", post.PublishedAt.UtcDateTime.ToString("r"));
						xml.WriteElementString("description", post.Summary ?? Excerpt(post.Body));
						xml.WriteElementString("category", post.Category);

						foreach (var tag in post.Tags) {
							xml.WriteElementString("category", tag);
						}

						xml.WriteEndElement();
					}

					xml.WriteEndElement();
					xml.WriteEndElement();
					xml.WriteEndDocument();
				}

				return Results.Bytes(buffer.ToArray(), "application/rss+xml; charset=utf-8");
			})
			.CacheOutput(policy => policy.Expire(TimeSpan.FromMinutes(5)));

		return endpoints;
	}

	static string Excerpt(string markdown) {
		var text = String.Join(' ', Markdig.Markdown.ToPlainText(markdown).Split((char[]?) null, StringSplitOptions.RemoveEmptyEntries));
		return text.Length <= ExcerptLength ? text : text[..ExcerptLength].TrimEnd() + "…";
	}

	sealed record FeedItem(string Slug, string Title, string? Summary, string Body, DateTimeOffset PublishedAt, string Category, List<string> Tags);
}
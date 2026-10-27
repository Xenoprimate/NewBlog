namespace BenBowenBlog.Data;

// One visitor's visits to one post on one day. Visitors are identified only by a keyed hash of (date, IP, user agent)
// that changes daily; no IP address or user agent is ever stored.
public class PostVisit {
	public const int VisitorHashLength = 16;
	public const int SourceMaxLength = 200;

	public int Id { get; set; }

	public int PostId { get; set; }

	public DateOnly Date { get; set; }

	public required byte[] VisitorHash { get; set; }

	// Page views by this visitor on this post that day.
	public int Views { get; set; }

	// Where the first view that day came from: a host, a campaign tag (?ref= / ?utm_source=), "(direct)" or "(internal)".
	public required string Source { get; set; }
}
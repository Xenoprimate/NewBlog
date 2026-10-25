namespace BenBowenBlog.Data;

public static class PostLimits
{
    public const int SlugMaxLength = 200;
    public const int TitleMaxLength = 300;
    public const int SummaryMaxLength = 1000;

    public const int MaxTags = 10;
    public const int TagMaxLength = 50;

    public const int CoverImageNameMaxLength = 100;
    public const int CoverImageAltMaxLength = 300;
    public const long CoverImageMaxBytes = 10 * 1024 * 1024;
}

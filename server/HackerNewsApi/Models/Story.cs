namespace HackerNewsApi.Models;

public class Story
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Url { get; set; }
    public string By { get; set; } = string.Empty;
    public long Time { get; set; }
    public int Score { get; set; }
    public int? Descendants { get; set; }
    public string Type { get; set; } = string.Empty;
}

public class PaginatedStories
{
    public List<Story> Stories { get; set; } = new();
    public int TotalCount { get; set; }
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public int PageSize { get; set; }
}

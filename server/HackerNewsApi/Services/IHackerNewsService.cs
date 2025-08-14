using HackerNewsApi.Models;

namespace HackerNewsApi.Services;

public interface IHackerNewsService
{
    Task<PaginatedStories> GetNewStoriesAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<Story?> GetStoryByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<List<Story>> SearchStoriesAsync(string query, int maxResults = 100, CancellationToken cancellationToken = default);
}

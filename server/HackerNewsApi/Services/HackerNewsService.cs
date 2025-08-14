using System.Text.Json;
using HackerNewsApi.Models;
using Microsoft.Extensions.Caching.Memory;

namespace HackerNewsApi.Services;

public class HackerNewsService : IHackerNewsService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<HackerNewsService> _logger;

    private const string BaseUrl = "https://hacker-news.firebaseio.com/v0";
    private const string NewStoriesCacheKey = "new_stories";
    private const int NewStoriesCacheExpirationMinutes = 5;
    private const int StoryCacheExpirationMinutes = 30;
    private const int SearchBatchMultiplier = 2;

    public HackerNewsService(HttpClient httpClient, IMemoryCache cache, ILogger<HackerNewsService> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
    }

    public async Task<PaginatedStories> GetNewStoriesAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching new stories for page {Page} with page size {PageSize}", page, pageSize);

            var storyIds = await GetNewStoryIdsAsync(cancellationToken);

            var totalCount = storyIds.Count;
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var startIndex = (page - 1) * pageSize;
            var pageStoryIds = storyIds.Skip(startIndex).Take(pageSize).ToList();

            var stories = new List<Story>();
            var tasks = pageStoryIds.Select(id => GetStoryByIdAsync(id, cancellationToken));
            var storyResults = await Task.WhenAll(tasks);

            stories.AddRange(storyResults.Where(story => story != null).Cast<Story>());

            return new PaginatedStories
            {
                Stories = stories,
                TotalCount = totalCount,
                CurrentPage = page,
                TotalPages = totalPages,
                PageSize = pageSize
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching new stories");
            throw new InvalidOperationException("Failed to fetch stories from Hacker News API", ex);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Request timeout fetching new stories");
            throw new TimeoutException("Request timeout while fetching stories", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing error fetching new stories");
            throw new InvalidOperationException("Failed to parse response from Hacker News API", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching new stories");
            throw;
        }
    }

    public async Task<Story?> GetStoryByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = $"story_{id}";

            if (_cache.TryGetValue(cacheKey, out Story? cachedStory))
            {
                return cachedStory;
            }

            var response = await _httpClient.GetAsync($"{BaseUrl}/item/{id}.json", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch story {StoryId}. Status: {StatusCode}", id, response.StatusCode);
                return null;
            }

            var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var story = JsonSerializer.Deserialize<Story>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (story != null)
            {
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(StoryCacheExpirationMinutes),
                    Size = 1
                };
                _cache.Set(cacheKey, story, cacheOptions);
            }

            return story;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching story {StoryId}", id);
            return null;
        }
    }

    public async Task<List<Story>> SearchStoriesAsync(string query, int maxResults = 100, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Searching stories with query: {Query}", query);

            if (string.IsNullOrWhiteSpace(query))
            {
                return new List<Story>();
            }

            var storyIds = await GetNewStoryIdsAsync(cancellationToken);
            var searchIds = storyIds.Take(maxResults * SearchBatchMultiplier).ToList();

            var tasks = searchIds.Select(id => GetStoryByIdAsync(id, cancellationToken));
            var stories = await Task.WhenAll(tasks);

            var validStories = stories.Where(s => s != null).Cast<Story>().ToList();
    
            var queryLower = query.ToLowerInvariant();
            var filteredStories = validStories
                .Where(story =>
                    story.Title.ToLowerInvariant().Contains(queryLower) ||
                    story.By.ToLowerInvariant().Contains(queryLower))
                .Take(maxResults)
                .ToList();

            return filteredStories;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error searching stories with query: {Query}", query);
            throw new InvalidOperationException("Failed to search stories from Hacker News API", ex);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Request timeout searching stories with query: {Query}", query);
            throw new TimeoutException("Request timeout while searching stories", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error searching stories with query: {Query}", query);
            throw;
        }
    }

    private async Task<List<int>> GetNewStoryIdsAsync(CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(NewStoriesCacheKey, out List<int>? cachedIds))
        {
            return cachedIds ?? new List<int>();
        }

        var response = await _httpClient.GetAsync($"{BaseUrl}/newstories.json", cancellationToken);
        response.EnsureSuccessStatusCode();

        var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var storyIds = JsonSerializer.Deserialize<List<int>>(jsonContent) ?? new List<int>();

        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(NewStoriesCacheExpirationMinutes),
            Size = 10
        };
        _cache.Set(NewStoriesCacheKey, storyIds, cacheOptions);

        return storyIds;
    }
}

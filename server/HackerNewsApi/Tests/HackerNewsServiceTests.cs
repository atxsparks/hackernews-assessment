using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using HackerNewsApi.Services;
using HackerNewsApi.Models;
using Xunit;

namespace HackerNewsApi.Tests;

public class HackerNewsServiceTests : IDisposable
{
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _memoryCache;
    private readonly Mock<ILogger<HackerNewsService>> _loggerMock;
    private readonly HackerNewsService _service;

    public HackerNewsServiceTests()
    {
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _loggerMock = new Mock<ILogger<HackerNewsService>>();
        _service = new HackerNewsService(_httpClient, _memoryCache, _loggerMock.Object);
    }

    [Fact]
    public async Task GetNewStoriesAsync_ReturnsCorrectPaginatedResult()
    {
        var storyIds = new List<int> { 1, 2, 3, 4, 5 };
        var story1 = new Story { Id = 1, Title = "Test Story 1", By = "user1", Score = 100 };
        var story2 = new Story { Id = 2, Title = "Test Story 2", By = "user2", Score = 85 };

        SetupHttpResponse("/v0/newstories.json", JsonSerializer.Serialize(storyIds));
        SetupHttpResponse("/v0/item/1.json", JsonSerializer.Serialize(story1));
        SetupHttpResponse("/v0/item/2.json", JsonSerializer.Serialize(story2));

        var result = await _service.GetNewStoriesAsync(page: 1, pageSize: 2);

        Assert.Equal(2, result.Stories.Count);
        Assert.Equal(5, result.TotalCount);
        Assert.Equal(1, result.CurrentPage);
        Assert.Equal(3, result.TotalPages);
        Assert.Equal(2, result.PageSize);
        Assert.Equal("Test Story 1", result.Stories[0].Title);
        Assert.Equal("Test Story 2", result.Stories[1].Title);
    }

    [Fact]
    public async Task GetNewStoriesAsync_HandlesEmptyResponse()
    {
        SetupHttpResponse("/v0/newstories.json", JsonSerializer.Serialize(new List<int>()));

        var result = await _service.GetNewStoriesAsync();

        Assert.Empty(result.Stories);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(1, result.CurrentPage);
        Assert.Equal(0, result.TotalPages);
    }

    [Fact]
    public async Task GetStoryByIdAsync_ReturnsStoryFromApi()
    {
        var expectedStory = new Story
        {
            Id = 123,
            Title = "Test Story",
            By = "testuser",
            Score = 42,
            Time = 1640995200,
            Url = "https://example.com"
        };

        SetupHttpResponse("/v0/item/123.json", JsonSerializer.Serialize(expectedStory));

        var result = await _service.GetStoryByIdAsync(123);

        Assert.NotNull(result);
        Assert.Equal(123, result.Id);
        Assert.Equal("Test Story", result.Title);
        Assert.Equal("testuser", result.By);
        Assert.Equal(42, result.Score);
    }

    [Fact]
    public async Task GetStoryByIdAsync_ReturnsCachedStory()
    {
        var cachedStory = new Story { Id = 456, Title = "Cached Story", By = "cacheduser" };
        _memoryCache.Set("story_456", cachedStory);

        var result = await _service.GetStoryByIdAsync(456);

        Assert.NotNull(result);
        Assert.Equal("Cached Story", result.Title);
        Assert.Equal("cacheduser", result.By);

        _httpMessageHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Never(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task GetStoryByIdAsync_ReturnsNullForNotFound()
    {
        SetupHttpResponse("/v0/item/999.json", "", HttpStatusCode.NotFound);

        var result = await _service.GetStoryByIdAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task SearchStoriesAsync_FiltersStoriesByTitle()
    {
        var storyIds = new List<int> { 1, 2, 3 };
        var story1 = new Story { Id = 1, Title = "Angular Tutorial", By = "dev1" };
        var story2 = new Story { Id = 2, Title = "React Guide", By = "dev2" };
        var story3 = new Story { Id = 3, Title = "Vue.js Tips", By = "dev3" };

        SetupHttpResponse("/v0/newstories.json", JsonSerializer.Serialize(storyIds));
        SetupHttpResponse("/v0/item/1.json", JsonSerializer.Serialize(story1));
        SetupHttpResponse("/v0/item/2.json", JsonSerializer.Serialize(story2));
        SetupHttpResponse("/v0/item/3.json", JsonSerializer.Serialize(story3));

        var result = await _service.SearchStoriesAsync("Angular");

        Assert.Single(result);
        Assert.Equal("Angular Tutorial", result[0].Title);
    }

    [Fact]
    public async Task SearchStoriesAsync_FiltersStoriesByAuthor()
    {
        var storyIds = new List<int> { 1, 2 };
        var story1 = new Story { Id = 1, Title = "Some Title", By = "john_doe" };
        var story2 = new Story { Id = 2, Title = "Another Title", By = "jane_smith" };

        SetupHttpResponse("/v0/newstories.json", JsonSerializer.Serialize(storyIds));
        SetupHttpResponse("/v0/item/1.json", JsonSerializer.Serialize(story1));
        SetupHttpResponse("/v0/item/2.json", JsonSerializer.Serialize(story2));

        var result = await _service.SearchStoriesAsync("john");

        Assert.Single(result);
        Assert.Equal("john_doe", result[0].By);
    }

    [Fact]
    public async Task SearchStoriesAsync_IsCaseInsensitive()
    {
        var storyIds = new List<int> { 1 };
        var story = new Story { Id = 1, Title = "JavaScript Basics", By = "coder" };

        SetupHttpResponse("/v0/newstories.json", JsonSerializer.Serialize(storyIds));
        SetupHttpResponse("/v0/item/1.json", JsonSerializer.Serialize(story));

        var result = await _service.SearchStoriesAsync("JAVASCRIPT");

        Assert.Single(result);
        Assert.Equal("JavaScript Basics", result[0].Title);
    }

    [Fact]
    public async Task SearchStoriesAsync_ReturnsEmptyForEmptyQuery()
    {
        var result = await _service.SearchStoriesAsync("");

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetNewStoriesAsync_UsesCachedStoryIds()
    {
        var storyIds = new List<int> { 1, 2 };
        _memoryCache.Set("new_stories", storyIds);

        var story1 = new Story { Id = 1, Title = "Cached Test", By = "user1" };
        SetupHttpResponse("/v0/item/1.json", JsonSerializer.Serialize(story1));

        var result = await _service.GetNewStoriesAsync(page: 1, pageSize: 1);

        Assert.Single(result.Stories);
        Assert.Equal("Cached Test", result.Stories[0].Title);

        _httpMessageHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task GetNewStoriesAsync_ThrowsInvalidOperationException_OnHttpRequestException()
    {
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.GetNewStoriesAsync());

        Assert.Equal("Failed to fetch stories from Hacker News API", exception.Message);
        Assert.IsType<HttpRequestException>(exception.InnerException);
    }

    [Fact]
    public async Task GetNewStoriesAsync_ThrowsTimeoutException_OnTaskCanceledException()
    {
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("Request timeout"));

        var exception = await Assert.ThrowsAsync<TimeoutException>(
            () => _service.GetNewStoriesAsync());

        Assert.Equal("Request timeout while fetching stories", exception.Message);
        Assert.IsType<TaskCanceledException>(exception.InnerException);
    }

    [Fact]
    public async Task GetNewStoriesAsync_ThrowsInvalidOperationException_OnJsonException()
    {
        SetupHttpResponse("/v0/newstories.json", "invalid json");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.GetNewStoriesAsync());

        Assert.Equal("Failed to parse response from Hacker News API", exception.Message);
        Assert.IsType<JsonException>(exception.InnerException);
    }

    [Fact]
    public async Task SearchStoriesAsync_ThrowsInvalidOperationException_OnHttpRequestException()
    {
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.SearchStoriesAsync("test"));

        Assert.Equal("Failed to search stories from Hacker News API", exception.Message);
        Assert.IsType<HttpRequestException>(exception.InnerException);
    }

    [Fact]
    public async Task SearchStoriesAsync_ThrowsTimeoutException_OnTaskCanceledException()
    {
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("Request timeout"));

        var exception = await Assert.ThrowsAsync<TimeoutException>(
            () => _service.SearchStoriesAsync("test"));

        Assert.Equal("Request timeout while searching stories", exception.Message);
        Assert.IsType<TaskCanceledException>(exception.InnerException);
    }

    private void SetupHttpResponse(string requestUri, string responseContent, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains(requestUri)),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
            });
    }

    public void Dispose()
    {
        _httpClient.Dispose();
        _memoryCache.Dispose();
    }
}

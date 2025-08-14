using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using HackerNewsApi.Controllers;
using HackerNewsApi.Services;
using HackerNewsApi.Models;
using Xunit;

namespace HackerNewsApi.Tests;

public class StoriesControllerTests
{
    private readonly Mock<IHackerNewsService> _serviceMock;
    private readonly Mock<ILogger<StoriesController>> _loggerMock;
    private readonly StoriesController _controller;

    public StoriesControllerTests()
    {
        _serviceMock = new Mock<IHackerNewsService>();
        _loggerMock = new Mock<ILogger<StoriesController>>();
        _controller = new StoriesController(_serviceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetNewestStories_ReturnsOkResult_WithValidParameters()
    {

        var expectedResult = new PaginatedStories
        {
            Stories = new List<Story>
            {
                new() { Id = 1, Title = "Test Story 1", By = "user1" },
                new() { Id = 2, Title = "Test Story 2", By = "user2" }
            },
            TotalCount = 100,
            CurrentPage = 1,
            TotalPages = 5,
            PageSize = 20
        };

        _serviceMock.Setup(s => s.GetNewStoriesAsync(1, 20, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(expectedResult);

        var result = await _controller.GetNewestStories();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedStories = Assert.IsType<PaginatedStories>(okResult.Value);
        Assert.Equal(2, returnedStories.Stories.Count);
        Assert.Equal(100, returnedStories.TotalCount);
        Assert.Equal(1, returnedStories.CurrentPage);
    }

    [Fact]
    public async Task GetNewestStories_ReturnsBadRequest_WhenPageIsZero()
    {
        var result = await _controller.GetNewestStories(page: 0);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Page number must be greater than 0", badRequestResult.Value);
    }

    [Fact]
    public async Task GetNewestStories_ReturnsBadRequest_WhenPageSizeIsInvalid()
    {
        var result1 = await _controller.GetNewestStories(pageSize: 0);
        var result2 = await _controller.GetNewestStories(pageSize: 51);

        var badRequestResult1 = Assert.IsType<BadRequestObjectResult>(result1.Result);
        var badRequestResult2 = Assert.IsType<BadRequestObjectResult>(result2.Result);
        Assert.Equal("Page size must be between 1 and 50", badRequestResult1.Value);
        Assert.Equal("Page size must be between 1 and 50", badRequestResult2.Value);
    }

    [Fact]
    public async Task GetNewestStories_ReturnsInternalServerError_WhenServiceThrows()
    {
        _serviceMock.Setup(s => s.GetNewStoriesAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                   .ThrowsAsync(new Exception("Service error"));

        var result = await _controller.GetNewestStories();

        var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        Assert.Equal("An error occurred while fetching stories", statusCodeResult.Value);
    }

    [Fact]
    public async Task GetStoryById_ReturnsOkResult_WhenStoryExists()
    {
        var expectedStory = new Story { Id = 123, Title = "Test Story", By = "testuser" };
        _serviceMock.Setup(s => s.GetStoryByIdAsync(123, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(expectedStory);

        var result = await _controller.GetStoryById(123);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedStory = Assert.IsType<Story>(okResult.Value);
        Assert.Equal(123, returnedStory.Id);
        Assert.Equal("Test Story", returnedStory.Title);
    }

    [Fact]
    public async Task GetStoryById_ReturnsNotFound_WhenStoryDoesNotExist()
    {
        _serviceMock.Setup(s => s.GetStoryByIdAsync(999, It.IsAny<CancellationToken>()))
                   .ReturnsAsync((Story?)null);

        var result = await _controller.GetStoryById(999);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal("Story with ID 999 not found", notFoundResult.Value);
    }

    [Fact]
    public async Task GetStoryById_ReturnsBadRequest_WhenIdIsInvalid()
    {
        var result = await _controller.GetStoryById(-1);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Story ID must be greater than 0", badRequestResult.Value);
    }

    [Fact]
    public async Task SearchStories_ReturnsOkResult_WithValidQuery()
    {
        var expectedStories = new List<Story>
        {
            new() { Id = 1, Title = "Angular Tutorial", By = "dev1" },
            new() { Id = 2, Title = "Angular Components", By = "dev2" }
        };

        _serviceMock.Setup(s => s.SearchStoriesAsync("Angular", 50, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(expectedStories);

        var result = await _controller.SearchStories("Angular");

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedStories = Assert.IsType<List<Story>>(okResult.Value);
        Assert.Equal(2, returnedStories.Count);
        Assert.All(returnedStories, story => Assert.Contains("Angular", story.Title));
    }

    [Fact]
    public async Task SearchStories_ReturnsBadRequest_WhenQueryIsEmpty()
    {
        var result1 = await _controller.SearchStories("");
        var result2 = await _controller.SearchStories("   ");

        var badRequestResult1 = Assert.IsType<BadRequestObjectResult>(result1.Result);
        var badRequestResult2 = Assert.IsType<BadRequestObjectResult>(result2.Result);
        Assert.Equal("Search query cannot be empty", badRequestResult1.Value);
        Assert.Equal("Search query cannot be empty", badRequestResult2.Value);
    }

    [Fact]
    public async Task SearchStories_ReturnsBadRequest_WhenLimitIsInvalid()
    {
        var result1 = await _controller.SearchStories("test", limit: 0);
        var result2 = await _controller.SearchStories("test", limit: 101);

        var badRequestResult1 = Assert.IsType<BadRequestObjectResult>(result1.Result);
        var badRequestResult2 = Assert.IsType<BadRequestObjectResult>(result2.Result);
        Assert.Equal("Limit must be between 1 and 100", badRequestResult1.Value);
        Assert.Equal("Limit must be between 1 and 100", badRequestResult2.Value);
    }

    [Fact]
    public async Task SearchStories_ReturnsInternalServerError_WhenServiceThrows()
    {
        _serviceMock.Setup(s => s.SearchStoriesAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                   .ThrowsAsync(new Exception("Search error"));

        var result = await _controller.SearchStories("test");

        var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        Assert.Equal("An error occurred while searching stories", statusCodeResult.Value);
    }

    [Fact]
    public async Task GetNewestStories_ReturnsInternalServerError_WhenServiceThrowsTimeoutException()
    {
        _serviceMock.Setup(s => s.GetNewStoriesAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                   .ThrowsAsync(new TimeoutException("Request timeout"));

        var result = await _controller.GetNewestStories();

        var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        Assert.Equal("An error occurred while fetching stories", statusCodeResult.Value);
    }

    [Fact]
    public async Task GetNewestStories_ReturnsInternalServerError_WhenServiceThrowsInvalidOperationException()
    {
        _serviceMock.Setup(s => s.GetNewStoriesAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                   .ThrowsAsync(new InvalidOperationException("API error"));

        var result = await _controller.GetNewestStories();

        var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        Assert.Equal("An error occurred while fetching stories", statusCodeResult.Value);
    }

    [Fact]
    public async Task SearchStories_ReturnsInternalServerError_WhenServiceThrowsTimeoutException()
    {
        _serviceMock.Setup(s => s.SearchStoriesAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                   .ThrowsAsync(new TimeoutException("Request timeout"));

        var result = await _controller.SearchStories("test");

        var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        Assert.Equal("An error occurred while searching stories", statusCodeResult.Value);
    }

    [Fact]
    public async Task GetStoryById_ReturnsInternalServerError_WhenServiceThrowsTimeoutException()
    {

        _serviceMock.Setup(s => s.GetStoryByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                   .ThrowsAsync(new TimeoutException("Request timeout"));

        var result = await _controller.GetStoryById(123);

        var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        Assert.Equal("An error occurred while fetching the story", statusCodeResult.Value);
    }


}

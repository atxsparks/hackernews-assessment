using Microsoft.AspNetCore.Mvc;
using HackerNewsApi.Services;
using HackerNewsApi.Models;
using System.ComponentModel.DataAnnotations;

namespace HackerNewsApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StoriesController : ControllerBase
{
    private readonly IHackerNewsService _hackerNewsService;
    private readonly ILogger<StoriesController> _logger;

    public StoriesController(IHackerNewsService hackerNewsService, ILogger<StoriesController> logger)
    {
        _hackerNewsService = hackerNewsService;
        _logger = logger;
    }

    /// <summary>
    /// Get newest stories with pagination
    /// </summary>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Number of stories per page (default: 20, max: 50)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of newest stories</returns>
    [HttpGet("newest")]
    public async Task<ActionResult<PaginatedStories>> GetNewestStories(
        [FromQuery][Range(1, int.MaxValue, ErrorMessage = "Page number must be greater than 0")] int page = 1,
        [FromQuery][Range(1, 50, ErrorMessage = "Page size must be between 1 and 50")] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (page < 1)
            {
                return BadRequest("Page number must be greater than 0");
            }

            if (pageSize < 1 || pageSize > 50)
            {
                return BadRequest("Page size must be between 1 and 50");
            }

            _logger.LogInformation("Getting newest stories - Page: {Page}, PageSize: {PageSize}", page, pageSize);

            var result = await _hackerNewsService.GetNewStoriesAsync(page, pageSize, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting newest stories");
            return StatusCode(500, "An error occurred while fetching stories");
        }
    }

    /// <summary>
    /// Get a specific story by ID
    /// </summary>
    /// <param name="id">Story ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Story details or 404 if not found</returns>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<Story>> GetStoryById(
        [Range(1, int.MaxValue, ErrorMessage = "Story ID must be greater than 0")] int id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (id <= 0)
            {
                return BadRequest("Story ID must be greater than 0");
            }

            _logger.LogInformation("Getting story by ID: {StoryId}", id);

            var story = await _hackerNewsService.GetStoryByIdAsync(id, cancellationToken);

            if (story == null)
            {
                return NotFound($"Story with ID {id} not found");
            }

            return Ok(story);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting story {StoryId}", id);
            return StatusCode(500, "An error occurred while fetching the story");
        }
    }

    /// <summary>
    /// Search stories by title or author
    /// </summary>
    /// <param name="q">Search query</param>
    /// <param name="limit">Maximum number of results (default: 50, max: 100)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of matching stories</returns>
    [HttpGet("search")]
    public async Task<ActionResult<List<Story>>> SearchStories(
        [FromQuery][Required(ErrorMessage = "Search query is required")] string q,
        [FromQuery][Range(1, 100, ErrorMessage = "Limit must be between 1 and 100")] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return BadRequest("Search query cannot be empty");
            }

            if (limit < 1 || limit > 100)
            {
                return BadRequest("Limit must be between 1 and 100");
            }

            _logger.LogInformation("Searching stories with query: {Query}, Limit: {Limit}", q, limit);

            var stories = await _hackerNewsService.SearchStoriesAsync(q, limit, cancellationToken);
            return Ok(stories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching stories with query: {Query}", q);
            return StatusCode(500, "An error occurred while searching stories");
        }
    }


}

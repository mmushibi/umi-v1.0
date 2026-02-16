using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using UmiHealthPOS.Models.DTOs;
using UmiHealthPOS.Services;

namespace UmiHealthPOS.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SearchController : ControllerBase
    {
        private readonly IWebSearchService _webSearchService;
        private readonly ILogger<SearchController> _logger;

        public SearchController(IWebSearchService webSearchService, ILogger<SearchController> logger)
        {
            _webSearchService = webSearchService;
            _logger = logger;
        }

        /// <summary>
        /// Perform web search for medical information
        /// </summary>
        /// <param name="request">Search request parameters</param>
        /// <returns>Search results with summaries</returns>
        [HttpPost("web")]
        public async Task<ActionResult<SearchResponseDto>> SearchWeb([FromBody] SearchRequestDto request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Query))
                {
                    return BadRequest(new { error = "Search query is required" });
                }

                if (request.Query.Length < 2)
                {
                    return BadRequest(new { error = "Search query must be at least 2 characters long" });
                }

                var result = await _webSearchService.SearchAsync(request);

                _logger.LogInformation("Web search performed for query: {Query}, Type: {Type}, Results: {Count}",
                    request.Query, request.SearchType, result.Results.Count);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing web search for query: {Query}", request.Query);
                return StatusCode(500, new { error = "An error occurred while performing search" });
            }
        }

        /// <summary>
        /// Get autocomplete suggestions for search queries
        /// </summary>
        /// <param name="request">Autocomplete request parameters</param>
        /// <returns>List of search suggestions</returns>
        [HttpPost("autocomplete")]
        public async Task<ActionResult<AutoCompleteResponseDto>> GetAutoComplete([FromBody] AutoCompleteRequestDto request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Query))
                {
                    return BadRequest(new { error = "Search query is required" });
                }

                if (request.Query.Length < 1)
                {
                    return BadRequest(new { error = "Search query must be at least 1 character long" });
                }

                var result = await _webSearchService.GetAutoCompleteAsync(request);

                _logger.LogInformation("Autocomplete requested for query: {Query}, Type: {Type}, Suggestions: {Count}",
                    request.Query, request.SearchType, result.Suggestions.Count);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting autocomplete suggestions for query: {Query}", request.Query);
                return StatusCode(500, new { error = "An error occurred while getting suggestions" });
            }
        }

        /// <summary>
        /// Get popular search terms and trending medical topics
        /// </summary>
        /// <param name="searchType">Optional search type filter</param>
        /// <returns>List of popular search terms</returns>
        [HttpGet("trending")]
        public async Task<ActionResult<AutoCompleteResponseDto>> GetTrendingSearches([FromQuery] string searchType = "general")
        {
            try
            {
                var request = new AutoCompleteRequestDto
                {
                    Query = "", // Empty query to get all trending terms
                    SearchType = searchType,
                    MaxSuggestions = 10
                };

                var result = await _webSearchService.GetAutoCompleteAsync(request);

                _logger.LogInformation("Trending searches requested for type: {Type}, Results: {Count}",
                    searchType, result.Suggestions.Count);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting trending searches for type: {Type}", searchType);
                return StatusCode(500, new { error = "An error occurred while getting trending searches" });
            }
        }

        /// <summary>
        /// Get search history for the current user (placeholder for future implementation)
        /// </summary>
        /// <returns>User's search history</returns>
        [HttpGet("history")]
        public async Task<ActionResult<List<string>>> GetSearchHistory()
        {
            try
            {
                // Placeholder implementation
                // In production, this would fetch from user's search history in database
                var mockHistory = new List<string>
                {
                    "blood pressure medication",
                    "diabetes treatment guidelines",
                    "antibiotic interactions",
                    "pain management protocols"
                };

                await Task.Delay(100); // Simulate database call

                return Ok(mockHistory);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting search history");
                return StatusCode(500, new { error = "An error occurred while getting search history" });
            }
        }

        /// <summary>
        /// Save search term to user history (placeholder for future implementation)
        /// </summary>
        /// <param name="searchTerm">The search term to save</param>
        /// <returns>Success status</returns>
        [HttpPost("history")]
        public async Task<ActionResult> SaveSearchTerm([FromBody] string searchTerm)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    return BadRequest(new { error = "Search term is required" });
                }

                // Placeholder implementation
                // In production, this would save to user's search history in database
                await Task.Delay(50); // Simulate database call

                _logger.LogInformation("Search term saved: {SearchTerm}", searchTerm);

                return Ok(new { success = true, message = "Search term saved successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving search term: {SearchTerm}", searchTerm);
                return StatusCode(500, new { error = "An error occurred while saving search term" });
            }
        }
    }
}

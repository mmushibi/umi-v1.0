using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using UmiHealthPOS.Models.DTOs;
using UmiHealthPOS.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Http;
using UmiHealthPOS.Data;
using UmiHealthPOS.Models;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace UmiHealthPOS.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SearchController : ControllerBase
    {
        private readonly IWebSearchService _webSearchService;
        private readonly ILogger<SearchController> _logger;
        private readonly IMemoryCache _memoryCache;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ApplicationDbContext _context;

        public SearchController(
            IWebSearchService webSearchService, 
            ILogger<SearchController> logger,
            IMemoryCache memoryCache,
            IHttpContextAccessor httpContextAccessor,
            ApplicationDbContext context)
        {
            _webSearchService = webSearchService;
            _logger = logger;
            _memoryCache = memoryCache;
            _httpContextAccessor = httpContextAccessor;
            _context = context;
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

                // Save search to history
                await SaveSearchToHistory(request.Query, request.SearchType, result.Results.Count);

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
        /// Get search history for the current user from database
        /// </summary>
        /// <param name="limit">Maximum number of results to return</param>
        /// <param name="searchType">Optional search type filter</param>
        /// <returns>User's search history</returns>
        [HttpGet("history")]
        public async Task<ActionResult<List<SearchHistoryDto>>> GetSearchHistory([FromQuery] int limit = 10, [FromQuery] string? searchType = null)
        {
            try
            {
                var userId = GetCurrentUserId();
                var tenantId = GetCurrentTenantId();

                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(tenantId))
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var query = _context.SearchHistories
                    .Where(sh => sh.TenantId == tenantId && sh.UserId == userId && sh.IsActive)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(searchType))
                {
                    query = query.Where(sh => sh.SearchType == searchType);
                }

                var searchHistory = await query
                    .OrderByDescending(sh => sh.SearchedAt)
                    .Take(limit)
                    .Select(sh => new SearchHistoryDto
                    {
                        Id = sh.Id,
                        Query = sh.Query,
                        SearchType = sh.SearchType,
                        ResultCount = sh.ResultCount,
                        SearchedAt = sh.SearchedAt,
                        Source = sh.Source
                    })
                    .ToListAsync();

                return Ok(searchHistory);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting search history for user");
                return StatusCode(500, new { error = "An error occurred while getting search history" });
            }
        }

        /// <summary>
        /// Save search term to user history in database
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

                await SaveSearchToHistory(searchTerm, "general", null);

                _logger.LogInformation("Search term saved for user {UserId}: {SearchTerm}", GetCurrentUserId(), searchTerm);

                return Ok(new { success = true, message = "Search term saved successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving search term: {SearchTerm}", searchTerm);
                return StatusCode(500, new { error = "An error occurred while saving search term" });
            }
        }

        /// <summary>
        /// Clear search history for the current user
        /// </summary>
        /// <returns>Success status</returns>
        [HttpDelete("history")]
        public async Task<ActionResult> ClearSearchHistory()
        {
            try
            {
                var userId = GetCurrentUserId();
                var tenantId = GetCurrentTenantId();

                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(tenantId))
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var userSearchHistory = await _context.SearchHistories
                    .Where(sh => sh.TenantId == tenantId && sh.UserId == userId && sh.IsActive)
                    .ToListAsync();

                foreach (var item in userSearchHistory)
                {
                    item.IsActive = false;
                    item.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Search history cleared for user {UserId}", userId);

                return Ok(new { success = true, message = "Search history cleared successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing search history for user");
                return StatusCode(500, new { error = "An error occurred while clearing search history" });
            }
        }

        private async Task SaveSearchToHistory(string query, string searchType, int? resultCount)
        {
            try
            {
                var userId = GetCurrentUserId();
                var tenantId = GetCurrentTenantId();

                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(tenantId))
                {
                    return; // Skip saving if user is not authenticated
                }

                var searchHistory = new SearchHistory
                {
                    TenantId = tenantId,
                    UserId = userId,
                    Query = query.Trim(),
                    SearchType = searchType,
                    ResultCount = resultCount,
                    Source = "web",
                    IPAddress = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString(),
                    UserAgent = _httpContextAccessor.HttpContext?.Request?.Headers["User-Agent"].ToString(),
                    SearchedAt = DateTime.UtcNow
                };

                _context.SearchHistories.Add(searchHistory);
                await _context.SaveChangesAsync();

                // Clean up old search history (keep only last 100 entries per user)
                await CleanupOldSearchHistory(userId, tenantId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving search to history: {Query}", query);
                // Don't throw - search saving should not break the main functionality
            }
        }

        private async Task CleanupOldSearchHistory(string userId, string tenantId)
        {
            try
            {
                var oldSearches = await _context.SearchHistories
                    .Where(sh => sh.TenantId == tenantId && sh.UserId == userId && sh.IsActive)
                    .OrderByDescending(sh => sh.SearchedAt)
                    .Skip(100)
                    .ToListAsync();

                foreach (var oldSearch in oldSearches)
                {
                    oldSearch.IsActive = false;
                    oldSearch.UpdatedAt = DateTime.UtcNow;
                }

                if (oldSearches.Any())
                {
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up old search history for user: {UserId}", userId);
            }
        }

        private string GetCurrentUserId()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.User?.Identity?.IsAuthenticated == true)
            {
                return httpContext.User.FindFirst("sub")?.Value ??
                       httpContext.User.FindFirst("userId")?.Value ??
                       httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ??
                       "anonymous";
            }
            
            return httpContext?.Connection?.RemoteIpAddress?.ToString() ?? "anonymous";
        }

        private string GetCurrentTenantId()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.User?.Identity?.IsAuthenticated == true)
            {
                return httpContext.User.FindFirst("tenantId")?.Value ?? "";
            }
            
            return "";
        }
    }

    // DTO for search history response
    public class SearchHistoryDto
    {
        public int Id { get; set; }
        public string Query { get; set; } = string.Empty;
        public string SearchType { get; set; } = string.Empty;
        public int? ResultCount { get; set; }
        public DateTime SearchedAt { get; set; }
        public string? Source { get; set; }
    }
}

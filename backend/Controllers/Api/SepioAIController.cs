using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using UmiHealthPOS.Models.DTOs;
using UmiHealthPOS.DTOs;
using UmiHealthPOS.Services;
using UmiHealthPOS.Data;
using UmiHealthPOS.Models.AI;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace UmiHealthPOS.Controllers.Api
{
    /// <summary>
    /// Sepio AI Controller - Provides intelligent medical information and assistance
    /// Features: Natural language processing, learning capabilities, and contextual responses
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [Produces("application/json")]
    public class SepioAIController : ControllerBase
    {
        private readonly ILogger<SepioAIController> _logger;
        private readonly ISepioAIService _sepioAIService;
        private readonly ApplicationDbContext _context;
        private const int MAX_QUERY_LENGTH = 1000;
        private const int MIN_QUERY_LENGTH = 3;

        public SepioAIController(ILogger<SepioAIController> logger, ISepioAIService sepioAIService, ApplicationDbContext context)
        {
            _logger = logger;
            _sepioAIService = sepioAIService;
            _context = context;
        }

        /// <summary>
        /// Ask Sepio AI a medical question with enhanced natural language processing
        /// </summary>
        /// <param name="request">AI request containing query, context, and session information</param>
        /// <returns>AI response with confidence score, sources, and processing time</returns>
        /// <response code="200">Successfully processed the query and returned AI response</response>
        /// <response code="400">Invalid request - missing or malformed query</response>
        /// <response code="401">Unauthorized - valid JWT token required</response>
        /// <response code="429">Rate limit exceeded - too many requests</response>
        /// <response code="500">Internal server error - AI service unavailable</response>
        [HttpPost("ask")]
        [ProducesResponseType(typeof(AIResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<AIResponseDto>> AskSepioAI([FromBody] AIRequestDto request)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            try
            {
                // Enhanced validation
                var validationResult = ValidateAIRequest(request);
                if (!validationResult.IsValid)
                {
                    _logger.LogWarning("Invalid AI request: {Errors}", string.Join(", ", validationResult.Errors));
                    return BadRequest(new { error = "Invalid request", details = validationResult.Errors });
                }

                // Generate session ID if not provided
                request.SessionId ??= GenerateSessionId();
                
                // Get user context for personalization
                var userContext = GetUserContext();
                var userId = GetCurrentUserId();
                var tenantId = GetCurrentTenantId();
                
                // Use the actual AI service with real-time data support
                var response = await _sepioAIService.AskAIAsync(request);
                
                stopwatch.Stop();
                response.ResponseTime = stopwatch.Elapsed;
                
                // Add real-time data metadata
                response.HasRealTimeData = request.IncludeRealTimeData;
                response.LastUpdated = DateTime.UtcNow;
                
                // Save conversation to database
                await SaveConversationToDatabase(request, response, userId, tenantId);
                
                // Log successful interaction
                _logger.LogInformation("AI query processed successfully. Query: {QueryLength} chars, Confidence: {Confidence:P1}, Time: {ResponseTime}ms", 
                    request.Query.Length, response.Confidence, response.ResponseTime.TotalMilliseconds);
                
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing AI request for query: {Query}", request.Query);
                return StatusCode(500, new { error = "An error occurred while processing your request", requestId = Guid.NewGuid().ToString() });
            }
        }

        /// <summary>
        /// Get smart suggestions using machine learning
        /// </summary>
        /// <param name="request">Suggestion request with query and user context</param>
        /// <returns>List of ML-enhanced suggestions</returns>
        [HttpPost("smart-suggestions")]
        public async Task<ActionResult<List<string>>> GetSmartSuggestions([FromBody] AIRequestDto request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Query))
                    return BadRequest(new { error = "Query is required" });

                // Use the actual service for ML-enhanced suggestions
                var userContext = GetUserContext();
                var smartSuggestions = await _sepioAIService.GenerateSmartSuggestionsAsync(request.Query, userContext);

                _logger.LogInformation("Smart suggestions generated for query: {Query}, Count: {Count}",
                    request.Query, smartSuggestions.Count);

                return Ok(smartSuggestions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting smart suggestions for query: {Query}", request.Query);
                return StatusCode(500, new { error = "An error occurred while getting smart suggestions" });
            }
        }

        /// <summary>
        /// Get learning insights and analytics
        /// </summary>
        /// <param name="userId">User identifier</param>
        /// <returns>Learning insights and analytics</returns>
        [HttpGet("learning-insights/{userId}")]
        public async Task<ActionResult<LearningInsightDto>> GetLearningInsights(string userId)
        {
            try
            {
                // Use the actual service for learning insights
                var insights = await _sepioAIService.GetLearningInsightsAsync(userId);

                _logger.LogInformation("Learning insights retrieved for user: {UserId}", userId);

                return Ok(insights);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting learning insights for user: {UserId}", userId);
                return StatusCode(500, new { error = "An error occurred while getting learning insights" });
            }
        }

        /// <summary>
        /// Train the model with user feedback
        /// </summary>
        /// <param name="feedback">Feedback data</param>
        /// <returns>Training result</returns>
        [HttpPost("train-model")]
        public async Task<ActionResult<bool>> TrainModel([FromBody] ModelFeedbackRequest feedback)
        {
            try
            {
                // Use the actual service for model training
                var trainingResult = await _sepioAIService.TrainModelAsync(
                    feedback.Feedback, 
                    feedback.Query, 
                    feedback.Response
                );

                _logger.LogInformation("Model training completed with feedback: {Feedback}", feedback.Feedback);

                return Ok(trainingResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error training model");
                return StatusCode(500, new { error = "An error occurred while training the model" });
            }
        }

        /// <summary>
        /// Get trending medical topics
        /// </summary>
        /// <returns>List of trending topics with popularity scores</returns>
        [HttpGet("trending")]
        public async Task<ActionResult<object>> GetTrendingTopics()
        {
            try
            {
                var trendingTopics = new[]
                {
                    new { Topic = "Hypertension Management", Popularity = 0.92, Category = "Cardiovascular" },
                    new { Topic = "Diabetes Medication", Popularity = 0.88, Category = "Endocrinology" },
                    new { Topic = "Antibiotic Resistance", Popularity = 0.85, Category = "Infectious Disease" },
                    new { Topic = "Malaria Treatment", Popularity = 0.82, Category = "Tropical Medicine" },
                    new { Topic = "COVID-19 Management", Popularity = 0.78, Category = "Respiratory" }
                };

                return Ok(new { Topics = trendingTopics, LastUpdated = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting trending topics");
                return StatusCode(500, new { error = "An error occurred while getting trending topics" });
            }
        }

        private ValidationResult ValidateAIRequest(AIRequestDto request)
        {
            var errors = new List<string>();

            // Basic validation
            if (string.IsNullOrWhiteSpace(request.Query))
                errors.Add("Query is required");

            if (request.Query?.Length < MIN_QUERY_LENGTH)
                errors.Add($"Query must be at least {MIN_QUERY_LENGTH} characters long");

            if (request.Query?.Length > MAX_QUERY_LENGTH)
                errors.Add($"Query must not exceed {MAX_QUERY_LENGTH} characters");

            // Content validation - check for potentially harmful content
            if (request.Query != null)
            {
                var suspiciousPatterns = new[] { "DROP TABLE", "DELETE FROM", "INSERT INTO", "UPDATE SET", "<script>", "javascript:" };
                foreach (var pattern in suspiciousPatterns)
                {
                    if (request.Query.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                    {
                        errors.Add("Query contains potentially harmful content");
                        break;
                    }
                }
            }

            // Context validation (optional)
            if (request.Context != null && request.Context.Length > 2000)
                errors.Add("Context must not exceed 2000 characters");

            return new ValidationResult
            {
                IsValid = !errors.Any(),
                Errors = errors
            };
        }

        private string GetUserContext()
        {
            var userRole = User?.FindFirst("role")?.Value ?? "unknown";
            var tenant = User?.FindFirst("tenant")?.Value ?? "unknown";
            var userId = User?.FindFirst("sub")?.Value ?? "unknown";
            
            return $"role:{userRole},tenant:{tenant},userId:{userId}";
        }

        private string GenerateSessionId()
        {
            return Guid.NewGuid().ToString("N")[..16];
        }

        private async Task SaveConversationToDatabase(AIRequestDto request, AIResponseDto response, string? userId, string? tenantId)
        {
            try
            {
                // Get or create conversation session
                var session = await _context.AIConversationSessions
                    .FirstOrDefaultAsync(s => s.SessionId == request.SessionId);

                if (session == null)
                {
                    session = new AIConversationSession
                    {
                        SessionId = request.SessionId,
                        UserId = userId,
                        TenantId = tenantId,
                        StartedAt = DateTime.UtcNow,
                        LastActivityAt = DateTime.UtcNow,
                        MessageCount = 0,
                        IsActive = true
                    };
                    await _context.AIConversationSessions.AddAsync(session);
                }
                else
                {
                    session.LastActivityAt = DateTime.UtcNow;
                    session.MessageCount += 1;
                }

                // Save user message
                var userMessage = new AIMessage
                {
                    SessionId = request.SessionId,
                    MessageId = Guid.NewGuid().ToString(),
                    MessageType = "user",
                    Content = request.Query,
                    Timestamp = DateTime.UtcNow,
                    ProcessingTimeMs = (int)response.ResponseTime.TotalMilliseconds,
                    Context = request.Context,
                    Metadata = System.Text.Json.JsonSerializer.Serialize(new { 
                        IncludeRealTimeData = request.IncludeRealTimeData,
                        MaxTokens = request.MaxTokens
                    })
                };
                await _context.AIMessages.AddAsync(userMessage);

                // Save AI response
                var aiMessage = new AIMessage
                {
                    SessionId = request.SessionId,
                    MessageId = Guid.NewGuid().ToString(),
                    MessageType = "assistant",
                    Content = response.Response,
                    Timestamp = DateTime.UtcNow,
                    ProcessingTimeMs = (int)response.ResponseTime.TotalMilliseconds,
                    Confidence = (decimal?)response.Confidence,
                    Sources = response.Sources != null ? System.Text.Json.JsonSerializer.Serialize(response.Sources) : null,
                    Metadata = System.Text.Json.JsonSerializer.Serialize(new { 
                        HasRealTimeData = response.HasRealTimeData,
                        LastUpdated = response.LastUpdated,
                        Suggestions = response.Suggestions
                    })
                };
                await _context.AIMessages.AddAsync(aiMessage);

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving conversation to database for session: {SessionId}", request.SessionId);
                // Don't throw - conversation saving shouldn't break the main flow
            }
        }

        private string? GetCurrentUserId()
        {
            return User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        }

        private string? GetCurrentTenantId()
        {
            return User.FindFirst("TenantId")?.Value;
        }

        private class ValidationResult
        {
            public bool IsValid { get; set; }
            public List<string> Errors { get; set; } = new();
        }
    }
}

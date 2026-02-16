using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using UmiHealthPOS.Models.DTOs;

namespace UmiHealthPOS.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SepioAIController : ControllerBase
    {
        private readonly ILogger<SepioAIController> _logger;
        private static readonly Dictionary<string, List<object>> _conversations = new();

        public SepioAIController(ILogger<SepioAIController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Ask Sepio AI a medical question
        /// </summary>
        /// <param name="request">AI request with query and context</param>
        /// <returns>AI response with sources and confidence</returns>
        [HttpPost("ask")]
        public async Task<ActionResult<object>> AskSepioAI([FromBody] AIRequestDto request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Query))
                    return BadRequest(new { error = "Query is required" });

                if (request.Query.Length < 3)
                    return BadRequest(new { error = "Query must be at least 3 characters long" });

                // Generate session ID if not provided
                request.SessionId ??= GenerateSessionId();

                // Mock AI response for now - replace with real service when compilation issues are resolved
                var response = new
                {
                    Response = GenerateMockResponse(request.Query),
                    Sources = GenerateMockSources(request.Query),
                    Confidence = 0.85,
                    SessionId = request.SessionId,
                    Timestamp = DateTime.UtcNow
                };

                // Store conversation (simplified)
                StoreConversation(request.SessionId, request.Query, response.Response);

                _logger.LogInformation("Sepio AI query: {Query}, Session: {SessionId}",
                    request.Query, request.SessionId);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Sepio AI response for query: {Query}", request.Query);
                return StatusCode(500, new { error = "An error occurred while processing your request" });
            }
        }

        /// <summary>
        /// Get AI suggestions for follow-up questions
        /// </summary>
        /// <param name="request">Suggestion request with query and context</param>
        /// <returns>List of AI-generated suggestions</returns>
        [HttpPost("suggestions")]
        public async Task<ActionResult<List<string>>> GetAISuggestions([FromBody] AIRequestDto request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Query))
                    return BadRequest(new { error = "Query is required" });

                var suggestions = GenerateMockSuggestions(request.Query);

                _logger.LogInformation("AI suggestions requested for query: {Query}, Count: {Count}",
                    request.Query, suggestions.Count);

                return Ok(suggestions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting AI suggestions for query: {Query}", request.Query);
                return StatusCode(500, new { error = "An error occurred while getting suggestions" });
            }
        }

        /// <summary>
        /// Get trending medical topics and questions
        /// </summary>
        /// <returns>List of trending topics</returns>
        [HttpGet("trending")]
        public async Task<ActionResult<List<string>>> GetTrendingTopics()
        {
            try
            {
                // Mock trending topics
                var trendingTopics = new List<string>
                {
                    "What are the side effects of metformin?",
                    "How to manage hypertension in elderly patients?",
                    "What are the latest COVID-19 treatment guidelines?",
                    "Drug interactions between antidepressants and blood thinners",
                    "Best practices for diabetes management",
                    "Symptoms of vitamin D deficiency",
                    "Antibiotic resistance guidelines",
                    "Pain management options for chronic conditions"
                };

                await Task.Delay(100); // Simulate database call

                _logger.LogInformation("Trending topics requested, Count: {Count}", trendingTopics.Count);

                return Ok(trendingTopics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting trending topics");
                return StatusCode(500, new { error = "An error occurred while getting trending topics" });
            }
        }

        private string GenerateMockResponse(string query)
        {
            var queryLower = query.ToLower();

            if (queryLower.Contains("drug") || queryLower.Contains("medication"))
            {
                return $"Based on current medical information, here's what I can tell you about {query}:\n\n" +
                       $"**Important Information:**\n" +
                       $"• Always consult with a healthcare professional before starting any medication\n" +
                       $"• This information is for educational purposes only\n" +
                       $"• Dosage and administration should be determined by your doctor\n" +
                       $"• Report any side effects to your healthcare provider\n\n" +
                       $"**Disclaimer:** This is not medical advice. Please consult with a qualified healthcare professional for personalized medical recommendations.";
            }

            if (queryLower.Contains("symptom") || queryLower.Contains("pain"))
            {
                return $"Regarding symptoms of {query}:\n\n" +
                       $"**Common symptoms may include:**\n" +
                       $"• Pain or discomfort\n" +
                       $"• Changes in bodily function\n" +
                       $"• Fever or inflammation\n" +
                       $"**When to seek care:**\n" +
                       $"• Severe or worsening symptoms\n" +
                       $"• Symptoms accompanied by fever or shortness of breath\n" +
                       $"• Any concerning new symptoms\n\n" +
                       $"**Disclaimer:** This information is not a substitute for professional medical evaluation.";
            }

            return $"Here's information about {query}:\n\n" +
                   $"I'm here to provide general medical information and guidance. " +
                   $"For specific medical advice, please consult with a qualified healthcare professional who can assess your individual situation.\n\n" +
                   $"**Sources:** Medical literature, clinical guidelines, and reputable health websites";
        }

        private List<object> GenerateMockSources(string query)
        {
            return new List<object>
            {
                new { Title = "MedlinePlus - Drug Information", Url = "https://medlineplus.gov", Domain = "medlineplus.gov" },
                new { Title = "CDC - Clinical Guidelines", Url = "https://cdc.gov", Domain = "cdc.gov" },
                new { Title = "WHO - Health Guidelines", Url = "https://who.int", Domain = "who.int" }
            };
        }

        private List<string> GenerateMockSuggestions(string query)
        {
            var queryLower = query.ToLower();
            var suggestions = new List<string>();

            if (queryLower.Contains("drug"))
            {
                suggestions.AddRange(new[]
                {
                    "What are the side effects of this drug?",
                    "What is the recommended dosage?",
                    "Are there any contraindications?",
                    "How should this medication be taken?",
                    "What are the common drug interactions?"
                });
            }

            if (queryLower.Contains("symptom"))
            {
                suggestions.AddRange(new[]
                {
                    "What are the common causes of these symptoms?",
                    "When should I see a doctor?",
                    "What home remedies might help?",
                    "What tests are used for diagnosis?",
                    "How can I prevent these symptoms?"
                });
            }

            if (suggestions.Count == 0)
            {
                suggestions.AddRange(new[]
                {
                    "What are the treatment options?",
                    "What is the prognosis?",
                    "How does this condition progress?",
                    "What lifestyle changes can help?",
                    "Are there any complications to watch for?"
                });
            }

            return suggestions.Take(8).ToList();
        }

        private void StoreConversation(string sessionId, string query, string response)
        {
            if (!_conversations.ContainsKey(sessionId))
            {
                _conversations[sessionId] = new List<object>();
            }

            _conversations[sessionId].Add(new { Role = "user", Content = query, Timestamp = DateTime.UtcNow });
            _conversations[sessionId].Add(new { Role = "assistant", Content = response, Timestamp = DateTime.UtcNow });

            // Keep only last 10 messages per session
            if (_conversations[sessionId].Count > 20)
            {
                _conversations[sessionId] = _conversations[sessionId].TakeLast(20).ToList();
            }
        }

        private string GenerateSessionId()
        {
            return $"session_{Guid.NewGuid():N}[8]";
        }
    }
}

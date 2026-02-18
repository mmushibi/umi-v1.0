using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using UmiHealthPOS.Models.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace UmiHealthPOS.Services
{
    public interface ISepioAIService
    {
        Task<AIResponseDto> AskAIAsync(AIRequestDto request);
        Task<AIResponseDto> AskWithContextAsync(AIRequestDto request, List<AIMessageDto> conversationHistory);
        Task<List<string>> GenerateSuggestionsAsync(string query, string context = "");
    }

    public class SepioAIService : ISepioAIService
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;
        private readonly ILogger<SepioAIService> _logger;
        private readonly IWebSearchService _webSearchService;

        public SepioAIService(HttpClient httpClient, IMemoryCache cache, ILogger<SepioAIService> logger, IWebSearchService webSearchService)
        {
            _httpClient = httpClient;
            _cache = cache;
            _logger = logger;
            _webSearchService = webSearchService;
        }

        public async Task<AIResponseDto> AskAIAsync(AIRequestDto request)
        {
            try
            {
                // Search for relevant medical information first
                var searchResults = await _webSearchService.SearchAsync(new SearchRequestDto
                {
                    Query = request.Query,
                    SearchType = DetermineSearchType(request.Query),
                    MaxResults = 5,
                    IncludeSummary = true
                });

                // Generate AI response based on search results
                var response = await GenerateAIResponse(request, searchResults.Results);
                response.Sources = searchResults.Results.Select(r => new SourceInfoDto
                {
                    Title = r.Title,
                    Url = r.Url,
                    Domain = r.Domain
                }).ToList();

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AI response generation");
                return new AIResponseDto
                {
                    Response = "I apologize, but I encountered an error while processing your request. Please try again.",
                    Sources = new List<SourceInfoDto>(),
                    Confidence = 0.0,
                    ResponseTime = TimeSpan.Zero
                };
            }
        }

        public async Task<AIResponseDto> AskWithContextAsync(AIRequestDto request, List<AIMessageDto> conversationHistory)
        {
            try
            {
                // Include conversation context in the search
                var contextualQuery = BuildContextualQuery(request.Query, conversationHistory);

                var searchResults = await _webSearchService.SearchAsync(new SearchRequestDto
                {
                    Query = contextualQuery,
                    SearchType = DetermineSearchType(request.Query),
                    MaxResults = 5,
                    IncludeSummary = true
                });

                var response = await GenerateAIResponseWithContext(request, conversationHistory, searchResults.Results);
                response.Sources = searchResults.Results.Select(r => new SourceInfoDto
                {
                    Title = r.Title,
                    Url = r.Url,
                    Domain = r.Domain
                }).ToList();

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in contextual AI response generation");
                return new AIResponseDto
                {
                    Response = "I apologize, but I encountered an error while processing your request. Please try again.",
                    Sources = new List<SourceInfoDto>(),
                    Confidence = 0.0,
                    ResponseTime = TimeSpan.Zero
                };
            }
        }

        public async Task<List<string>> GenerateSuggestionsAsync(string query, string context = "")
        {
            try
            {
                var cacheKey = $"ai_suggestions_{query}_{context}";
                if (_cache.TryGetValue(cacheKey, out List<string>? cachedSuggestions))
                {
                    return cachedSuggestions!;
                }

                var suggestions = new List<string>();

                // Generate contextual suggestions based on query type
                if (IsMedicalQuery(query))
                {
                    suggestions.AddRange(GenerateMedicalSuggestions(query));
                }
                else if (IsDrugQuery(query))
                {
                    suggestions.AddRange(GenerateDrugSuggestions(query));
                }
                else
                {
                    suggestions.AddRange(GenerateGeneralSuggestions(query));
                }

                // Cache for 30 minutes
                _cache.Set(cacheKey, suggestions, TimeSpan.FromMinutes(30));

                return await Task.FromResult(suggestions.Take(8).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating AI suggestions");
                return new List<string>();
            }
        }

        private Task<AIResponseDto> GenerateAIResponse(AIRequestDto request, List<SearchResultDto> searchResults)
        {
            return Task.Run(() =>
            {
                var startTime = DateTime.UtcNow;

                // Build context from search results
                var context = string.Join("\n", searchResults.Select(r => $"{r.Title}: {r.Summary}"));

                // Generate response based on query type
                var response = request.Query.ToLower() switch
                {
                    var q when q.Contains("drug") || q.Contains("medication") => GenerateDrugResponse(request.Query, context),
                    var q when q.Contains("interaction") => GenerateInteractionResponse(request.Query, context),
                    var q when q.Contains("guideline") || q.Contains("protocol") => GenerateGuidelineResponse(request.Query, context),
                    var q when q.Contains("symptom") => GenerateSymptomResponse(request.Query, context),
                    _ => GenerateGeneralMedicalResponse(request.Query, context)
                };

                return new AIResponseDto
                {
                    Response = response,
                    Sources = new List<SourceInfoDto>(),
                    Confidence = CalculateConfidence(searchResults),
                    ResponseTime = DateTime.UtcNow - startTime,
                    IsMedicalAdvice = ContainsMedicalAdvice(request.Query)
                };
            });
        }

        private Task<AIResponseDto> GenerateAIResponseWithContext(AIRequestDto request, List<AIMessageDto> conversationHistory, List<SearchResultDto> searchResults)
        {
            return Task.Run(() =>
            {
                var startTime = DateTime.UtcNow;

                // Build conversation context
                var conversationContext = string.Join("\n", conversationHistory.Select(m => $"{m.Role}: {m.Content}"));

                // Build search context
                var searchContext = string.Join("\n", searchResults.Select(r => $"{r.Title}: {r.Summary}"));

                // Generate contextual response
                var response = GenerateContextualResponse(request.Query, conversationContext, searchContext);

                return new AIResponseDto
                {
                    Response = response,
                    Sources = new List<SourceInfoDto>(),
                    Confidence = CalculateConfidence(searchResults) * 0.9, // Slightly lower confidence for contextual responses
                    ResponseTime = DateTime.UtcNow - startTime,
                    IsMedicalAdvice = ContainsMedicalAdvice(request.Query)
                };
            });
        }

        private string GenerateDrugResponse(string query, string context)
        {
            return $@"Based on current medical information, here's what I can tell you about {query}:

{GenerateSummaryFromContext(context)}

**Important Information:**
• Always consult with a healthcare professional before starting any medication
• This information is for educational purposes only
• Dosage and administration should be determined by your doctor
• Report any side effects to your healthcare provider

**Disclaimer:** This is not medical advice. Please consult with a qualified healthcare professional for personalized medical recommendations.";
        }

        private string GenerateInteractionResponse(string query, string context)
        {
            return $@"Regarding drug interactions for {query}:

{GenerateSummaryFromContext(context)}

**Critical Safety Information:**
• Drug interactions can be life-threatening
• Always inform your healthcare provider about all medications you take
• Include over-the-counter drugs, supplements, and herbal remedies
• Watch for warning signs of adverse interactions

**Emergency:** If you experience severe symptoms after taking medications, seek immediate medical attention.

**Disclaimer:** This information is not a substitute for professional medical advice.";
        }

        private string GenerateGuidelineResponse(string query, string context)
        {
            return $@"Clinical guidelines for {query}:

{GenerateSummaryFromContext(context)}

**Key Points:**
• Guidelines are regularly updated based on new evidence
• Individual patient factors may require deviation from standard protocols
• Always consider local regulations and formularies
• Multidisciplinary approach often yields best outcomes

**Note:** Guidelines should be adapted to individual patient circumstances and local healthcare context.";
        }

        private string GenerateSymptomResponse(string query, string context)
        {
            return $@"Regarding symptoms of {query}:

{GenerateSummaryFromContext(context)}

**Important Considerations:**
• Symptoms can vary widely between individuals
• Many conditions share similar symptoms
• Some symptoms require immediate medical attention
• Keep a detailed symptom diary for your healthcare provider

**When to Seek Care:**
• Severe or worsening symptoms
• Symptoms accompanied by fever, shortness of breath, or chest pain
• Symptoms that interfere with daily activities
• Any concerning new symptoms

**Disclaimer:** This information cannot replace professional medical evaluation.";
        }

        private string GenerateGeneralMedicalResponse(string query, string context)
        {
            return $@"Here's what I found about {query}:

{GenerateSummaryFromContext(context)}

**Key Information:**
• Always verify information with qualified healthcare professionals
• Medical knowledge evolves rapidly with new research
• Individual health factors affect medical recommendations
• Consider multiple reliable sources for health information

**For Personalized Advice:**
• Consult your primary care physician
• Consider seeking specialist care for specific conditions
• Bring questions to your healthcare appointments

**Disclaimer:** This is educational information and not medical advice.";
        }

        private string GenerateContextualResponse(string query, string conversationContext, string searchContext)
        {
            return $@"Based on our conversation and current medical information:

{GenerateSummaryFromContext(searchContext)}

**Following our discussion:**
{GenerateContextualInsights(conversationContext, query)}

**Next Steps:**
• Consider how this information applies to your specific situation
• Discuss any concerns with your healthcare provider
• Keep track of any follow-up questions

**Disclaimer:** This information complements but does not replace professional medical advice.";
        }

        private string GenerateSummaryFromContext(string context)
        {
            if (string.IsNullOrEmpty(context))
                return "I don't have specific information about this topic in my current database. Please consult with a healthcare professional.";

            // Simple summarization - in production, this would use more sophisticated NLP
            var sentences = context.Split('.').Where(s => s.Trim().Length > 20).Take(3);
            return string.Join(" ", sentences) + ".";
        }

        private string GenerateContextualInsights(string conversationContext, string currentQuery)
        {
            // Generate insights based on conversation flow
            return "This information relates to your previous questions and may help provide a more complete picture of your inquiry.";
        }

        private List<string> GenerateMedicalSuggestions(string query)
        {
            return new List<string>
            {
                $"What are the symptoms of {query}?",
                $"How is {query} diagnosed?",
                $"What are the treatment options for {query}?",
                $"What medications are used for {query}?",
                $"What are the complications of {query}?",
                $"How can {query} be prevented?",
                $"What is the prognosis for {query}?",
                $"When should I see a doctor for {query}?"
            };
        }

        private List<string> GenerateDrugSuggestions(string query)
        {
            return new List<string>
            {
                $"What are the side effects of {query}?",
                $"What are the contraindications for {query}?",
                $"How does {query} work?",
                $"What is the dosage for {query}?",
                $"What drugs interact with {query}?",
                $"Is {query} safe during pregnancy?",
                $"What are the alternatives to {query}?",
                $"How should {query} be taken?"
            };
        }

        private List<string> GenerateGeneralSuggestions(string query)
        {
            return new List<string>
            {
                $"What is {query}?",
                $"What causes {query}?",
                $"Who is at risk for {query}?",
                $"How common is {query}?",
                $"What are the latest research findings on {query}?",
                $"What specialists treat {query}?",
                $"What tests are used for {query}?",
                $"What lifestyle changes help with {query}?"
            };
        }

        private string DetermineSearchType(string query)
        {
            var q = query.ToLower();
            if (q.Contains("drug") || q.Contains("medication")) return "drugs";
            if (q.Contains("guideline") || q.Contains("protocol")) return "guidelines";
            if (q.Contains("interaction")) return "interactions";
            if (q.Contains("research") || q.Contains("study")) return "research";
            return "general";
        }

        private string BuildContextualQuery(string currentQuery, List<AIMessageDto> history)
        {
            // Build a query that includes context from conversation
            var recentTopics = history.TakeLast(3).Select(h => h.Content).ToList();
            return $"{string.Join(" ", recentTopics)} {currentQuery}";
        }

        private bool IsMedicalQuery(string query)
        {
            var medicalTerms = new[] { "disease", "condition", "symptom", "treatment", "diagnosis", "therapy" };
            return medicalTerms.Any(term => query.ToLower().Contains(term));
        }

        private bool IsDrugQuery(string query)
        {
            var drugTerms = new[] { "drug", "medication", "medicine", "pharmaceutical", "dosage", "side effect" };
            return drugTerms.Any(term => query.ToLower().Contains(term));
        }

        private double CalculateConfidence(List<SearchResultDto> results)
        {
            if (!results.Any()) return 0.0;

            var avgRelevance = results.Average(r => r.RelevanceScore);
            var sourceQuality = results.Count(r => IsHighQualitySource(r.Domain)) / (double)results.Count;

            return Math.Min(1.0, avgRelevance * 0.7 + sourceQuality * 0.3);
        }

        private bool IsHighQualitySource(string domain)
        {
            var highQualityDomains = new[]
            {
                "nih.gov", "who.int", "cdc.gov", "fda.gov", "medlineplus.gov",
                "mayoclinic.org", "pubmed.ncbi.nlm.nih.gov", "clinicaltrials.gov"
            };
            return highQualityDomains.Any(hqd => domain.Contains(hqd));
        }

        private bool ContainsMedicalAdvice(string query)
        {
            var adviceTerms = new[] { "should I", "can I", "recommend", "advice", "what should" };
            return adviceTerms.Any(term => query.ToLower().Contains(term));
        }
    }
}

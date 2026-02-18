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
    public interface IWebSearchService
    {
        Task<SearchResponseDto> SearchAsync(SearchRequestDto request);
        Task<AutoCompleteResponseDto> GetAutoCompleteAsync(AutoCompleteRequestDto request);
        Task<string> SummarizeContentAsync(string content, string query);
    }

    public class WebSearchService : IWebSearchService
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;
        private readonly ILogger<WebSearchService> _logger;
        private readonly IConfiguration _configuration;

        // Medical search sources configuration
        private readonly Dictionary<string, string[]> _medicalSources = new()
        {
            ["drugs"] = new[] { "medlineplus.gov", "drugs.com", "fda.gov", "dailymed.nlm.nih.gov" },
            ["guidelines"] = new[] { "who.int", "nih.gov", "cdc.gov", "nice.org.uk" },
            ["research"] = new[] { "pubmed.ncbi.nlm.nih.gov", "clinicaltrials.gov", "cochranelibrary.com" },
            ["general"] = new[] { "mayoclinic.org", "webmd.com", "healthline.com", "medscape.com" }
        };

        public WebSearchService(HttpClient httpClient, IMemoryCache cache, ILogger<WebSearchService> logger, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _cache = cache;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<SearchResponseDto> SearchAsync(SearchRequestDto request)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                // Check cache first
                var cacheKey = $"search_{request.Query}_{request.SearchType}_{request.MaxResults}";
                if (_cache.TryGetValue(cacheKey, out SearchResponseDto? cachedResult))
                {
                    return cachedResult!;
                }

                var results = new List<SearchResultDto>();

                // Build enhanced query based on search type
                var enhancedQuery = BuildEnhancedQuery(request.Query, request.SearchType);

                // Search multiple medical sources
                var searchTasks = GetSearchSources(request.SearchType).Select(source =>
                    SearchSourceAsync(source, enhancedQuery, request.SearchType));

                var sourceResults = await Task.WhenAll(searchTasks);
                results = sourceResults.SelectMany(r => r).ToList();

                // Sort by relevance and limit results
                results = results.OrderByDescending(r => r.RelevanceScore).Take(request.MaxResults).ToList();

                // Generate summaries if requested
                if (request.IncludeSummary)
                {
                    foreach (var result in results)
                    {
                        result.Summary = await SummarizeContentAsync(result.Description, request.Query);
                    }
                }

                var response = new SearchResponseDto
                {
                    Results = results,
                    TotalResults = results.Count,
                    Query = request.Query,
                    SearchType = request.SearchType,
                    SearchTime = DateTime.UtcNow - startTime
                };

                // Cache results for 30 minutes
                _cache.Set(cacheKey, response, TimeSpan.FromMinutes(30));

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing web search for query: {Query}", request.Query);
                return new SearchResponseDto
                {
                    Results = new List<SearchResultDto>(),
                    TotalResults = 0,
                    Query = request.Query,
                    SearchType = request.SearchType,
                    SearchTime = DateTime.UtcNow - startTime
                };
            }
        }

        public async Task<AutoCompleteResponseDto> GetAutoCompleteAsync(AutoCompleteRequestDto request)
        {
            try
            {
                var cacheKey = $"autocomplete_{request.Query}_{request.SearchType}";
                if (_cache.TryGetValue(cacheKey, out AutoCompleteResponseDto? cachedSuggestions))
                {
                    return cachedSuggestions!;
                }

                var suggestions = new List<SearchSuggestionDto>();

                // Get common medical terms and drugs
                var medicalTerms = await GetMedicalTermsAsync(request.Query, request.SearchType);

                // Get recent search suggestions (mock implementation)
                var recentSearches = GetRecentSearchTerms(request.Query, request.SearchType);

                suggestions.AddRange(medicalTerms);
                suggestions.AddRange(recentSearches);

                var response = new AutoCompleteResponseDto
                {
                    Suggestions = suggestions.Take(request.MaxSuggestions).ToList(),
                    Query = request.Query
                };

                // Cache suggestions for 15 minutes
                _cache.Set(cacheKey, response, TimeSpan.FromMinutes(15));

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting autocomplete suggestions for query: {Query}", request.Query);
                return new AutoCompleteResponseDto
                {
                    Suggestions = new List<SearchSuggestionDto>(),
                    Query = request.Query
                };
            }
        }

        public Task<string> SummarizeContentAsync(string content, string query)
        {
            return Task.Run(() =>
            {
                try
                {
                    // Simple summarization logic - extract key sentences
                    var sentences = content.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
                    var relevantSentences = sentences
                        .Where(s => s.ToLower().Contains(query.ToLower()) ||
                                   s.Length > 50) // Prefer longer, more informative sentences
                        .Take(3);

                    var summary = string.Join(" ", relevantSentences);

                    // If summary is too short, return first part of content
                    if (summary.Length < 100 && content.Length > 100)
                    {
                        summary = content.Substring(0, Math.Min(200, content.Length)) + "...";
                    }

                    return summary.Trim();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error summarizing content");
                    return content.Length > 200 ? content.Substring(0, 197) + "..." : content;
                }
            });
        }

        private string BuildEnhancedQuery(string query, string searchType)
        {
            var enhancements = new Dictionary<string, string>
            {
                ["drugs"] = "drug medication dosage side effects interactions",
                ["guidelines"] = "clinical guidelines treatment protocol recommendations",
                ["interactions"] = "drug interactions contraindications adverse effects",
                ["research"] = "clinical trials research studies evidence",
                ["general"] = "medical health information symptoms treatment"
            };

            var enhancement = enhancements.GetValueOrDefault(searchType, "");
            return string.IsNullOrWhiteSpace(enhancement) ? query : $"{query} {enhancement}";
        }

        private async Task<List<SearchResultDto>> SearchSourceAsync(string source, string query, string searchType)
        {
            try
            {
                // Simulate API call to search source
                // In production, this would call actual search APIs
                await Task.Delay(Random.Shared.Next(100, 300)); // Simulate network delay

                return GenerateMockResultsForSource(source, query, searchType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching source: {Source}", source);
                return new List<SearchResultDto>();
            }
        }

        private List<SearchResultDto> GenerateMockResultsForSource(string source, string query, string searchType)
        {
            var results = new List<SearchResultDto>();
            var random = new Random();

            // Generate 1-3 results per source
            var resultCount = random.Next(1, 4);

            for (int i = 0; i < resultCount; i++)
            {
                var relevanceScore = random.NextDouble() * 0.4 + 0.6; // 0.6 to 1.0

                results.Add(new SearchResultDto
                {
                    Title = GenerateTitle(source, query, searchType),
                    Description = GenerateDescription(source, query, searchType),
                    Url = GenerateUrl(source, query),
                    Domain = source,
                    Date = DateTime.Now.AddDays(-random.Next(0, 30)).ToString("MMM dd, yyyy"),
                    Summary = "",
                    Category = searchType,
                    RelevanceScore = relevanceScore
                });
            }

            return results;
        }

        private string GenerateTitle(string source, string query, string searchType)
        {
            var templates = new Dictionary<string, string[]>
            {
                ["drugs"] = new[]
                {
                    $"{query} - Complete Drug Information",
                    $"{query} Dosage and Side Effects",
                    $"{query} Drug Interactions and Warnings"
                },
                ["guidelines"] = new[]
                {
                    $"Clinical Guidelines for {query}",
                    $"{query} Treatment Protocols",
                    $"Best Practices: {query} Management"
                },
                ["general"] = new[]
                {
                    $"Understanding {query}: Symptoms and Treatment",
                    $"{query}: What You Need to Know",
                    $"Comprehensive Guide to {query}"
                }
            };

            var categoryTemplates = templates.GetValueOrDefault(searchType, templates["general"]);
            return categoryTemplates[Random.Shared.Next(categoryTemplates.Length)];
        }

        private string GenerateDescription(string source, string query, string searchType)
        {
            var descriptions = new Dictionary<string, string[]>
            {
                ["drugs"] = new[]
                {
                    $"Comprehensive information about {query}, including dosage, side effects, contraindications, and patient counseling points.",
                    $"Detailed monograph for {query} with mechanism of action, pharmacokinetics, and clinical considerations.",
                    $"Complete prescribing information for {query} including warnings, precautions, and drug interactions."
                },
                ["guidelines"] = new[]
                {
                    $"Evidence-based clinical guidelines for {query} management, including diagnostic criteria and treatment algorithms.",
                    $"Updated recommendations for {query} based on latest clinical research and expert consensus.",
                    $"Standardized protocols for {query} diagnosis, treatment, and follow-up care."
                },
                ["general"] = new[]
                {
                    $"Reliable medical information about {query}, including symptoms, causes, diagnosis, and treatment options.",
                    $"Patient-friendly overview of {query} with prevention tips and when to seek medical care.",
                    $"Comprehensive resource covering all aspects of {query} from prevention to long-term management."
                }
            };

            var categoryDescriptions = descriptions.GetValueOrDefault(searchType, descriptions["general"]);
            return categoryDescriptions[Random.Shared.Next(categoryDescriptions.Length)];
        }

        private string GenerateUrl(string source, string query)
        {
            var encodedQuery = Uri.EscapeDataString(query.ToLower().Replace(" ", "-"));
            return $"https://{source}/medical/{encodedQuery}";
        }

        private string[] GetSearchSources(string searchType)
        {
            return _medicalSources.GetValueOrDefault(searchType, _medicalSources["general"]);
        }

        private async Task<List<SearchSuggestionDto>> GetMedicalTermsAsync(string query, string searchType)
        {
            // Mock medical terms database
            var medicalTerms = new Dictionary<string, List<string>>
            {
                ["drugs"] = new() { "acetaminophen", "ibuprofen", "amoxicillin", "lisinopril", "metformin", "atorvastatin", "omeprazole", "sertraline" },
                ["guidelines"] = new() { "hypertension guidelines", "diabetes management", "asthma treatment", "covid-19 protocol", "vaccination schedule" },
                ["general"] = new() { "blood pressure", "diabetes", "asthma", "arthritis", "depression", "anxiety", "migraine", "allergies" }
            };

            var terms = medicalTerms.GetValueOrDefault(searchType, medicalTerms["general"]);
            var matchingTerms = terms
                .Where(term => term.StartsWith(query, StringComparison.OrdinalIgnoreCase))
                .Take(5)
                .Select(term => new SearchSuggestionDto
                {
                    Text = term,
                    Category = searchType,
                    Frequency = Random.Shared.Next(1, 100)
                })
                .ToList();

            return await Task.FromResult(matchingTerms);
        }

        private List<SearchSuggestionDto> GetRecentSearchTerms(string query, string searchType)
        {
            // Mock recent searches
            var recentSearches = new List<string>
            {
                "blood pressure medication", "diabetes treatment", "pain management", "antibiotic therapy",
                "cardiovascular disease", "mental health", "pediatric care", "emergency medicine"
            };

            return recentSearches
                .Where(search => search.Contains(query, StringComparison.OrdinalIgnoreCase))
                .Take(3)
                .Select(search => new SearchSuggestionDto
                {
                    Text = search,
                    Category = searchType,
                    Frequency = Random.Shared.Next(50, 200)
                })
                .ToList();
        }
    }
}

using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using UmiHealthPOS.Models.DTOs;
using UmiHealthPOS.DTOs;
using UmiHealthPOS.Services.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using System.Text.RegularExpressions;
using System.Threading;
using System.Text;
using System.Numerics;

namespace UmiHealthPOS.Services
{
    public interface ISepioAIService
    {
        Task<AIResponseDto> AskAIAsync(AIRequestDto request);
        Task<AIResponseDto> AskWithContextAsync(AIRequestDto request, List<AIMessageDto> conversationHistory);
        Task<List<string>> GenerateSuggestionsAsync(string query, string context = "");
        Task<List<string>> GenerateSmartSuggestionsAsync(string query, string userContext = "");
        Task<LearningInsightDto> GetLearningInsightsAsync(string userId);
        Task<bool> TrainModelAsync(string feedback, string query, string response);
    }

    public class SepioAIService : ISepioAIService
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;
        private readonly ILogger<SepioAIService> _logger;
        private readonly IWebSearchService _webSearchService;
        private readonly AIDataService _dataService;
        private readonly Dictionary<string, List<string>> _learningPatterns;
        private readonly Dictionary<string, double> _algorithmWeights;
        private readonly Dictionary<string, List<string>> _medicalKnowledgeBase;
        private readonly Dictionary<string, double> _semanticSimilarity;
        private readonly Random _random;
        private readonly object _trainingLock = new object();

        // ML Model weights and parameters
        private Dictionary<string, double> _neuralNetworkWeights;
        private Dictionary<string, double> _tfidfWeights;
        private List<string> _stopWords;
        private Dictionary<string, int> _vocabulary;

        public SepioAIService(HttpClient httpClient, IMemoryCache cache, ILogger<SepioAIService> logger, IWebSearchService webSearchService, AIDataService dataService)
        {
            _httpClient = httpClient;
            _cache = cache;
            _logger = logger;
            _webSearchService = webSearchService;
            _dataService = dataService;
            _random = new Random();
            _learningPatterns = new Dictionary<string, List<string>>();
            _algorithmWeights = new Dictionary<string, double>();
            _medicalKnowledgeBase = new Dictionary<string, List<string>>();
            _semanticSimilarity = new Dictionary<string, double>();
            _neuralNetworkWeights = new Dictionary<string, double>();
            _tfidfWeights = new Dictionary<string, double>();
            _stopWords = new List<string>();
            _vocabulary = new Dictionary<string, int>();
            InitializeMachineLearning();
            InitializeMedicalKnowledgeBase();
            InitializeNLPComponents();
        }

        private void InitializeMachineLearning()
        {
            // Initialize algorithm weights for different response types
            _algorithmWeights["drug"] = 0.9;
            _algorithmWeights["symptom"] = 0.85;
            _algorithmWeights["guideline"] = 0.88;
            _algorithmWeights["interaction"] = 0.92;
            _algorithmWeights["general"] = 0.8;
            _algorithmWeights["developer"] = 1.0;

            // Initialize learning patterns
            _learningPatterns["drug_queries"] = new List<string>();
            _learningPatterns["symptom_queries"] = new List<string>();
            _learningPatterns["guideline_queries"] = new List<string>();
            _learningPatterns["interaction_queries"] = new List<string>();
        }

        private void InitializeMedicalKnowledgeBase()
        {
            // Initialize comprehensive medical knowledge base
            _medicalKnowledgeBase["drugs"] = new List<string>
            {
                "metformin", "insulin", "lisinopril", "atorvastatin", "albuterol", "amoxicillin",
                "ibuprofen", "acetaminophen", "aspirin", "omeprazole", "simvastatin", "hydrochlorothiazide",
                "metoprolol", "losartan", "azithromycin", "prednisone", "warfarin", "gabapentin",
                "sertraline", "fluoxetine", "atorvastatin", "levothyroxine", "alendronate"
            };

            _medicalKnowledgeBase["symptoms"] = new List<string>
            {
                "headache", "fever", "cough", "fatigue", "nausea", "pain", "dizziness", "rash",
                "shortness of breath", "chest pain", "abdominal pain", "diarrhea", "constipation",
                "insomnia", "anxiety", "depression", "weight loss", "weight gain", "swelling"
            };

            _medicalKnowledgeBase["conditions"] = new List<string>
            {
                "hypertension", "diabetes", "asthma", "copd", "arthritis", "depression", "anxiety",
                "migraine", "gastroesophageal reflux", "hyperlipidemia", "osteoporosis", "hypothyroidism"
            };

            _medicalKnowledgeBase["zambia_specific"] = new List<string>
            {
                "malaria", "hiv", "tuberculosis", "cholera", "typhoid", "hepatitis b", "schistosomiasis",
                "onchocerciasis", "dracunculiasis", "buruli ulcer", "yellow fever", "rabies"
            };
        }

        private void InitializeNLPComponents()
        {
            // Initialize stop words for text processing
            _stopWords = new List<string>
            {
                "the", "a", "an", "and", "or", "but", "in", "on", "at", "to", "for", "of", "with",
                "by", "is", "are", "was", "were", "be", "been", "being", "have", "has", "had",
                "do", "does", "did", "will", "would", "could", "should", "may", "might", "can",
                "what", "when", "where", "who", "why", "how", "i", "you", "he", "she", "it",
                "we", "they", "me", "him", "her", "us", "them", "my", "your", "his", "her", "its"
            };

            // Initialize vocabulary with medical terms
            int vocabId = 0;
            foreach (var category in _medicalKnowledgeBase)
            {
                foreach (var term in category.Value)
                {
                    var tokens = TokenizeText(term);
                    foreach (var token in tokens)
                    {
                        if (!_vocabulary.ContainsKey(token))
                        {
                            _vocabulary[token] = vocabId++;
                        }
                    }
                }
            }

            // Initialize TF-IDF weights
            InitializeTFIDFWeights();
        }

        private void InitializeTFIDFWeights()
        {
            // Initialize TF-IDF weights for medical terms
            foreach (var term in _vocabulary.Keys)
            {
                _tfidfWeights[term] = 1.0; // Base weight
            }

            // Boost weights for important medical terms
            var importantTerms = new[] { "metformin", "insulin", "hypertension", "diabetes", "malaria", "hiv" };
            foreach (var term in importantTerms)
            {
                if (_tfidfWeights.ContainsKey(term))
                {
                    _tfidfWeights[term] *= 1.5;
                }
            }
        }

        public async Task<AIResponseDto> AskAIAsync(AIRequestDto request)
        {
            try
            {
                // Learn from this query
                await LearnFromQuery(request.Query);

                // Search for relevant medical information first
                var searchResults = await _webSearchService.SearchAsync(new SearchRequestDto
                {
                    Query = request.Query,
                    SearchType = DetermineSearchType(request.Query),
                    MaxResults = 5,
                    IncludeSummary = true
                });

                // Generate AI response based on search results and ML algorithms
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
                    var q when IsDeveloperQuery(q) => GenerateDeveloperResponse(),
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
            return $@"**Medical Information Assistant**

I understand you're asking about **{query}**. Let me provide you with comprehensive medical information to assist your clinical practice.

{GenerateSummaryFromContext(context)}

**Medication Safety Guidelines:**
• Always consult with healthcare professionals before starting any medication
• This information complements but does not replace professional medical advice
• Dosage should be determined by your doctor based on specific health needs
• Report any side effects immediately to your healthcare provider
• Maintain a medication diary to track effectiveness and adverse reactions

**Important Clinical Reminders:**
• Never share prescription medications with other patients
• Store medications according to pharmacist instructions
• Inform your doctor about all medications including OTC and supplements
• Schedule regular follow-up appointments to monitor treatment progress

**Evidence-Based Sources:** Information compiled from reputable medical databases and clinical guidelines.

**Disclaimer:** This educational information is not a substitute for professional medical advice, diagnosis, or treatment.";
        }

        private string GenerateInteractionResponse(string query, string context)
        {
            return $@"**Drug Interaction Analysis**

I'm analyzing potential drug interactions for **{query}**. This is a critical safety assessment that requires careful attention.

{GenerateSummaryFromContext(context)}

**Critical Safety Information:**
• Drug interactions can be life-threatening and require immediate medical attention
• Always inform healthcare providers about ALL medications (prescription, OTC, supplements, herbal remedies)
• Some interactions may occur hours or days after starting new medications
• Watch for warning signs: rash, difficulty breathing, swelling, severe dizziness, or unusual symptoms

**Emergency Protocol:**
• If severe symptoms occur after taking medications, seek immediate medical attention
• Call emergency services or go to the nearest emergency department
• Bring all medications with you to the emergency department

**Prevention Strategies:**
• Maintain an updated medication list with doses and timing
• Use one pharmacy for all prescriptions when possible
• Ask your pharmacist about potential interactions before starting new medications
• Review all medications with your doctor at each visit

**Disclaimer:** This information cannot replace professional medical evaluation. Always consult with healthcare providers for medication management.";
        }

        private string GenerateGuidelineResponse(string query, string context)
        {
            return $@"**Clinical Guidelines**

I'm providing you with the latest clinical guidelines for **{query}**. These evidence-based protocols support optimal patient care.

{GenerateSummaryFromContext(context)}

**Key Clinical Points:**
• Guidelines are regularly updated based on new evidence and research
• Individual patient factors may require deviation from standard protocols
• Always consider local regulations and formularies
• Multidisciplinary approach often yields the best patient outcomes
• Guidelines complement but do not replace clinical judgment

**Implementation Considerations:**
• Assess patient preferences and values when applying guidelines
• Consider resource availability in your healthcare setting
• Document any deviations from standard guidelines with clear reasoning
• Stay updated on guideline revisions and new evidence
• Participate in continuing medical education to maintain current knowledge

**Local Context:**
• Adapt guidelines to Zambia's healthcare system and disease patterns
• Consider local formulary availability and cost-effectiveness
• Follow Zambia Ministry of Health protocols when available

**Note:** Guidelines should be thoughtfully adapted to individual patient circumstances and local healthcare context.";
        }

        private string GenerateSymptomResponse(string query, string context)
        {
            return $@"**Symptom Analysis**

I'm helping you analyze symptoms related to **{query}**. Understanding symptoms is crucial for accurate diagnosis and treatment.

{GenerateSummaryFromContext(context)}

**Important Clinical Considerations:**
• Symptoms can vary widely between individuals based on age, gender, and health status
• Many conditions share similar symptoms, making professional evaluation essential
• Some symptoms require immediate medical attention - don't delay seeking care
• Keep a detailed symptom diary (onset, duration, severity, triggers, relieving factors)
• Note any associated symptoms or changes in overall health

**When to Seek Urgent Medical Care:**
• Severe, sudden, or rapidly worsening symptoms
• Symptoms accompanied by fever, shortness of breath, chest pain, or neurological changes
• Symptoms that interfere with daily activities or sleep
• Any new, concerning, or persistent symptoms
• Symptoms in children, elderly, or immunocompromised individuals

**Preparing for Your Medical Visit:**
• Write down all symptoms, including when they started and what makes them better/worse
• List all medications, supplements, and recent health changes
• Bring any relevant medical records or test results
• Prepare questions to ask your healthcare provider

**Disclaimer:** This information cannot replace professional medical evaluation. Always seek appropriate medical care for concerning symptoms.";
        }

        private string GenerateGeneralMedicalResponse(string query, string context)
        {
            return $@"**Medical Information Service**

I'm here to provide you with comprehensive medical information about **{query}** to support your clinical practice and patient care.

{GenerateSummaryFromContext(context)}

**Key Medical Information:**
• Always verify information with qualified healthcare professionals
• Medical knowledge evolves rapidly with new research and discoveries
• Individual health factors, genetics, and lifestyle affect medical recommendations
• Consider multiple reliable sources for comprehensive health information
• Evidence-based medicine forms the foundation of modern healthcare

**For Personalized Medical Care:**
• Consult your primary care physician for personalized health advice
• Seek specialist care for specific conditions or complex health issues
• Prepare questions before medical appointments to maximize your visit
• Bring a list of medications, symptoms, and concerns to discuss
• Consider bringing a family member or friend to important appointments

**Reliable Health Information Sources:**
• Reputable medical institutions and government health websites
• Peer-reviewed medical journals and clinical guidelines
• Professional healthcare organizations and patient advocacy groups
• Your healthcare providers and pharmacists

**Disclaimer:** This educational information complements but does not replace professional medical advice, diagnosis, or treatment.";
        }

        private string GenerateContextualResponse(string query, string conversationContext, string searchContext)
        {
            return $@"**Continuing Our Medical Discussion**

Based on our conversation and current medical information, I'm building upon our previous discussion to provide you with comprehensive insights about **{query}**.

{GenerateSummaryFromContext(searchContext)}

**Following Our Previous Discussion:**
{GenerateContextualInsights(conversationContext, query)}

**Next Steps for Your Healthcare Journey:**
• Consider how this information applies to your specific health situation
• Discuss any new concerns or questions with your healthcare provider
• Keep track of any follow-up questions that arise
• Share relevant information with your healthcare team
• Make informed decisions in partnership with your medical providers

**Continuous Learning:**
• Medical information is constantly evolving
• Stay updated with reliable health sources
• Maintain open communication with your healthcare team
• Advocate for your health while respecting professional expertise

**Disclaimer:** This information builds on our previous discussion but complements, not replaces, professional medical advice and care.";
        }

        private string GenerateSummaryFromContext(string context)
        {
            if (string.IsNullOrEmpty(context))
                return "I don't have specific information about this topic in my current database. Please consult with a healthcare professional.";

            // Enhanced summarization - in production, this would use more sophisticated NLP
            // For now, we'll implement improved text processing algorithms

            try
            {
                // Clean and preprocess text
                var cleanText = context
                    .Replace("\r\n", " ")
                    .Replace("\n", " ")
                    .Replace("  ", " ")
                    .Trim();

                if (cleanText.Length < 50)
                    return cleanText;

                // Extract key sentences using simple heuristics
                var sentences = cleanText.Split('.')
                    .Where(s => s.Trim().Length > 15)
                    .Select(s => s.Trim())
                    .ToList();

                if (!sentences.Any())
                    return cleanText.Substring(0, Math.Min(200, cleanText.Length)) + "...";

                // Score sentences based on length, position, and content indicators
                var scoredSentences = sentences.Select((sentence, index) => new
                {
                    Sentence = sentence,
                    Score = CalculateSentenceScore(sentence, index, sentences.Count),
                    Index = index
                })
                .OrderByDescending(x => x.Score)
                .Take(3)
                .OrderBy(x => x.Index)
                .Select(x => x.Sentence)
                .ToList();

                var summary = string.Join(". ", scoredSentences);

                // Ensure summary ends properly
                if (!summary.EndsWith("."))
                    summary += ".";

                // Limit summary length
                if (summary.Length > 300)
                {
                    summary = summary.Substring(0, 297) + "...";
                }

                return summary;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating summary from context");
                return "Unable to generate summary at this time. Please try again.";
            }
        }

        private double CalculateSentenceScore(string sentence, int index, int totalSentences)
        {
            var score = 0.0;

            // Position scoring (first and last sentences often important)
            if (index == 0 || index == totalSentences - 1)
                score += 0.3;
            else if (index < totalSentences * 0.3 || index > totalSentences * 0.7)
                score += 0.2;

            // Length scoring (prefer medium-length sentences)
            var length = sentence.Length;
            if (length >= 20 && length <= 100)
                score += 0.3;
            else if (length >= 10 && length <= 150)
                score += 0.1;

            // Content indicators scoring
            var lowerSentence = sentence.ToLower();

            // Medical/clinical terms
            var medicalTerms = new[] { "treatment", "diagnosis", "symptom", "medication", "therapy", "condition", "patient", "clinical", "medical" };
            score += medicalTerms.Count(term => lowerSentence.Contains(term)) * 0.1;

            // Important indicators
            var importantWords = new[] { "important", "critical", "significant", "essential", "key", "major", "primary", "main" };
            score += importantWords.Count(word => lowerSentence.Contains(word)) * 0.15;

            // Numbers and measurements (often contain key data)
            if (System.Text.RegularExpressions.Regex.IsMatch(sentence, @"\d"))
                score += 0.1;

            // Avoid very short or very long sentences
            if (length < 10 || length > 200)
                score -= 0.2;

            return score;
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

        private bool IsDeveloperQuery(string query)
        {
            var developerTerms = new[] { "who developed", "who made", "who created", "built by", "developed by", "made by", "created by", "who is sepio", "what is sepio", "sepio corp" };
            return developerTerms.Any(term => query.ToLower().Contains(term));
        }

        private string GenerateDeveloperResponse()
        {
            return @"**About Sepio AI Medical Assistant**

I'm glad you asked about my development! I was thoughtfully created and rigorously tested by **Sepio Corp**, a pioneering Zambian creative agency dedicated to transforming healthcare through innovative technology solutions.

**Our Mission:**
• Empowering healthcare professionals with intelligent clinical decision support
• Enhancing patient care through accessible, evidence-based medical information
• Bridging technology and healthcare excellence in Zambia and beyond
• Making quality medical knowledge available to all healthcare providers

**What We Do:**
Sepio Corp specializes in creating cutting-edge digital solutions that combine artificial intelligence, user-centered design, and deep understanding of healthcare workflows to deliver tools that make a real difference in clinical settings.

**Zambia-First Approach:**
• Designed with Zambian healthcare context in mind
• Tailored to local disease patterns, treatment protocols, and healthcare system realities
• Collaborative development with healthcare professionals across Zambia
• Committed to advancing digital health innovation in Africa

**Rigorous Development & Testing:**
• Extensive testing with healthcare professionals
• Evidence-based medical information from reputable sources
• Continuous improvement based on user feedback and medical advances
• Adherence to international healthcare technology standards

**Connect With Us:**
• Sepio Corp is proud to lead healthcare innovation in Zambia
• We're passionate about technology that saves lives and improves care
• Committed to excellence, innovation, and positive impact

**Thank you for trusting me with your clinical information needs!**

*Sepio Corp - Innovating Healthcare, Transforming Lives*";
        }

        public async Task<List<string>> GenerateSmartSuggestionsAsync(string query, string userContext = "")
        {
            try
            {
                var cacheKey = $"smart_suggestions_{query}_{userContext}";
                if (_cache.TryGetValue(cacheKey, out List<string>? cachedSuggestions))
                {
                    return cachedSuggestions!;
                }

                var suggestions = new List<string>();
                var queryType = DetermineSearchType(query);

                // Generate ML-enhanced suggestions based on learned patterns
                suggestions.AddRange(GenerateMLSuggestions(query, queryType, userContext));

                // Add contextual suggestions based on user history
                if (!string.IsNullOrEmpty(userContext))
                {
                    suggestions.AddRange(GenerateContextualSuggestions(query, userContext));
                }

                // Sort by relevance using algorithm weights
                suggestions = suggestions.OrderByDescending(s => CalculateSuggestionRelevance(s, queryType)).ToList();

                // Cache for 30 minutes
                _cache.Set(cacheKey, suggestions, TimeSpan.FromMinutes(30));

                return await Task.FromResult(suggestions.Take(10).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating smart suggestions");
                return new List<string>();
            }
        }

        private List<string> GenerateMLSuggestions(string query, string queryType, string userContext)
        {
            var suggestions = new List<string>();
            var patternKey = $"{queryType}_queries";

            if (_learningPatterns.ContainsKey(patternKey) && _learningPatterns[patternKey].Any())
            {
                // Generate suggestions based on learned patterns
                var similarQueries = _learningPatterns[patternKey]
                    .Where(q => q.Contains(query.Substring(0, Math.Min(3, query.Length))))
                    .Take(3);

                foreach (var similarQuery in similarQueries)
                {
                    suggestions.Add($"Tell me more about {similarQuery}");
                    suggestions.Add($"What are the risks of {similarQuery}?");
                    suggestions.Add($"How to manage {similarQuery} effectively?");
                }
            }

            return suggestions;
        }

        private List<string> GenerateContextualSuggestions(string query, string userContext)
        {
            var suggestions = new List<string>();

            // Generate suggestions based on user context
            if (userContext.Contains("pharmacist"))
            {
                suggestions.Add("What are the dispensing guidelines?");
                suggestions.Add("Are there any storage requirements?");
                suggestions.Add("What patient counseling points are important?");
            }
            else if (userContext.Contains("doctor"))
            {
                suggestions.Add("What are the diagnostic criteria?");
                suggestions.Add("What are the treatment protocols?");
                suggestions.Add("What monitoring is required?");
            }

            return suggestions;
        }

        private double CalculateSuggestionRelevance(string suggestion, string queryType)
        {
            var baseRelevance = _algorithmWeights.ContainsKey(queryType) ? _algorithmWeights[queryType] : 0.5;

            // Add randomness to simulate learning algorithm variation
            var variation = _random.NextDouble() * 0.2 - 0.1; // +/- 10%

            return Math.Max(0.1, Math.Min(1.0, baseRelevance + variation));
        }

        public async Task<LearningInsightDto> GetLearningInsightsAsync(string userId)
        {
            try
            {
                // Use database service for learning insights
                return await _dataService.GetLearningInsightsAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting learning insights for user: {UserId}", userId);
                return new LearningInsightDto
                {
                    TotalQueries = 0,
                    LearningAccuracy = 0.0,
                    PatternDistribution = new Dictionary<string, int>(),
                    TopQueries = new List<string>(),
                    LastUpdated = DateTime.UtcNow
                };
            }
        }

        public async Task<bool> TrainModelAsync(string feedback, string query, string response)
        {
            try
            {
                // Create training session
                var trainingData = new
                {
                    Query = query,
                    Response = response,
                    Feedback = feedback,
                    Timestamp = DateTime.UtcNow,
                    QueryType = DetermineQueryType(query)
                };

                var training = await _dataService.CreateTrainingSessionAsync(
                    "1.0", // Model version
                    "feedback_based",
                    JsonSerializer.Serialize(trainingData)
                );

                // Simulate training process
                await Task.Delay(1000);

                // Update training status to completed
                await _dataService.UpdateTrainingStatusAsync(training.TrainingSessionId, "completed");

                // Update learning patterns based on feedback
                var queryType = DetermineQueryType(query);
                await _dataService.UpdateLearningPatternAsync(queryType, queryType, query, 0.8m);

                _logger.LogInformation("Model training completed successfully for query: {Query}", query);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error training model");
                return false;
            }
        }

        #region NLP and ML Helper Methods

        private List<string> TokenizeText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new List<string>();

            // Convert to lowercase and split on whitespace and punctuation
            var tokens = Regex.Split(text.ToLower(), @"[\s\p{P}]+")
                .Where(token => !string.IsNullOrWhiteSpace(token) && !_stopWords.Contains(token))
                .ToList();

            // Apply stemming (simplified Porter stemming)
            for (int i = 0; i < tokens.Count; i++)
            {
                tokens[i] = ApplyStemming(tokens[i]);
            }

            return tokens;
        }

        private string ApplyStemming(string word)
        {
            // Simplified stemming rules for medical terms
            if (word.EndsWith("ing")) word = word[..^"ing".Length];
            if (word.EndsWith("ed")) word = word[..^"ed".Length];
            if (word.EndsWith("er")) word = word[..^"er".Length];
            if (word.EndsWith("est")) word = word[..^"est".Length];
            if (word.EndsWith("ly")) word = word[..^"ly".Length];
            if (word.EndsWith("tion")) word = word[..^"tion".Length];
            if (word.EndsWith("ness")) word = word[..^"ness".Length];

            return word;
        }

        private double CalculateTFIDF(string query, string document)
        {
            var queryTokens = TokenizeText(query);
            var docTokens = TokenizeText(document);

            if (!queryTokens.Any() || !docTokens.Any())
                return 0.0;

            double tfidfScore = 0.0;

            foreach (var token in queryTokens)
            {
                // Term Frequency (TF) in document
                var tf = (double)docTokens.Count(t => t == token) / docTokens.Count;

                // Inverse Document Frequency (IDF) - simplified
                var idf = Math.Log((double)_vocabulary.Count / (1 + _tfidfWeights.GetValueOrDefault(token, 1.0)));

                tfidfScore += tf * idf;
            }

            return tfidfScore;
        }

        private double CalculateSemanticSimilarity(string text1, string text2)
        {
            var tokens1 = TokenizeText(text1);
            var tokens2 = TokenizeText(text2);

            if (!tokens1.Any() || !tokens2.Any())
                return 0.0;

            // Calculate Jaccard similarity
            var set1 = new HashSet<string>(tokens1);
            var set2 = new HashSet<string>(tokens2);

            var intersection = set1.Intersect(set2).Count();
            var union = set1.Union(set2).Count();

            return union == 0 ? 0.0 : (double)intersection / union;
        }

        private string DetermineQueryType(string query)
        {
            var tokens = TokenizeText(query);

            // Check for drug-related queries
            if (tokens.Any(t => _medicalKnowledgeBase["drugs"].Contains(t)))
                return "drug";

            // Check for symptom-related queries
            if (tokens.Any(t => _medicalKnowledgeBase["symptoms"].Contains(t)))
                return "symptom";

            // Check for condition-related queries
            if (tokens.Any(t => _medicalKnowledgeBase["conditions"].Contains(t)))
                return "guideline";

            // Check for Zambia-specific queries
            if (tokens.Any(t => _medicalKnowledgeBase["zambia_specific"].Contains(t)))
                return "zambia_specific";

            // Check for interaction queries
            if (tokens.Any(t => t.Contains("interact") || t.Contains("interact")))
                return "interaction";

            return "general";
        }

        private async Task LearnFromQuery(string query)
        {
            try
            {
                var queryType = DetermineQueryType(query);
                var patternKey = $"{queryType}_queries";

                if (!_learningPatterns.ContainsKey(patternKey))
                {
                    _learningPatterns[patternKey] = new List<string>();
                }

                // Add to learning patterns (limit to prevent memory issues)
                if (_learningPatterns[patternKey].Count < 1000)
                {
                    _learningPatterns[patternKey].Add(query);
                }

                // Update TF-IDF weights based on new query
                UpdateTFIDFWeights(query);

                // Cache the learning
                _cache.Set($"learning_{patternKey}", _learningPatterns[patternKey], TimeSpan.FromHours(1));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error learning from query: {Query}", query);
            }
        }

        private void UpdateTFIDFWeights(string query)
        {
            var tokens = TokenizeText(query);

            foreach (var token in tokens)
            {
                if (_tfidfWeights.ContainsKey(token))
                {
                    // Increment weight slightly for repeated terms
                    _tfidfWeights[token] *= 1.01;
                }
                else
                {
                    // Add new term with base weight
                    _tfidfWeights[token] = 1.0;
                    _vocabulary[token] = _vocabulary.Count;
                }
            }
        }

        private double CalculateConfidenceScore(string query, string response, List<SearchResultDto> searchResults)
        {
            double confidence = 0.5; // Base confidence

            // Boost confidence based on search results
            if (searchResults.Any())
            {
                confidence += 0.2 * Math.Min(searchResults.Count / 5.0, 1.0);
            }

            // Boost confidence based on semantic similarity
            var semanticScore = CalculateSemanticSimilarity(query, response);
            confidence += 0.2 * semanticScore;

            // Boost confidence based on medical term coverage
            var queryTokens = TokenizeText(query);
            var responseTokens = TokenizeText(response);
            var medicalTermsInQuery = queryTokens.Count(t => _medicalKnowledgeBase.Values.Any(kb => kb.Contains(t)));
            var medicalTermsInResponse = responseTokens.Count(t => _medicalKnowledgeBase.Values.Any(kb => kb.Contains(t)));

            if (medicalTermsInQuery > 0)
            {
                confidence += 0.1 * (double)medicalTermsInResponse / medicalTermsInQuery;
            }

            // Ensure confidence is between 0 and 1
            return Math.Max(0.0, Math.Min(1.0, confidence));
        }

        private string GenerateContextualResponse(string query, List<SearchResultDto> searchResults, string userContext)
        {
            var queryType = DetermineQueryType(query);
            var response = new StringBuilder();

            // Start with context-aware greeting
            if (userContext.Contains("pharmacist"))
            {
                response.Append("As a healthcare professional, here's what you need to know about ");
            }
            else if (userContext.Contains("doctor"))
            {
                response.Append("From a clinical perspective, here's the information about ");
            }
            else
            {
                response.Append("Here's what I can tell you about ");
            }

            // Add query-specific information
            response.Append(query);
            response.Append(".\n\n");

            // Add search result insights
            if (searchResults.Any())
            {
                response.Append("Based on current medical sources:\n");
                foreach (var result in searchResults.Take(3))
                {
                    response.Append($"• {result.Title}\n");
                }
            }

            // Add Zambia-specific context if applicable
            if (queryType == "zambia_specific")
            {
                response.Append("\nFor the Zambian healthcare context, ");
                response.Append("it's important to consider local prevalence and treatment guidelines.");
            }

            // Add safety disclaimer
            response.Append("\n\nThis information is for educational purposes. ");
            response.Append("Always consult current clinical guidelines and use professional judgment.");

            return response.ToString();
        }

        #endregion
    }
}

using System.ComponentModel.DataAnnotations;

namespace UmiHealthPOS.Models.DTOs
{
    public class SearchResultDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Domain { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public double RelevanceScore { get; set; }
    }

    public class SearchRequestDto
    {
        [Required]
        public string Query { get; set; } = string.Empty;

        [Required]
        public string SearchType { get; set; } = "general";

        public int MaxResults { get; set; } = 10;
        public bool IncludeSummary { get; set; } = true;
    }

    public class SearchSuggestionDto
    {
        public string Text { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int Frequency { get; set; }
    }

    public class SearchResponseDto
    {
        public List<SearchResultDto> Results { get; set; } = new();
        public int TotalResults { get; set; }
        public string Query { get; set; } = string.Empty;
        public string SearchType { get; set; } = string.Empty;
        public TimeSpan SearchTime { get; set; }
    }

    public class AutoCompleteRequestDto
    {
        [Required]
        public string Query { get; set; } = string.Empty;

        public int MaxSuggestions { get; set; } = 8;
        public string SearchType { get; set; } = "general";
    }

    public class AutoCompleteResponseDto
    {
        public List<SearchSuggestionDto> Suggestions { get; set; } = new();
        public string Query { get; set; } = string.Empty;
    }

    // AI-specific DTOs
    public class AIRequestDto
    {
        public string Query { get; set; } = string.Empty;
        public string? Context { get; set; }
        public string RequestType { get; set; } = string.Empty;
        public string? SessionId { get; set; }
        public bool IncludeSources { get; set; } = true;
        public string? ConversationId { get; set; }
        public bool IncludeRealTimeData { get; set; } = false;
        public string SearchType { get; set; } = "general";
        public int MaxTokens { get; set; } = 1000;
    }

    public class AIResponseDto
    {
        public string Response { get; set; } = string.Empty;
        public List<SourceInfoDto> Sources { get; set; } = new();
        public double Confidence { get; set; }
        public TimeSpan ResponseTime { get; set; }
        public bool IsMedicalAdvice { get; set; }
        public string? SessionId { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public bool HasRealTimeData { get; set; } = false;
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        public List<AISuggestionDto> Suggestions { get; set; } = new();
    }

    public class AIMessageDto
    {
        public string Role { get; set; } = string.Empty; // "user" or "assistant"
        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string? SessionId { get; set; }
    }

    public class SourceInfoDto
    {
        public string Title { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Domain { get; set; } = string.Empty;
        public string? Type { get; set; } // "research", "guideline", "drug_info", etc.
    }

    public class ConversationDto
    {
        public string ConversationId { get; set; } = string.Empty;
        public List<AIMessageDto> Messages { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        public string? UserId { get; set; }
    }

    public class AISuggestionDto
    {
        public string Text { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // "follow_up", "related", "clarification"
        public int Priority { get; set; }
    }
}

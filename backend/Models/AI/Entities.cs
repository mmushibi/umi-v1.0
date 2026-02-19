using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UmiHealthPOS.Models.AI
{
    /// <summary>
    /// AI Conversation Session Entity
    /// Tracks individual user conversations with the AI
    /// </summary>
    [Table("AI_ConversationSessions")]
    public class AIConversationSession
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(64)]
        [Column(TypeName = "varchar(64)")]
        public string SessionId { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string TenantId { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string UserRole { get; set; } = string.Empty;

        public DateTime StartedAt { get; set; } = DateTime.UtcNow;

        public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;

        public int MessageCount { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        [Column(TypeName = "jsonb")]
        public string? Metadata { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<AIMessage> Messages { get; set; } = new List<AIMessage>();
    }

    /// <summary>
    /// AI Message Entity
    /// Stores individual messages in conversations
    /// </summary>
    [Table("AI_Messages")]
    public class AIMessage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(64)]
        [Column(TypeName = "varchar(64)")]
        public string SessionId { get; set; } = string.Empty;

        [Required]
        [StringLength(64)]
        [Column(TypeName = "varchar(64)")]
        public string MessageId { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Role { get; set; } = string.Empty; // 'user' or 'assistant'

        [Required]
        public string Content { get; set; } = string.Empty;

        [StringLength(20)]
        public string? MessageType { get; set; } // 'user' or 'assistant'

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public int? ProcessingTimeMs { get; set; }

        [StringLength(1000)]
        public string? Context { get; set; }

        [Column(TypeName = "jsonb")]
        public string? Metadata { get; set; }

        [StringLength(50)]
        public string? QueryType { get; set; }

        [Column(TypeName = "decimal(3,2)")]
        public decimal? Confidence { get; set; }

        [Column(TypeName = "interval")]
        public TimeSpan? ResponseTime { get; set; }

        public int TokensUsed { get; set; } = 0;

        [Column(TypeName = "jsonb")]
        public string? Sources { get; set; }

        [Range(1, 5)]
        public int? FeedbackRating { get; set; }

        public string? FeedbackText { get; set; }

        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("SessionId")]
        public virtual AIConversationSession Session { get; set; } = null!;
    }

    /// <summary>
    /// AI Learning Pattern Entity
    /// Tracks learned patterns from user queries
    /// </summary>
    [Table("AI_LearningPatterns")]
    public class AILearningPattern
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string PatternType { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string PatternKey { get; set; } = string.Empty;

        [Required]
        public string QueryText { get; set; } = string.Empty;

        public int Frequency { get; set; } = 1;

        public DateTime LastSeen { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "decimal(3,2)")]
        public decimal SuccessRate { get; set; } = 0.0m;

        [Column(TypeName = "decimal(3,2)")]
        public decimal AverageConfidence { get; set; } = 0.0m;

        [Column(TypeName = "jsonb")]
        public string? ContextData { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// AI Model Training Entity
    /// Tracks model training sessions and results
    /// </summary>
    [Table("AI_ModelTraining")]
    public class AIModelTraining
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(64)]
        [Column(TypeName = "varchar(64)")]
        public string TrainingSessionId { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string ModelVersion { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string TrainingType { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "jsonb")]
        public string TrainingData { get; set; } = string.Empty;

        [Column(TypeName = "jsonb")]
        public string? FeedbackData { get; set; }

        [Column(TypeName = "jsonb")]
        public string? PerformanceMetrics { get; set; }

        [StringLength(50)]
        public string TrainingStatus { get; set; } = "pending";

        public DateTime StartedAt { get; set; } = DateTime.UtcNow;

        public DateTime? CompletedAt { get; set; }

        public string? ErrorText { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// AI User Feedback Entity
    /// Stores user feedback on AI responses
    /// </summary>
    [Table("AI_UserFeedback")]
    public class AIUserFeedback
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(64)]
        [Column(TypeName = "varchar(64)")]
        public string SessionId { get; set; } = string.Empty;

        [Required]
        [StringLength(64)]
        [Column(TypeName = "varchar(64)")]
        public string MessageId { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string FeedbackType { get; set; } = string.Empty;

        [Range(1, 5)]
        public int Rating { get; set; }

        public string? FeedbackText { get; set; }

        public string? QueryText { get; set; }

        public string? ResponseText { get; set; }

        public string? ImprovementSuggestions { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// AI Knowledge Base Entity
    /// Stores medical knowledge and reference information
    /// </summary>
    [Table("AI_KnowledgeBase")]
    public class AIKnowledgeBase
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Category { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string Term { get; set; } = string.Empty;

        public string? Definition { get; set; }

        [Column(TypeName = "jsonb")]
        public string? Context { get; set; }

        public string? SourceUrl { get; set; }

        public string? SourceTitle { get; set; }

        [Column(TypeName = "decimal(3,2)")]
        public decimal ReliabilityScore { get; set; } = 1.0m;

        public DateTime LastVerified { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// AI Semantic Cache Entity
    /// Caches semantic similarity calculations
    /// </summary>
    [Table("AI_SemanticCache")]
    public class AISemanticCache
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(128)]
        [Column(TypeName = "varchar(128)")]
        public string QueryHash { get; set; } = string.Empty;

        [Required]
        public string QueryText { get; set; } = string.Empty;

        [Column(TypeName = "jsonb")]
        public string? SimilarQueries { get; set; }

        public DateTime CachedAt { get; set; } = DateTime.UtcNow;

        public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddDays(7);

        public int HitCount { get; set; } = 0;
    }

    /// <summary>
    /// AI Performance Metrics Entity
    /// Tracks daily performance statistics
    /// </summary>
    [Table("AI_PerformanceMetrics")]
    public class AIPerformanceMetrics
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public DateTime MetricDate { get; set; } = DateTime.Today;

        public int TotalQueries { get; set; } = 0;

        [Column(TypeName = "decimal(8,2)")]
        public decimal AverageResponseTime { get; set; } = 0.0m;

        [Column(TypeName = "decimal(3,2)")]
        public decimal AverageConfidence { get; set; } = 0.0m;

        [Column(TypeName = "decimal(3,2)")]
        public decimal SuccessRate { get; set; } = 0.0m;

        [Column(TypeName = "jsonb")]
        public string? TopQueryTypes { get; set; }

        [Column(TypeName = "decimal(3,2)")]
        public decimal UserSatisfaction { get; set; } = 0.0m;

        [StringLength(50)]
        public string? ModelVersion { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// AI Vocabulary Weights Entity
    /// Stores TF-IDF weights for vocabulary terms
    /// </summary>
    [Table("AI_VocabularyWeights")]
    public class AIVocabularyWeight
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(255)]
        public string Term { get; set; } = string.Empty;

        [Column(TypeName = "decimal(8,4)")]
        public decimal Weight { get; set; } = 1.0m;

        public int Frequency { get; set; } = 1;

        [StringLength(50)]
        public string? Category { get; set; }

        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

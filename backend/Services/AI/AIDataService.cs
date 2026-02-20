using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UmiHealthPOS.Data;
using UmiHealthPOS.Models.AI;
using UmiHealthPOS.DTOs;

namespace UmiHealthPOS.Services.AI
{
    /// <summary>
    /// AI Data Service - Handles database persistence for AI learning and analytics
    /// </summary>
    public class AIDataService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AIDataService> _logger;

        public AIDataService(ApplicationDbContext context, ILogger<AIDataService> logger)
        {
            _context = context;
            _logger = logger;
        }

        #region Conversation Session Management

        /// <summary>
        /// Create or get a conversation session
        /// </summary>
        public async Task<AIConversationSession> GetOrCreateSessionAsync(string sessionId, string userId, string tenantId, string userRole)
        {
            try
            {
                var session = await _context.AIConversationSessions
                    .FirstOrDefaultAsync(s => s.SessionId == sessionId);

                if (session == null)
                {
                    session = new AIConversationSession
                    {
                        SessionId = sessionId,
                        UserId = userId,
                        TenantId = tenantId,
                        UserRole = userRole,
                        StartedAt = DateTime.UtcNow,
                        LastActivityAt = DateTime.UtcNow,
                        IsActive = true
                    };

                    _context.AIConversationSessions.Add(session);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    // Update last activity
                    session.LastActivityAt = DateTime.UtcNow;
                    session.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }

                return session;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating/getting conversation session: {SessionId}", sessionId);
                throw;
            }
        }

        /// <summary>
        /// Save a message to the database
        /// </summary>
        public async Task<AIMessage> SaveMessageAsync(AIMessage message)
        {
            try
            {
                _context.AIMessages.Add(message);
                await _context.SaveChangesAsync();
                return message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving AI message: {MessageId}", message.MessageId);
                throw;
            }
        }

        /// <summary>
        /// Get conversation history for a session
        /// </summary>
        public async Task<List<AIMessage>> GetConversationHistoryAsync(string sessionId, int limit = 50)
        {
            try
            {
                return await _context.AIMessages
                    .Where(m => m.SessionId == sessionId)
                    .OrderBy(m => m.CreatedAt)
                    .Take(limit)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting conversation history: {SessionId}", sessionId);
                return new List<AIMessage>();
            }
        }

        #endregion

        #region Learning Pattern Management

        /// <summary>
        /// Update learning pattern frequency
        /// </summary>
        public async Task UpdateLearningPatternAsync(string patternType, string patternKey, string queryText, decimal confidence)
        {
            try
            {
                var pattern = await _context.AILearningPatterns
                    .FirstOrDefaultAsync(p => p.PatternType == patternType && p.PatternKey == patternKey);

                if (pattern == null)
                {
                    pattern = new AILearningPattern
                    {
                        PatternType = patternType,
                        PatternKey = patternKey,
                        QueryText = queryText,
                        Frequency = 1,
                        SuccessRate = confidence,
                        AverageConfidence = confidence,
                        LastSeen = DateTime.UtcNow
                    };
                    _context.AILearningPatterns.Add(pattern);
                }
                else
                {
                    pattern.Frequency++;
                    pattern.LastSeen = DateTime.UtcNow;
                    pattern.UpdatedAt = DateTime.UtcNow;

                    // Update running averages
                    pattern.AverageConfidence = (pattern.AverageConfidence * (pattern.Frequency - 1) + confidence) / pattern.Frequency;
                    pattern.SuccessRate = Math.Max(0, Math.Min(1, confidence)); // Simplified success rate
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating learning pattern: {PatternType}/{PatternKey}", patternType, patternKey);
            }
        }

        /// <summary>
        /// Get top learning patterns by frequency
        /// </summary>
        public async Task<List<AILearningPattern>> GetTopLearningPatternsAsync(string patternType, int limit = 10)
        {
            try
            {
                return await _context.AILearningPatterns
                    .Where(p => p.PatternType == patternType)
                    .OrderByDescending(p => p.Frequency)
                    .Take(limit)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top learning patterns: {PatternType}", patternType);
                return new List<AILearningPattern>();
            }
        }

        #endregion

        #region User Feedback Management

        /// <summary>
        /// Save user feedback
        /// </summary>
        public async Task<AIUserFeedback> SaveFeedbackAsync(AIUserFeedback feedback)
        {
            try
            {
                _context.AIUserFeedback.Add(feedback);
                await _context.SaveChangesAsync();
                return feedback;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving user feedback: {MessageId}", feedback.MessageId);
                throw;
            }
        }

        /// <summary>
        /// Get feedback statistics for a user
        /// </summary>
        public async Task<object> GetUserFeedbackStatsAsync(string userId)
        {
            try
            {
                var feedback = await _context.AIUserFeedback
                    .Where(f => f.UserId == userId)
                    .ToListAsync();

                if (!feedback.Any())
                {
                    return new
                    {
                        TotalFeedback = 0,
                        AverageRating = 0.0,
                        SatisfactionRate = 0.0
                    };
                }

                return new
                {
                    TotalFeedback = feedback.Count,
                    AverageRating = feedback.Average(f => f.Rating),
                    SatisfactionRate = feedback.Count(f => f.Rating >= 4) * 100.0 / feedback.Count
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user feedback stats: {UserId}", userId);
                return new
                {
                    TotalFeedback = 0,
                    AverageRating = 0.0,
                    SatisfactionRate = 0.0
                };
            }
        }

        #endregion

        #region Model Training Management

        /// <summary>
        /// Create a model training session
        /// </summary>
        public async Task<AIModelTraining> CreateTrainingSessionAsync(string modelVersion, string trainingType, string trainingData)
        {
            try
            {
                var training = new AIModelTraining
                {
                    TrainingSessionId = Guid.NewGuid().ToString("N")[..16],
                    ModelVersion = modelVersion,
                    TrainingType = trainingType,
                    TrainingData = trainingData,
                    TrainingStatus = "pending",
                    StartedAt = DateTime.UtcNow
                };

                _context.AIModelTraining.Add(training);
                await _context.SaveChangesAsync();
                return training;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating training session: {ModelVersion}/{TrainingType}", modelVersion, trainingType);
                throw;
            }
        }

        /// <summary>
        /// Update training session status
        /// </summary>
        public async Task UpdateTrainingStatusAsync(string sessionId, string status, string? errorText = null)
        {
            try
            {
                var training = await _context.AIModelTraining
                    .FirstOrDefaultAsync(t => t.TrainingSessionId == sessionId);

                if (training != null)
                {
                    training.TrainingStatus = status;
                    if (status == "completed")
                    {
                        training.CompletedAt = DateTime.UtcNow;
                    }
                    if (!string.IsNullOrEmpty(errorText))
                    {
                        training.ErrorText = errorText;
                    }
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating training status: {SessionId}/{Status}", sessionId, status);
            }
        }

        #endregion

        #region Knowledge Base Management

        /// <summary>
        /// Get knowledge base entries by category
        /// </summary>
        public async Task<List<AIKnowledgeBase>> GetKnowledgeBaseAsync(string category)
        {
            try
            {
                return await _context.AIKnowledgeBase
                    .Where(k => k.Category == category && k.IsActive)
                    .OrderBy(k => k.Term)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting knowledge base: {Category}", category);
                return new List<AIKnowledgeBase>();
            }
        }

        /// <summary>
        /// Add or update knowledge base entry
        /// </summary>
        public async Task<AIKnowledgeBase> UpsertKnowledgeBaseAsync(AIKnowledgeBase entry)
        {
            try
            {
                var existing = await _context.AIKnowledgeBase
                    .FirstOrDefaultAsync(k => k.Category == entry.Category && k.Term == entry.Term);

                if (existing == null)
                {
                    entry.CreatedAt = DateTime.UtcNow;
                    entry.UpdatedAt = DateTime.UtcNow;
                    _context.AIKnowledgeBase.Add(entry);
                }
                else
                {
                    existing.Definition = entry.Definition;
                    existing.Context = entry.Context;
                    existing.SourceUrl = entry.SourceUrl;
                    existing.SourceTitle = entry.SourceTitle;
                    existing.ReliabilityScore = entry.ReliabilityScore;
                    existing.LastVerified = DateTime.UtcNow;
                    existing.UpdatedAt = DateTime.UtcNow;
                    existing.IsActive = entry.IsActive;
                }

                await _context.SaveChangesAsync();
                return existing ?? entry;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error upserting knowledge base entry: {Category}/{Term}", entry.Category, entry.Term);
                throw;
            }
        }

        #endregion

        #region Performance Metrics

        /// <summary>
        /// Record daily performance metrics
        /// </summary>
        public async Task RecordPerformanceMetricsAsync(AIPerformanceMetrics metrics)
        {
            try
            {
                var existing = await _context.AIPerformanceMetrics
                    .FirstOrDefaultAsync(m => m.MetricDate.Date == metrics.MetricDate.Date && m.ModelVersion == metrics.ModelVersion);

                if (existing == null)
                {
                    _context.AIPerformanceMetrics.Add(metrics);
                }
                else
                {
                    existing.TotalQueries = metrics.TotalQueries;
                    existing.AverageResponseTime = metrics.AverageResponseTime;
                    existing.AverageConfidence = metrics.AverageConfidence;
                    existing.SuccessRate = metrics.SuccessRate;
                    existing.TopQueryTypes = metrics.TopQueryTypes;
                    existing.UserSatisfaction = metrics.UserSatisfaction;
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording performance metrics: {Date}/{ModelVersion}", metrics.MetricDate, metrics.ModelVersion);
            }
        }

        /// <summary>
        /// Get performance metrics for a date range
        /// </summary>
        public async Task<List<AIPerformanceMetrics>> GetPerformanceMetricsAsync(DateTime startDate, DateTime endDate, string? modelVersion = null)
        {
            try
            {
                var query = _context.AIPerformanceMetrics
                    .Where(m => m.MetricDate >= startDate && m.MetricDate <= endDate);

                if (!string.IsNullOrEmpty(modelVersion))
                {
                    query = query.Where(m => m.ModelVersion == modelVersion);
                }

                return await query
                    .OrderByDescending(m => m.MetricDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting performance metrics: {StartDate} - {EndDate}", startDate, endDate);
                return new List<AIPerformanceMetrics>();
            }
        }

        #endregion

        #region Vocabulary Management

        /// <summary>
        /// Update vocabulary weight
        /// </summary>
        public async Task UpdateVocabularyWeightAsync(string term, decimal weight, string? category = null)
        {
            try
            {
                var vocab = await _context.AIVocabularyWeights
                    .FirstOrDefaultAsync(v => v.Term == term);

                if (vocab == null)
                {
                    vocab = new AIVocabularyWeight
                    {
                        Term = term,
                        Weight = weight,
                        Frequency = 1,
                        Category = category,
                        LastUpdated = DateTime.UtcNow
                    };
                    _context.AIVocabularyWeights.Add(vocab);
                }
                else
                {
                    vocab.Weight = weight;
                    vocab.Frequency++;
                    vocab.Category = category ?? vocab.Category;
                    vocab.LastUpdated = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating vocabulary weight: {Term}", term);
            }
        }

        /// <summary>
        /// Get top vocabulary terms by weight
        /// </summary>
        public async Task<List<AIVocabularyWeight>> GetTopVocabularyTermsAsync(string? category = null, int limit = 100)
        {
            try
            {
                var query = _context.AIVocabularyWeights.AsQueryable();

                if (!string.IsNullOrEmpty(category))
                {
                    query = query.Where(v => v.Category == category);
                }

                return await query
                    .OrderByDescending(v => v.Weight)
                    .Take(limit)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top vocabulary terms: {Category}", category);
                return new List<AIVocabularyWeight>();
            }
        }

        #endregion

        #region Semantic Cache Management

        /// <summary>
        /// Get cached semantic similarity results
        /// </summary>
        public async Task<AISemanticCache?> GetSemanticCacheAsync(string queryHash)
        {
            try
            {
                return await _context.AISemanticCache
                    .FirstOrDefaultAsync(c => c.QueryHash == queryHash && c.ExpiresAt > DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting semantic cache: {QueryHash}", queryHash);
                return null;
            }
        }

        /// <summary>
        /// Cache semantic similarity results
        /// </summary>
        public async Task CacheSemanticSimilarityAsync(string queryHash, string queryText, string similarQueries)
        {
            try
            {
                var cache = new AISemanticCache
                {
                    QueryHash = queryHash,
                    QueryText = queryText,
                    SimilarQueries = similarQueries,
                    CachedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddDays(7),
                    HitCount = 0
                };

                _context.AISemanticCache.Add(cache);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error caching semantic similarity: {QueryHash}", queryHash);
            }
        }

        /// <summary>
        /// Clean up expired semantic cache entries
        /// </summary>
        public async Task CleanupExpiredCacheAsync()
        {
            try
            {
                var expired = await _context.AISemanticCache
                    .Where(c => c.ExpiresAt < DateTime.UtcNow)
                    .ToListAsync();

                if (expired.Any())
                {
                    _context.AISemanticCache.RemoveRange(expired);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Cleaned up {Count} expired cache entries", expired.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up expired cache entries");
            }
        }

        #endregion

        #region Analytics and Reporting

        /// <summary>
        /// Get learning insights for a user
        /// </summary>
        public async Task<LearningInsightDto> GetLearningInsightsAsync(string userId)
        {
            try
            {
                // Get user's conversation sessions
                var sessions = await _context.AIConversationSessions
                    .Where(s => s.UserId == userId)
                    .ToListAsync();

                var totalQueries = await _context.AIMessages
                    .Where(m => m.Role == "assistant" && sessions.Select(s => s.SessionId).Contains(m.SessionId))
                    .CountAsync();

                var averageConfidence = await _context.AIMessages
                    .Where(m => m.Role == "assistant" && sessions.Select(s => s.SessionId).Contains(m.SessionId) && m.Confidence.HasValue)
                    .AverageAsync(m => m.Confidence!.Value);

                // Get pattern distribution
                var patternDistribution = await _context.AILearningPatterns
                    .GroupBy(p => p.PatternType)
                    .Select(g => new
                    {
                        Pattern = g.Key,
                        Count = g.Sum(p => p.Frequency)
                    })
                    .ToListAsync();

                // Get top queries
                var topQueries = await _context.AILearningPatterns
                    .OrderByDescending(p => p.Frequency)
                    .Take(5)
                    .Select(p => p.QueryText)
                    .ToListAsync();

                return new LearningInsightDto
                {
                    TotalQueries = totalQueries,
                    LearningAccuracy = (double)averageConfidence,
                    PatternDistribution = patternDistribution.ToDictionary(p => p.Pattern, p => p.Count),
                    TopQueries = topQueries,
                    LastUpdated = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting learning insights: {UserId}", userId);
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

        #endregion
    }
}

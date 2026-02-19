-- =============================================
-- Sepio AI Learning Database Schema
-- =============================================

-- AI Conversation Sessions Table
CREATE TABLE IF NOT EXISTS AI_ConversationSessions (
    Id SERIAL PRIMARY KEY,
    SessionId VARCHAR(64) UNIQUE NOT NULL,
    UserId VARCHAR(255) NOT NULL,
    TenantId VARCHAR(255) NOT NULL,
    UserRole VARCHAR(50) NOT NULL,
    StartedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    LastActivityAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    MessageCount INTEGER DEFAULT 0,
    IsActive BOOLEAN DEFAULT TRUE,
    Metadata JSONB,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- AI Messages Table
CREATE TABLE IF NOT EXISTS AI_Messages (
    Id SERIAL PRIMARY KEY,
    SessionId VARCHAR(64) NOT NULL REFERENCES AI_ConversationSessions(SessionId),
    MessageId VARCHAR(64) UNIQUE NOT NULL,
    Role VARCHAR(20) NOT NULL CHECK (Role IN ('user', 'assistant')),
    Content TEXT NOT NULL,
    QueryType VARCHAR(50),
    Confidence DECIMAL(3,2),
    ResponseTime INTERVAL,
    TokensUsed INTEGER DEFAULT 0,
    Sources JSONB,
    FeedbackRating INTEGER CHECK (FeedbackRating >= 1 AND FeedbackRating <= 5),
    FeedbackText TEXT,
    ProcessedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- AI Learning Patterns Table
CREATE TABLE IF NOT EXISTS AI_LearningPatterns (
    Id SERIAL PRIMARY KEY,
    PatternType VARCHAR(50) NOT NULL,
    PatternKey VARCHAR(100) NOT NULL,
    QueryText TEXT NOT NULL,
    Frequency INTEGER DEFAULT 1,
    LastSeen TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    SuccessRate DECIMAL(3,2) DEFAULT 0.0,
    AverageConfidence DECIMAL(3,2) DEFAULT 0.0,
    ContextData JSONB,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(PatternType, PatternKey)
);

-- AI Model Training Data Table
CREATE TABLE IF NOT EXISTS AI_ModelTraining (
    Id SERIAL PRIMARY KEY,
    TrainingSessionId VARCHAR(64) UNIQUE NOT NULL,
    ModelVersion VARCHAR(50) NOT NULL,
    TrainingType VARCHAR(50) NOT NULL,
    TrainingData JSONB NOT NULL,
    FeedbackData JSONB,
    PerformanceMetrics JSONB,
    TrainingStatus VARCHAR(50) DEFAULT 'pending',
    StartedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    CompletedAt TIMESTAMP WITH TIME ZONE,
    ErrorText TEXT,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- AI User Feedback Table
CREATE TABLE IF NOT EXISTS AI_UserFeedback (
    Id SERIAL PRIMARY KEY,
    SessionId VARCHAR(64) NOT NULL,
    MessageId VARCHAR(64) NOT NULL,
    UserId VARCHAR(255) NOT NULL,
    FeedbackType VARCHAR(50) NOT NULL,
    Rating INTEGER NOT NULL CHECK (Rating >= 1 AND Rating <= 5),
    FeedbackText TEXT,
    QueryText TEXT,
    ResponseText TEXT,
    ImprovementSuggestions TEXT,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- AI Knowledge Base Table
CREATE TABLE IF NOT EXISTS AI_KnowledgeBase (
    Id SERIAL PRIMARY KEY,
    Category VARCHAR(50) NOT NULL,
    Term VARCHAR(255) NOT NULL,
    Definition TEXT,
    Context JSONB,
    SourceUrl TEXT,
    SourceTitle TEXT,
    ReliabilityScore DECIMAL(3,2) DEFAULT 1.0,
    LastVerified TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    IsActive BOOLEAN DEFAULT TRUE,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(Category, Term)
);

-- AI Semantic Similarity Cache Table
CREATE TABLE IF NOT EXISTS AI_SemanticCache (
    Id SERIAL PRIMARY KEY,
    QueryHash VARCHAR(128) UNIQUE NOT NULL,
    QueryText TEXT NOT NULL,
    SimilarQueries JSONB,
    CachedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    ExpiresAt TIMESTAMP WITH TIME ZONE DEFAULT (CURRENT_TIMESTAMP + INTERVAL '7 days'),
    HitCount INTEGER DEFAULT 0
);

-- AI Performance Metrics Table
CREATE TABLE IF NOT EXISTS AI_PerformanceMetrics (
    Id SERIAL PRIMARY KEY,
    MetricDate DATE NOT NULL,
    TotalQueries INTEGER DEFAULT 0,
    AverageResponseTime DECIMAL(8,2),
    AverageConfidence DECIMAL(3,2),
    SuccessRate DECIMAL(3,2),
    TopQueryTypes JSONB,
    UserSatisfaction DECIMAL(3,2),
    ModelVersion VARCHAR(50),
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(MetricDate, ModelVersion)
);

-- AI Vocabulary Weights Table
CREATE TABLE IF NOT EXISTS AI_VocabularyWeights (
    Id SERIAL PRIMARY KEY,
    Term VARCHAR(255) UNIQUE NOT NULL,
    Weight DECIMAL(8,4) DEFAULT 1.0,
    Frequency INTEGER DEFAULT 1,
    Category VARCHAR(50),
    LastUpdated TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- =============================================
-- Indexes for Performance Optimization
-- =============================================

-- Conversation Sessions Indexes
CREATE INDEX IF NOT EXISTS idx_ai_conversation_sessions_user_id ON AI_ConversationSessions(UserId);
CREATE INDEX IF NOT EXISTS idx_ai_conversation_sessions_tenant_id ON AI_ConversationSessions(TenantId);
CREATE INDEX IF NOT EXISTS idx_ai_conversation_sessions_last_activity ON AI_ConversationSessions(LastActivityAt);
CREATE INDEX IF NOT EXISTS idx_ai_conversation_sessions_is_active ON AI_ConversationSessions(IsActive);

-- Messages Indexes
CREATE INDEX IF NOT EXISTS idx_ai_messages_session_id ON AI_Messages(SessionId);
CREATE INDEX IF NOT EXISTS idx_ai_messages_created_at ON AI_Messages(CreatedAt);
CREATE INDEX IF NOT EXISTS idx_ai_messages_role ON AI_Messages(Role);
CREATE INDEX IF NOT EXISTS idx_ai_messages_query_type ON AI_Messages(QueryType);

-- Learning Patterns Indexes
CREATE INDEX IF NOT EXISTS idx_ai_learning_patterns_type ON AI_LearningPatterns(PatternType);
CREATE INDEX IF NOT EXISTS idx_ai_learning_patterns_frequency ON AI_LearningPatterns(Frequency DESC);
CREATE INDEX IF NOT EXISTS idx_ai_learning_patterns_last_seen ON AI_LearningPatterns(LastSeen DESC);

-- Model Training Indexes
CREATE INDEX IF NOT EXISTS idx_ai_model_training_status ON AI_ModelTraining(TrainingStatus);
CREATE INDEX IF NOT EXISTS idx_ai_model_training_started_at ON AI_ModelTraining(StartedAt DESC);

-- User Feedback Indexes
CREATE INDEX IF NOT EXISTS idx_ai_user_feedback_user_id ON AI_UserFeedback(UserId);
CREATE INDEX IF NOT EXISTS idx_ai_user_feedback_created_at ON AI_UserFeedback(CreatedAt DESC);
CREATE INDEX IF NOT EXISTS idx_ai_user_feedback_rating ON AI_UserFeedback(Rating);

-- Knowledge Base Indexes
CREATE INDEX IF NOT EXISTS idx_ai_knowledge_base_category ON AI_KnowledgeBase(Category);
CREATE INDEX IF NOT EXISTS idx_ai_knowledge_base_term ON AI_KnowledgeBase(Term);
CREATE INDEX IF NOT EXISTS idx_ai_knowledge_base_is_active ON AI_KnowledgeBase(IsActive);

-- Semantic Cache Indexes
CREATE INDEX IF NOT EXISTS idx_ai_semantic_cache_expires_at ON AI_SemanticCache(ExpiresAt);
CREATE INDEX IF NOT EXISTS idx_ai_semantic_cache_query_hash ON AI_SemanticCache(QueryHash);

-- Performance Metrics Indexes
CREATE INDEX IF NOT EXISTS idx_ai_performance_metrics_date ON AI_PerformanceMetrics(MetricDate DESC);

-- Vocabulary Weights Indexes
CREATE INDEX IF NOT EXISTS idx_ai_vocabulary_weights_term ON AI_VocabularyWeights(Term);
CREATE INDEX IF NOT EXISTS idx_ai_vocabulary_weights_frequency ON AI_VocabularyWeights(Frequency DESC);

-- =============================================
-- Triggers for Automatic Updates
-- =============================================

-- Update conversation session last activity
CREATE OR REPLACE FUNCTION update_conversation_last_activity()
RETURNS TRIGGER AS $$
BEGIN
    UPDATE AI_ConversationSessions 
    SET LastActivityAt = CURRENT_TIMESTAMP, 
        MessageCount = MessageCount + 1,
        UpdatedAt = CURRENT_TIMESTAMP
    WHERE SessionId = NEW.SessionId;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trigger_update_conversation_last_activity
    AFTER INSERT ON AI_Messages
    FOR EACH ROW
    EXECUTE FUNCTION update_conversation_last_activity();

-- Update learning pattern frequency
CREATE OR REPLACE FUNCTION update_learning_pattern_frequency()
RETURNS TRIGGER AS $$
BEGIN
    INSERT INTO AI_LearningPatterns (PatternType, PatternKey, QueryText, Frequency, LastSeen)
    VALUES (NEW.QueryType, COALESCE(NEW.QueryType, 'general'), NEW.Content, 1, CURRENT_TIMESTAMP)
    ON CONFLICT (PatternType, PatternKey)
    DO UPDATE SET 
        Frequency = AI_LearningPatterns.Frequency + 1,
        LastSeen = CURRENT_TIMESTAMP,
        UpdatedAt = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trigger_update_learning_pattern_frequency
    AFTER INSERT ON AI_Messages
    FOR EACH ROW
    EXECUTE FUNCTION update_learning_pattern_frequency();

-- =============================================
-- Views for Analytics
-- =============================================

-- Daily Performance Summary View
CREATE OR REPLACE VIEW AI_DailyPerformanceSummary AS
SELECT 
    DATE(CreatedAt) AS MetricDate,
    COUNT(*) AS TotalQueries,
    AVG(EXTRACT(EPOCH FROM ResponseTime)) AS AverageResponseTimeSeconds,
    AVG(Confidence) AS AverageConfidence,
    COUNT(*) FILTER (WHERE Confidence >= 0.8) * 100.0 / COUNT(*) AS SuccessRate,
    COUNT(DISTINCT SessionId) AS UniqueSessions
FROM AI_Messages 
WHERE Role = 'assistant'
GROUP BY DATE(CreatedAt)
ORDER BY MetricDate DESC;

-- Top Query Types View
CREATE OR REPLACE VIEW AI_TopQueryTypes AS
SELECT 
    QueryType,
    COUNT(*) AS Frequency,
    AVG(Confidence) AS AverageConfidence,
    AVG(EXTRACT(EPOCH FROM ResponseTime)) AS AverageResponseTimeSeconds
FROM AI_Messages 
WHERE Role = 'assistant' AND QueryType IS NOT NULL
GROUP BY QueryType
ORDER BY Frequency DESC;

-- User Satisfaction View
CREATE OR REPLACE VIEW AI_UserSatisfactionSummary AS
SELECT 
    AVG(Rating) AS AverageRating,
    COUNT(*) AS TotalFeedback,
    COUNT(*) FILTER (WHERE Rating >= 4) * 100.0 / COUNT(*) AS SatisfactionRate,
    DATE(CreatedAt) AS FeedbackDate
FROM AI_UserFeedback
GROUP BY DATE(CreatedAt)
ORDER BY FeedbackDate DESC;

-- =============================================
-- Sample Data for Testing
-- =============================================

-- Insert sample knowledge base entries
INSERT INTO AI_KnowledgeBase (Category, Term, Definition, SourceUrl, SourceTitle, ReliabilityScore) VALUES
('drugs', 'metformin', 'Metformin is an oral diabetes medicine that helps control blood sugar levels.', 'https://www.drugs.com/metformin.html', 'Drugs.com', 0.95),
('drugs', 'insulin', 'Insulin is a hormone that helps control blood sugar levels in people with diabetes.', 'https://www.drugs.com/insulin.html', 'Drugs.com', 0.95),
('conditions', 'hypertension', 'Hypertension is high blood pressure, a condition in which the force of the blood against the artery walls is too high.', 'https://www.who.int/news-room/fact-sheets/detail/hypertension', 'WHO', 0.98),
('zambia_specific', 'malaria', 'Malaria is a life-threatening mosquito-borne blood disease caused by a parasite.', 'https://www.cdc.gov/parasites/malaria/index.html', 'CDC', 0.95),
('symptoms', 'headache', 'Headache is pain in any region of the head. Headaches can occur on one or both sides of the head.', 'https://www.mayoclinic.org/diseases-conditions/headache/symptoms-causes/syc-20354046', 'Mayo Clinic', 0.90)
ON CONFLICT (Category, Term) DO NOTHING;

-- =============================================
-- Cleanup Procedures
-- =============================================

-- Clean up old semantic cache entries
CREATE OR REPLACE FUNCTION cleanup_semantic_cache()
RETURNS void AS $$
BEGIN
    DELETE FROM AI_SemanticCache WHERE ExpiresAt < CURRENT_TIMESTAMP;
END;
$$ LANGUAGE plpgsql;

-- Clean up inactive conversation sessions (older than 30 days)
CREATE OR REPLACE FUNCTION cleanup_old_sessions()
RETURNS void AS $$
BEGIN
    UPDATE AI_ConversationSessions 
    SET IsActive = FALSE 
    WHERE IsActive = TRUE 
    AND LastActivityAt < CURRENT_TIMESTAMP - INTERVAL '30 days';
END;
$$ LANGUAGE plpgsql;

-- Archive old messages (older than 90 days)
CREATE OR REPLACE FUNCTION archive_old_messages()
RETURNS void AS $$
BEGIN
    -- This would move messages to an archive table
    DELETE FROM AI_Messages 
    WHERE CreatedAt < CURRENT_TIMESTAMP - INTERVAL '90 days';
END;
$$ LANGUAGE plpgsql;

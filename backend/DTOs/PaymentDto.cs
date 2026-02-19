using System;
using System.Collections.Generic;

namespace UmiHealthPOS.DTOs
{
    // AI/Machine Learning DTOs
    public class LearningInsightDto
    {
        public int TotalQueries { get; set; }
        public Dictionary<string, int> PatternDistribution { get; set; } = new();
        public Dictionary<string, double> AlgorithmPerformance { get; set; } = new();
        public DateTime LastUpdated { get; set; }
        public List<string> TopQueries { get; set; } = new();
        public double LearningAccuracy { get; set; }
        public int SessionsCount { get; set; }
    }

    public class ModelFeedbackRequest
    {
        public string Feedback { get; set; } = string.Empty;
        public string Query { get; set; } = string.Empty;
        public string Response { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public int Rating { get; set; } // 1-5 rating
        public List<string> Categories { get; set; } = new();
    }

    // Payment System DTOs
    public class ProcessPaymentRequest
    {
        public string SubscriptionId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "ZMW";
        public string PaymentMethod { get; set; } = string.Empty;
        public string? TransactionId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
    }

    public class PaymentResult
    {
        public bool Success { get; set; }
        public string? Response { get; set; }
        public string? TransactionId { get; set; }
        public string? FailureReason { get; set; }
    }

    public class RefundResult
    {
        public bool Success { get; set; }
        public string? Response { get; set; }
        public string? RefundId { get; set; }
        public string? FailureReason { get; set; }
    }

    public class PaymentDto
    {
        public string Id { get; set; } = string.Empty;
        public string SubscriptionId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? TransactionId { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public decimal RefundedAmount { get; set; }
        public List<PaymentRefundDto> Refunds { get; set; } = new();
    }

    public class PaymentRefundDto
    {
        public string Id { get; set; } = string.Empty;
        public string PaymentId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
    }

    public class PaymentHistoryFilterDto
    {
        public List<string>? Status { get; set; }
        public List<string>? PaymentMethod { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class PaymentStatsDto
    {
        public decimal TotalRevenue { get; set; }
        public int TotalTransactions { get; set; }
        public decimal AverageTransactionValue { get; set; }
        public Dictionary<string, int> PaymentMethodCounts { get; set; } = new();
        public Dictionary<string, decimal> MonthlyRevenue { get; set; } = new();
        public int SuccessfulPayments { get; set; }
        public int FailedPayments { get; set; }
        public decimal RefundedAmount { get; set; }
        public int RefundCount { get; set; }
    }

    public class PaymentMethodDto
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
        public Dictionary<string, object> RequiredFields { get; set; } = new();
        public decimal? MinimumAmount { get; set; }
        public decimal? MaximumAmount { get; set; }
        public decimal TransactionFee { get; set; }
        public string Icon { get; set; } = string.Empty;
    }

    public class PaymentValidationRequest
    {
        public string PaymentMethod { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public Dictionary<string, object> PaymentData { get; set; } = new();
    }

    public class PaymentValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public decimal? ProcessingFee { get; set; }
        public decimal? TotalAmount { get; set; }
        public DateTime? EstimatedProcessingTime { get; set; }
    }
}

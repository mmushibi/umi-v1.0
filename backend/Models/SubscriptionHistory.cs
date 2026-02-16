using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UmiHealthPOS.Models
{
    public class SubscriptionHistory
    {
        public int Id { get; set; }

        [Required]
        public int SubscriptionId { get; set; }

        [ForeignKey("SubscriptionId")]
        public virtual Subscription Subscription { get; set; } = null!;

        [Required]
        [MaxLength(20)]
        public string Action { get; set; } = string.Empty; // created, renewed, upgraded, downgraded, cancelled

        [Required]
        [MaxLength(100)]
        public string PreviousPlan { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string NewPlan { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Amount { get; set; }

        [Required]
        [MaxLength(500)]
        public string Notes { get; set; } = string.Empty;

        [Required]
        [MaxLength(450)]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public class SubscriptionHistoryDto
    {
        public int Id { get; set; }
        public string Date { get; set; } = string.Empty;
        public string Tenant { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string BusinessType { get; set; } = string.Empty;
        public string RegistrationNumber { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Plan { get; set; } = string.Empty;
        public string Amount { get; set; } = string.Empty;
        public string User { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public string NextBilling { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public string TotalUsers { get; set; } = string.Empty;
        public string TotalTransactions { get; set; } = string.Empty;
        public string TotalProducts { get; set; } = string.Empty;
        public string StorageUsed { get; set; } = string.Empty;
    }

    public class CreateSubscriptionHistoryRequest
    {
        [Required]
        public int SubscriptionId { get; set; }

        [Required]
        [MaxLength(20)]
        public string Action { get; set; } = string.Empty;

        [MaxLength(100)]
        public string PreviousPlan { get; set; } = string.Empty;

        [MaxLength(100)]
        public string NewPlan { get; set; } = string.Empty;

        [Required]
        public decimal Amount { get; set; }

        [MaxLength(500)]
        public string Notes { get; set; } = string.Empty;

        [Required]
        [MaxLength(450)]
        public string UserId { get; set; } = string.Empty;
    }

    public class UpdateSubscriptionHistoryRequest
    {
        [MaxLength(20)]
        public string Action { get; set; } = string.Empty;

        [MaxLength(100)]
        public string PreviousPlan { get; set; } = string.Empty;

        [MaxLength(100)]
        public string NewPlan { get; set; } = string.Empty;

        public decimal? Amount { get; set; }

        [MaxLength(500)]
        public string Notes { get; set; } = string.Empty;
    }
}

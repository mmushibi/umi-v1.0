using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UmiHealthPOS.Models
{
    public class Branch
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; }

        [Required]
        [StringLength(500)]
        public string Address { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(100)]
        [Required]
        public string Region { get; set; }

        public string? PostalCode { get; set; }

        [StringLength(50)]
        public string Country { get; set; } = "Zambia";

        [Required]
        [StringLength(50)]
        public string Phone { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string ManagerName { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string ManagerPhone { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string OperatingHours { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "active";

        [Column(TypeName = "decimal(10,2)")]
        public decimal MonthlyRevenue { get; set; } = 0;

        public int StaffCount { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<User> Users { get; set; } = new List<User>();
        public virtual ICollection<UserBranch> UserBranches { get; set; } = new List<UserBranch>();
        public virtual ICollection<Sale> Sales { get; set; } = new List<Sale>();
        public virtual ICollection<InventoryItem> InventoryItems { get; set; } = new List<InventoryItem>();
        public virtual ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
        public virtual ICollection<Patient> Patients { get; set; } = new List<Patient>();
    }

    public class UserBranch
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        public int BranchId { get; set; }

        [Required]
        public string UserRole { get; set; } // TenantAdmin, Pharmacist, Cashier

        [Required]
        public string Permission { get; set; } = "read"; // read, write, admin

        public bool IsActive { get; set; } = true;

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Branch Branch { get; set; }
        public virtual User? User { get; set; }
    }

    public class ReportSchedule
    {
        public int Id { get; set; }

        [Required]
        public string ReportType { get; set; }

        [Required]
        public string Frequency { get; set; }

        [Required]
        public string DateRange { get; set; }

        [Required]
        public int BranchId { get; set; }

        [Required]
        public string Format { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public TimeSpan Time { get; set; }

        [Required]
        [StringLength(200)]
        public string RecipientEmail { get; set; }

        [Required]
        public string CreatedBy { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastRunAt { get; set; }

        public DateTime? NextRunAt { get; set; }

        // Navigation properties
        public virtual Branch Branch { get; set; }
    }
}

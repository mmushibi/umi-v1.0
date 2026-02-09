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
        
        [StringLength(500)]
        public string Address { get; set; }
        
        [StringLength(100)]
        public string Phone { get; set; }
        
        [StringLength(100)]
        public string Email { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation properties
        public virtual ICollection<UserBranch> UserBranches { get; set; }
        public virtual ICollection<Sale> Sales { get; set; }
        public virtual ICollection<InventoryItem> InventoryItems { get; set; }
        public virtual ICollection<Prescription> Prescriptions { get; set; }
        public virtual ICollection<Patient> Patients { get; set; }
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
        
        public bool IsActive { get; set; } = true;
        
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual Branch Branch { get; set; }
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
        public string BranchId { get; set; }
        
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

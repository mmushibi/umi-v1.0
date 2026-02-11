using System.ComponentModel.DataAnnotations;

namespace UmiHealthPOS.Models
{
    public class User
    {
        public string Id { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;
        
        [Required]
        [StringLength(200)]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        [StringLength(200)]
        public string NormalizedEmail { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string PhoneNumber { get; set; } = string.Empty;
        
        [Required]
        [StringLength(50)]
        public string Role { get; set; } = string.Empty;
        
        public string Department { get; set; } = string.Empty;
        
        public bool TwoFactorEnabled { get; set; } = false;
        
        public DateTime? LastLogin { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        public int? BranchId { get; set; }
        
        public string Address { get; set; } = string.Empty;
        
        public string City { get; set; } = string.Empty;
        
        public string PostalCode { get; set; } = string.Empty;
        
        [StringLength(50)]
        public string Country { get; set; } = string.Empty;
        
        [StringLength(100)]
        public string LicenseNumber { get; set; } = string.Empty;
        
        [StringLength(100)]
        public string RegistrationNumber { get; set; } = string.Empty;
        
        public bool IsActive { get; set; } = true;
        
        public bool EmailConfirmed { get; set; } = false;
        
        public bool PhoneNumberConfirmed { get; set; } = false;
        
        public int FailedLoginAttempts { get; set; } = 0;
        
        public DateTime? LockoutEnd { get; set; }
        
        // Password hash (not included in exports)
        public string PasswordHash { get; set; } = string.Empty;
        
        // Status field
        public string Status { get; set; } = "active";
        
        // Navigation properties
        public virtual ICollection<UserBranch> UserBranches { get; set; } = new List<UserBranch>();
        
        public virtual Branch Branch { get; set; } = null!;
    }
    
    public class CreateUserRequest
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        [Phone]
        [StringLength(20)]
        public string Phone { get; set; } = string.Empty;
        
        [Required]
        [StringLength(50)]
        public string Role { get; set; } = string.Empty;
        
        [StringLength(100)]
        public string? Branch { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Password { get; set; } = string.Empty;
    }
    
    public class UserResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? Branch { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? LastLogin { get; set; }
        public DateTime CreatedAt { get; set; }
    }
    
    public class SignupRequest
    {
        [Required]
        [StringLength(200)]
        public string OrganizationName { get; set; }
        
        [Required]
        [StringLength(100)]
        public string FirstName { get; set; }
        
        [Required]
        [StringLength(100)]
        public string LastName { get; set; }
        
        [Required]
        [EmailAddress]
        [StringLength(200)]
        public string Email { get; set; }
        
        [Required]
        [StringLength(20)]
        public string Phone { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Password { get; set; }
        
        [StringLength(50)]
        public string Plan { get; set; }
        
        [StringLength(50)]
        public string Role { get; set; }
    }
}

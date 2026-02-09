using System.ComponentModel.DataAnnotations;

namespace UmiHealthPOS.Models
{
    public class User
    {
        public string Id { get; set; } = string.Empty;
        
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
        
        public DateTime? LastLogin { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Password hash (not included in exports)
        public string PasswordHash { get; set; } = string.Empty;
        
        // Status field
        public string Status { get; set; } = "active";
        
        // Navigation properties
        public virtual ICollection<UserBranch> UserBranches { get; set; } = new List<UserBranch>();
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
}

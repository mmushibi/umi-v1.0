using System.ComponentModel.DataAnnotations;

namespace UmiHealthPOS.Models.DTOs
{
    public class TenantDto
    {
        public string Id { get; set; }
        public string Company { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Avatar { get; set; }
        public string Status { get; set; }
        public string Plan { get; set; }
        public string Cost { get; set; }
        public string Users { get; set; }
        public string Created { get; set; }
        public string NextBilling { get; set; }
        public string Address { get; set; }
        public string LicenseNumber { get; set; }
        public string ZambiaRegNumber { get; set; }
        public string AdminName { get; set; }
        public List<ActivityDto> Activities { get; set; } = new List<ActivityDto>();
    }

    public class TenantDetailDto : TenantDto
    {
        public List<UserDto> UsersList { get; set; } = new List<UserDto>();
    }

    public class UserDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public string Status { get; set; }
        public string Avatar { get; set; }
        public string LastLogin { get; set; }
    }

    public class ActivityDto
    {
        public int Id { get; set; }
        public string Action { get; set; }
        public string Timestamp { get; set; }
    }

    public class CreateTenantRequest
    {
        [Required]
        public string PharmacyName { get; set; }

        [Required]
        public string AdminName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [Phone]
        public string PhoneNumber { get; set; }

        [Required]
        public string Address { get; set; }

        [Required]
        public string Subscription { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; }

        public string LicenseNumber { get; set; }
        public string ZambiaRegNumber { get; set; }
    }

    public class UpdateTenantRequest
    {
        [Required]
        public string PharmacyName { get; set; }

        [Required]
        public string AdminName { get; set; }

        [Required]
        [Phone]
        public string PhoneNumber { get; set; }

        [Required]
        public string Address { get; set; }

        [Required]
        public string Subscription { get; set; }

        [Required]
        public string Status { get; set; }

        public string LicenseNumber { get; set; }
        public string ZambiaRegNumber { get; set; }
    }

    public class ResetPasswordRequest
    {
        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string NewPassword { get; set; }
    }

    public class RevealPasswordRequest
    {
        [Required]
        public string Reason { get; set; } = string.Empty;
        
        [Required]
        public string AdminConfirmation { get; set; } = string.Empty;
    }
}

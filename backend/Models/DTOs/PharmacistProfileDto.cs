using System;

namespace UmiHealthPOS.Models.DTOs
{
    public class PharmacistProfileDto
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string LicenseNumber { get; set; } = string.Empty;
        public bool EmailNotifications { get; set; }
        public bool ClinicalAlerts { get; set; }
        public int SessionTimeout { get; set; }
        public string Language { get; set; } = string.Empty;
        public bool TwoFactorEnabled { get; set; }
        public string ProfilePicture { get; set; } = string.Empty;
        public string Signature { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class UpdatePharmacistProfileRequest
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string Phone { get; set; } = string.Empty;
        public string LicenseNumber { get; set; } = string.Empty;
        public string? Specialization { get; set; }
        public int YearsExperience { get; set; }
        public bool EmailNotifications { get; set; }
        public bool ClinicalAlerts { get; set; }
        public int SessionTimeout { get; set; }
        public string Language { get; set; } = string.Empty;
        public bool TwoFactorEnabled { get; set; }
        public string ProfilePicture { get; set; } = string.Empty;
        public string Signature { get; set; } = string.Empty;
    }
}

using System.ComponentModel.DataAnnotations;
using UmiHealthPOS.DTOs;

namespace UmiHealthPOS.Models
{
    public class UpdatePatientRequest
    {
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Email { get; set; }

        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        [StringLength(20)]
        public string? Phone { get; set; }

        [StringLength(20)]
        public string? IdNumber { get; set; }

        [StringLength(200)]
        public string? Address { get; set; }

        public DateTime? DateOfBirth { get; set; }

        [StringLength(10)]
        public string? Gender { get; set; }

        [StringLength(500)]
        public string? Allergies { get; set; }

        [StringLength(1000)]
        public string? MedicalHistory { get; set; }

        public int? BranchId { get; set; }
    }

    public class PatientResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string IdNumber { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public DateTime? DateOfBirth { get; set; }
        public string Gender { get; set; } = string.Empty;
        public string Allergies { get; set; } = string.Empty;
        public string MedicalHistory { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}

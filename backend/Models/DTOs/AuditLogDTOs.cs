using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UmiHealthPOS.DTOs
{
    #region Audit Log DTOs

    public class AuditLogDto
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string? TenantId { get; set; }
        public string? TenantName { get; set; }
        public string Action { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public string? EntityId { get; set; }
        public string? EntityName { get; set; }
        public string OldValues { get; set; } = string.Empty;
        public string NewValues { get; set; } = string.Empty;
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public string? Description { get; set; }
        public DateTime Timestamp { get; set; }
        public string Severity { get; set; } = string.Empty;
        public bool IsSuccess { get; set; }
        public string TimestampFormatted => Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
    }

    public class AuditLogFilterDto
    {
        public string? UserId { get; set; }
        public string? TenantId { get; set; }
        public string? Action { get; set; }
        public string? EntityType { get; set; }
        public string? Severity { get; set; }
        public bool? IsSuccess { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Search { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class AuditLogStatsDto
    {
        public int TotalLogs { get; set; }
        public int TodayLogs { get; set; }
        public int ThisWeekLogs { get; set; }
        public int ThisMonthLogs { get; set; }
        public int CriticalLogs { get; set; }
        public int FailedLogs { get; set; }
        public int SuccessLogs { get; set; }
        public Dictionary<string, int> ActionCounts { get; set; } = new();
        public Dictionary<string, int> EntityTypeCounts { get; set; } = new();
    }

    #endregion

    #region Employee DTOs

    public class EmployeeDto
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public string EmployeeNumber { get; set; } = string.Empty;
        public DateTime HireDate { get; set; }
        public decimal Salary { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string FullName => $"{FirstName} {LastName}";
        public string EmploymentDuration => EmployeeHelper.GetEmploymentDuration(HireDate);
    }

    public class CreateEmployeeRequest
    {
        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [StringLength(200)]
        public string Email { get; set; } = string.Empty;

        [StringLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Position { get; set; } = string.Empty;

        [StringLength(20)]
        public string EmployeeNumber { get; set; } = string.Empty;

        public DateTime HireDate { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "decimal(10,2)")]
        public decimal Salary { get; set; }
    }

    public class UpdateEmployeeRequest
    {
        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [StringLength(200)]
        public string Email { get; set; } = string.Empty;

        [StringLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Position { get; set; } = string.Empty;

        [StringLength(20)]
        public string EmployeeNumber { get; set; } = string.Empty;

        public DateTime HireDate { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal Salary { get; set; }
    }

    public class EmployeeFilterDto
    {
        public string? Search { get; set; }
        public string? Position { get; set; }
        public bool? IsActive { get; set; }
        public DateTime? HireDateFrom { get; set; }
        public DateTime? HireDateTo { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class EmployeeStatsDto
    {
        public int TotalEmployees { get; set; }
        public int ActiveEmployees { get; set; }
        public int InactiveEmployees { get; set; }
        public int NewHiresThisMonth { get; set; }
        public decimal AverageSalary { get; set; }
        public Dictionary<string, int> PositionCounts { get; set; } = new();
    }

    #endregion

    #region Settings DTOs

    public class AppSettingDto
    {
        public int Id { get; set; }
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public bool IsEncrypted { get; set; }
        public bool IsReadOnly { get; set; }
        public string Environment { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public string DisplayValue => IsEncrypted ? "***" : Value;
    }

    public class CreateAppSettingRequest
    {
        [Required]
        [StringLength(100)]
        public string Key { get; set; } = string.Empty;

        [Required]
        public string Value { get; set; } = string.Empty;

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Category { get; set; } = string.Empty;

        [StringLength(20)]
        public string DataType { get; set; } = "String";

        public bool IsEncrypted { get; set; } = false;

        public bool IsReadOnly { get; set; } = false;

        [StringLength(20)]
        public string Environment { get; set; } = "All";
    }

    public class UpdateAppSettingRequest
    {
        [Required]
        public string Value { get; set; } = string.Empty;

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [StringLength(20)]
        public string DataType { get; set; } = "String";

        public bool IsEncrypted { get; set; } = false;

        public bool IsReadOnly { get; set; } = false;

        [StringLength(20)]
        public string Environment { get; set; } = "All";
    }

    public class AppSettingFilterDto
    {
        public string? Category { get; set; }
        public string? Environment { get; set; }
        public string? Search { get; set; }
        public bool? IsEncrypted { get; set; }
        public bool? IsReadOnly { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class AppSettingStatsDto
    {
        public int TotalSettings { get; set; }
        public int EncryptedSettings { get; set; }
        public int ReadOnlySettings { get; set; }
        public Dictionary<string, int> CategoryCounts { get; set; } = new();
        public Dictionary<string, int> EnvironmentCounts { get; set; } = new();
    }

    #endregion

    #region Helper Methods

    public static class EmployeeHelper
    {
        public static string GetEmploymentDuration(DateTime hireDate)
        {
            var now = DateTime.UtcNow;
            var span = now - hireDate;

            if (span.Days < 30)
                return $"{span.Days} days";
            if (span.Days < 365)
                return $"{span.Days / 30} months";
            
            var years = span.Days / 365;
            var months = (span.Days % 365) / 30;
            
            return months > 0 ? $"{years}y {months}m" : $"{years}y";
        }
    }

    #endregion
}

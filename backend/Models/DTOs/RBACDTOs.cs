using System;
using System.ComponentModel.DataAnnotations;

namespace UmiHealthPOS.DTOs
{
    #region Role DTOs

    public class RoleDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Level { get; set; } = "Medium";
        public string Status { get; set; } = "Active";
        public bool IsSystem { get; set; }
        public bool IsGlobal { get; set; }
        public int PermissionCount { get; set; }
        public int UserCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class CreateRoleRequest
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Level { get; set; } = "Medium";

        public bool IsGlobal { get; set; } = true;

        public int[] PermissionIds { get; set; } = Array.Empty<int>();
    }

    public class UpdateRoleRequest
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Level { get; set; } = "Medium";

        public bool IsGlobal { get; set; }

        public int[] PermissionIds { get; set; } = Array.Empty<int>();
    }

    public class RoleStatusRequest
    {
        [Required]
        public int RoleId { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = string.Empty;
    }

    #endregion

    #region Permission DTOs

    public class PermissionDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string RiskLevel { get; set; } = "Medium";
        public string Status { get; set; } = "Active";
        public bool IsSystem { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int RoleCount { get; set; }
    }

    public class CreatePermissionRequest
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string DisplayName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Category { get; set; } = string.Empty;

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string RiskLevel { get; set; } = "Medium";
    }

    public class UpdatePermissionRequest
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string DisplayName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Category { get; set; } = string.Empty;

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string RiskLevel { get; set; } = "Medium";
    }

    public class PermissionStatusRequest
    {
        [Required]
        public int PermissionId { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = string.Empty;
    }

    #endregion

    #region Role Assignment DTOs

    public class RolePermissionDto
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public int PermissionId { get; set; }
        public string PermissionName { get; set; } = string.Empty;
        public string PermissionDisplayName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string RiskLevel { get; set; } = string.Empty;
        public DateTime AssignedAt { get; set; }
    }

    public class AssignPermissionsRequest
    {
        [Required]
        public int RoleId { get; set; }

        [Required]
        public int[] PermissionIds { get; set; } = Array.Empty<int>();
    }

    public class RemovePermissionsRequest
    {
        [Required]
        public int RoleId { get; set; }

        [Required]
        public int[] PermissionIds { get; set; } = Array.Empty<int>();
    }

    #endregion

    #region Tenant Role DTOs

    public class TenantRoleDto
    {
        public int Id { get; set; }
        public string TenantId { get; set; } = string.Empty;
        public string TenantName { get; set; } = string.Empty;
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public string RoleLevel { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public int UserCount { get; set; }
        public DateTime AssignedAt { get; set; }
        public DateTime? LastUsedAt { get; set; }
    }

    public class TenantRoleAssignmentDto
    {
        public int Id { get; set; }
        public string Tenant { get; set; } = string.Empty;
        public int UserCount { get; set; }
        public int RoleCount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime LastActivity { get; set; }
    }

    public class AssignTenantRoleRequest
    {
        [Required]
        [StringLength(6)]
        public string TenantId { get; set; } = string.Empty;

        [Required]
        public int RoleId { get; set; }
    }

    #endregion

    #region User Role DTOs

    public class UserRoleAssignmentDto
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public string? TenantId { get; set; }
        public string? TenantName { get; set; }
        public DateTime AssignedAt { get; set; }
        public string? AssignedBy { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public bool IsActive { get; set; }
    }

    public class AssignUserRoleRequest
    {
        [Required]
        [StringLength(450)]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public int RoleId { get; set; }

        [StringLength(6)]
        public string? TenantId { get; set; }

        public DateTime? ExpiresAt { get; set; }
    }

    #endregion

    #region Stats DTOs

    public class RbacStatsDto
    {
        public int TotalRoles { get; set; }
        public int ActiveRoles { get; set; }
        public int InactiveRoles { get; set; }
        public int SystemRoles { get; set; }
        public int TotalPermissions { get; set; }
        public int ActivePermissions { get; set; }
        public int CriticalPermissions { get; set; }
        public int TotalRoleAssignments { get; set; }
        public int ActiveRoleAssignments { get; set; }
        public int TenantRoleAssignments { get; set; }
    }

    #endregion
}

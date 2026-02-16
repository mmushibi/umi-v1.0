using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using UmiHealthPOS.Data;
using UmiHealthPOS.Models;
using UmiHealthPOS.DTOs;
using System.Security.Claims;

namespace UmiHealthPOS.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "superadmin")]
    public class RBACController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<RBACController> _logger;

        public RBACController(
            ApplicationDbContext context,
            ILogger<RBACController> logger)
        {
            _context = context;
            _logger = logger;
        }

        #region Roles

        [HttpGet("roles")]
        public async Task<ActionResult<PagedResult<RoleDto>>> GetRoles([FromQuery] string search = "", [FromQuery] string status = "", [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var query = _context.Roles.AsQueryable();

                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(r => 
                        r.Name.Contains(search) || 
                        r.Description.Contains(search));
                }

                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(r => r.Status == status);
                }

                var totalCount = await query.CountAsync();

                var roles = await query
                    .OrderBy(r => r.Name)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(r => new RoleDto
                    {
                        Id = r.Id,
                        Name = r.Name,
                        Description = r.Description,
                        Level = r.Level,
                        Status = r.Status,
                        IsSystem = r.IsSystem,
                        IsGlobal = r.IsGlobal,
                        PermissionCount = r.RolePermissions.Count,
                        UserCount = r.UserRoles.Count(ur => ur.IsActive),
                        CreatedAt = r.CreatedAt,
                        UpdatedAt = r.UpdatedAt
                    })
                    .ToListAsync();

                return Ok(new PagedResult<RoleDto>
                {
                    Data = roles,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving roles");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("roles/{id}")]
        public async Task<ActionResult<RoleDto>> GetRole(int id)
        {
            try
            {
                var role = await _context.Roles
                    .Include(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (role == null)
                {
                    return NotFound(new { error = "Role not found" });
                }

                return Ok(new RoleDto
                {
                    Id = role.Id,
                    Name = role.Name,
                    Description = role.Description,
                    Level = role.Level,
                    Status = role.Status,
                    IsSystem = role.IsSystem,
                    IsGlobal = role.IsGlobal,
                    PermissionCount = role.RolePermissions.Count,
                    UserCount = role.UserRoles.Count(ur => ur.IsActive),
                    CreatedAt = role.CreatedAt,
                    UpdatedAt = role.UpdatedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving role");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("roles")]
        public async Task<ActionResult<RoleDto>> CreateRole([FromBody] CreateRoleRequest request)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                // Check if role name already exists
                var existingRole = await _context.Roles
                    .FirstOrDefaultAsync(r => r.Name.ToLower() == request.Name.ToLower());

                if (existingRole != null)
                {
                    return BadRequest(new { error = "Role name already exists" });
                }

                var role = new Role
                {
                    Name = request.Name,
                    Description = request.Description,
                    Level = request.Level,
                    IsGlobal = request.IsGlobal,
                    Status = "Active",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _context.Roles.AddAsync(role);
                await _context.SaveChangesAsync();

                // Add permissions if provided
                if (request.PermissionIds.Length > 0)
                {
                    var rolePermissions = request.PermissionIds.Select(permissionId => new RolePermission
                    {
                        RoleId = role.Id,
                        PermissionId = permissionId,
                        CreatedAt = DateTime.UtcNow
                    }).ToList();

                    await _context.RolePermissions.AddRangeAsync(rolePermissions);
                    await _context.SaveChangesAsync();
                }

                var roleDto = new RoleDto
                {
                    Id = role.Id,
                    Name = role.Name,
                    Description = role.Description,
                    Level = role.Level,
                    Status = role.Status,
                    IsSystem = role.IsSystem,
                    IsGlobal = role.IsGlobal,
                    PermissionCount = request.PermissionIds.Length,
                    UserCount = 0,
                    CreatedAt = role.CreatedAt,
                    UpdatedAt = role.UpdatedAt
                };

                return CreatedAtAction(nameof(GetRole), new { id = role.Id }, roleDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating role");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPut("roles/{id}")]
        public async Task<ActionResult<RoleDto>> UpdateRole(int id, [FromBody] UpdateRoleRequest request)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var role = await _context.Roles.FindAsync(id);
                if (role == null)
                {
                    return NotFound(new { error = "Role not found" });
                }

                if (role.IsSystem)
                {
                    return BadRequest(new { error = "Cannot modify system roles" });
                }

                // Check if role name already exists (excluding this role)
                var existingRole = await _context.Roles
                    .FirstOrDefaultAsync(r => r.Name.ToLower() == request.Name.ToLower() && r.Id != id);

                if (existingRole != null)
                {
                    return BadRequest(new { error = "Role name already exists" });
                }

                role.Name = request.Name;
                role.Description = request.Description;
                role.Level = request.Level;
                role.IsGlobal = request.IsGlobal;
                role.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Update permissions
                var existingPermissions = await _context.RolePermissions
                    .Where(rp => rp.RoleId == id)
                    .ToListAsync();

                _context.RolePermissions.RemoveRange(existingPermissions);

                if (request.PermissionIds.Length > 0)
                {
                    var rolePermissions = request.PermissionIds.Select(permissionId => new RolePermission
                    {
                        RoleId = id,
                        PermissionId = permissionId,
                        CreatedAt = DateTime.UtcNow
                    }).ToList();

                    await _context.RolePermissions.AddRangeAsync(rolePermissions);
                }

                await _context.SaveChangesAsync();

                var roleDto = new RoleDto
                {
                    Id = role.Id,
                    Name = role.Name,
                    Description = role.Description,
                    Level = role.Level,
                    Status = role.Status,
                    IsSystem = role.IsSystem,
                    IsGlobal = role.IsGlobal,
                    PermissionCount = request.PermissionIds.Length,
                    UserCount = role.UserRoles.Count(ur => ur.IsActive),
                    CreatedAt = role.CreatedAt,
                    UpdatedAt = role.UpdatedAt
                };

                return Ok(roleDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating role");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpDelete("roles/{id}")]
        public async Task<ActionResult> DeleteRole(int id)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var role = await _context.Roles.FindAsync(id);
                if (role == null)
                {
                    return NotFound(new { error = "Role not found" });
                }

                if (role.IsSystem)
                {
                    return BadRequest(new { error = "Cannot delete system roles" });
                }

                // Check if role is in use
                var userCount = await _context.UserRoles
                    .CountAsync(ur => ur.RoleId == id && ur.IsActive);

                if (userCount > 0)
                {
                    return BadRequest(new { error = "Cannot delete role that is assigned to users" });
                }

                _context.Roles.Remove(role);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Role deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting role");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        #endregion

        #region Permissions

        [HttpGet("permissions")]
        public async Task<ActionResult<PagedResult<PermissionDto>>> GetPermissions([FromQuery] string search = "", [FromQuery] string category = "", [FromQuery] string riskLevel = "", [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var query = _context.Permissions.AsQueryable();

                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(p => 
                        p.Name.Contains(search) || 
                        p.DisplayName.Contains(search) ||
                        p.Description.Contains(search));
                }

                if (!string.IsNullOrEmpty(category))
                {
                    query = query.Where(p => p.Category == category);
                }

                if (!string.IsNullOrEmpty(riskLevel))
                {
                    query = query.Where(p => p.RiskLevel == riskLevel);
                }

                var totalCount = await query.CountAsync();

                var permissions = await query
                    .OrderBy(p => p.Category)
                    .ThenBy(p => p.DisplayName)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new PermissionDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        DisplayName = p.DisplayName,
                        Category = p.Category,
                        Description = p.Description,
                        RiskLevel = p.RiskLevel,
                        Status = p.Status,
                        IsSystem = p.IsSystem,
                        CreatedAt = p.CreatedAt,
                        UpdatedAt = p.UpdatedAt,
                        RoleCount = p.RolePermissions.Count
                    })
                    .ToListAsync();

                return Ok(new PagedResult<PermissionDto>
                {
                    Data = permissions,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving permissions");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("permissions/{id}")]
        public async Task<ActionResult<PermissionDto>> GetPermission(int id)
        {
            try
            {
                var permission = await _context.Permissions.FindAsync(id);
                if (permission == null)
                {
                    return NotFound(new { error = "Permission not found" });
                }

                return Ok(new PermissionDto
                {
                    Id = permission.Id,
                    Name = permission.Name,
                    DisplayName = permission.DisplayName,
                    Category = permission.Category,
                    Description = permission.Description,
                    RiskLevel = permission.RiskLevel,
                    Status = permission.Status,
                    IsSystem = permission.IsSystem,
                    CreatedAt = permission.CreatedAt,
                    UpdatedAt = permission.UpdatedAt,
                    RoleCount = permission.RolePermissions.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving permission");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("permissions")]
        public async Task<ActionResult<PermissionDto>> CreatePermission([FromBody] CreatePermissionRequest request)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                // Check if permission name already exists
                var existingPermission = await _context.Permissions
                    .FirstOrDefaultAsync(p => p.Name.ToLower() == request.Name.ToLower());

                if (existingPermission != null)
                {
                    return BadRequest(new { error = "Permission name already exists" });
                }

                var permission = new Permission
                {
                    Name = request.Name,
                    DisplayName = request.DisplayName,
                    Category = request.Category,
                    Description = request.Description,
                    RiskLevel = request.RiskLevel,
                    Status = "Active",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _context.Permissions.AddAsync(permission);
                await _context.SaveChangesAsync();

                var permissionDto = new PermissionDto
                {
                    Id = permission.Id,
                    Name = permission.Name,
                    DisplayName = permission.DisplayName,
                    Category = permission.Category,
                    Description = permission.Description,
                    RiskLevel = permission.RiskLevel,
                    Status = permission.Status,
                    IsSystem = permission.IsSystem,
                    CreatedAt = permission.CreatedAt,
                    UpdatedAt = permission.UpdatedAt,
                    RoleCount = 0
                };

                return CreatedAtAction(nameof(GetPermission), new { id = permission.Id }, permissionDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating permission");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPut("permissions/{id}")]
        public async Task<ActionResult<PermissionDto>> UpdatePermission(int id, [FromBody] UpdatePermissionRequest request)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var permission = await _context.Permissions.FindAsync(id);
                if (permission == null)
                {
                    return NotFound(new { error = "Permission not found" });
                }

                if (permission.IsSystem)
                {
                    return BadRequest(new { error = "Cannot modify system permissions" });
                }

                // Check if permission name already exists (excluding this permission)
                var existingPermission = await _context.Permissions
                    .FirstOrDefaultAsync(p => p.Name.ToLower() == request.Name.ToLower() && p.Id != id);

                if (existingPermission != null)
                {
                    return BadRequest(new { error = "Permission name already exists" });
                }

                permission.Name = request.Name;
                permission.DisplayName = request.DisplayName;
                permission.Category = request.Category;
                permission.Description = request.Description;
                permission.RiskLevel = request.RiskLevel;
                permission.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                var permissionDto = new PermissionDto
                {
                    Id = permission.Id,
                    Name = permission.Name,
                    DisplayName = permission.DisplayName,
                    Category = permission.Category,
                    Description = permission.Description,
                    RiskLevel = permission.RiskLevel,
                    Status = permission.Status,
                    IsSystem = permission.IsSystem,
                    CreatedAt = permission.CreatedAt,
                    UpdatedAt = permission.UpdatedAt,
                    RoleCount = permission.RolePermissions.Count
                };

                return Ok(permissionDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating permission");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpDelete("permissions/{id}")]
        public async Task<ActionResult> DeletePermission(int id)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var permission = await _context.Permissions.FindAsync(id);
                if (permission == null)
                {
                    return NotFound(new { error = "Permission not found" });
                }

                if (permission.IsSystem)
                {
                    return BadRequest(new { error = "Cannot delete system permissions" });
                }

                // Check if permission is in use
                var roleCount = await _context.RolePermissions
                    .CountAsync(rp => rp.PermissionId == id);

                if (roleCount > 0)
                {
                    return BadRequest(new { error = "Cannot delete permission that is assigned to roles" });
                }

                _context.Permissions.Remove(permission);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Permission deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting permission");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        #endregion

        #region Role Permissions

        [HttpGet("roles/{roleId}/permissions")]
        public async Task<ActionResult<List<RolePermissionDto>>> GetRolePermissions(int roleId)
        {
            try
            {
                var rolePermissions = await _context.RolePermissions
                    .Include(rp => rp.Role)
                    .Include(rp => rp.Permission)
                    .Where(rp => rp.RoleId == roleId)
                    .Select(rp => new RolePermissionDto
                    {
                        RoleId = rp.RoleId,
                        RoleName = rp.Role.Name,
                        PermissionId = rp.PermissionId,
                        PermissionName = rp.Permission.Name,
                        PermissionDisplayName = rp.Permission.DisplayName,
                        Category = rp.Permission.Category,
                        RiskLevel = rp.Permission.RiskLevel,
                        AssignedAt = rp.CreatedAt
                    })
                    .ToListAsync();

                return Ok(rolePermissions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving role permissions");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("roles/{roleId}/permissions")]
        public async Task<ActionResult> AssignPermissionsToRole(int roleId, [FromBody] AssignPermissionsRequest request)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var role = await _context.Roles.FindAsync(roleId);
                if (role == null)
                {
                    return NotFound(new { error = "Role not found" });
                }

                // Remove existing permissions
                var existingPermissions = await _context.RolePermissions
                    .Where(rp => rp.RoleId == roleId)
                    .ToListAsync();

                _context.RolePermissions.RemoveRange(existingPermissions);

                // Add new permissions
                if (request.PermissionIds.Length > 0)
                {
                    var rolePermissions = request.PermissionIds.Select(permissionId => new RolePermission
                    {
                        RoleId = roleId,
                        PermissionId = permissionId,
                        CreatedAt = DateTime.UtcNow
                    }).ToList();

                    await _context.RolePermissions.AddRangeAsync(rolePermissions);
                }

                await _context.SaveChangesAsync();

                return Ok(new { message = $"Assigned {request.PermissionIds.Length} permissions to role" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning permissions to role");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        #endregion

        #region Stats

        [HttpGet("stats")]
        public async Task<ActionResult<RbacStatsDto>> GetRbacStats()
        {
            try
            {
                var query = _context.Roles.AsQueryable();
                var permissionQuery = _context.Permissions.AsQueryable();
                var userRoleQuery = _context.UserRoles.AsQueryable();

                var totalRoles = await query.CountAsync();
                var activeRoles = await query.CountAsync(r => r.Status == "Active");
                var inactiveRoles = await query.CountAsync(r => r.Status == "Inactive");
                var systemRoles = await query.CountAsync(r => r.IsSystem);

                var totalPermissions = await permissionQuery.CountAsync();
                var activePermissions = await permissionQuery.CountAsync(p => p.Status == "Active");
                var criticalPermissions = await permissionQuery.CountAsync(p => p.RiskLevel == "Critical");

                var totalRoleAssignments = await userRoleQuery.CountAsync();
                var activeRoleAssignments = await userRoleQuery.CountAsync(ur => ur.IsActive);
                var tenantRoleAssignments = await userRoleQuery.CountAsync(ur => ur.TenantId != null);

                return Ok(new RbacStatsDto
                {
                    TotalRoles = totalRoles,
                    ActiveRoles = activeRoles,
                    InactiveRoles = inactiveRoles,
                    SystemRoles = systemRoles,
                    TotalPermissions = totalPermissions,
                    ActivePermissions = activePermissions,
                    CriticalPermissions = criticalPermissions,
                    TotalRoleAssignments = totalRoleAssignments,
                    ActiveRoleAssignments = activeRoleAssignments,
                    TenantRoleAssignments = tenantRoleAssignments
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving RBAC stats");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        #endregion

        #region Helper Methods

        [HttpGet("categories")]
        public async Task<ActionResult<List<string>>> GetPermissionCategories()
        {
            try
            {
                var categories = await _context.Permissions
                    .Where(p => p.Status == "Active")
                    .Select(p => p.Category)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToListAsync();

                return Ok(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving permission categories");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        #endregion
    }
}

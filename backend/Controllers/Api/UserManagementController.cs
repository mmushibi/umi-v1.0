using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using UmiHealthPOS.DTOs;
using UmiHealthPOS.Models;
using UmiHealthPOS.Security;

namespace UmiHealthPOS.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UserManagementController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserManagementController> _logger;
        private readonly IRowLevelSecurityService _securityService;

        public UserManagementController(
            ApplicationDbContext context,
            ILogger<UserManagementController> logger,
            IRowLevelSecurityService securityService)
        {
            _context = context;
            _logger = logger;
            _securityService = securityService;
        }

        // GET: api/usermanagement/users
        [HttpGet("users")]
        [RequirePermission(SystemPermissions.USER_READ)]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers(
            [FromQuery] string? tenantId = null,
            [FromQuery] int? branchId = null,
            [FromQuery] string? role = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var securityContext = await _securityService.GetSecurityContextAsync(User);
                var query = _context.Users.AsQueryable();

                // Apply row-level security
                query = await _securityService.ApplyTenantFilter(query, User) as IQueryable<UserAccount>;
                query = await _securityService.ApplyBranchFilter(query, User) as IQueryable<UserAccount>;

                // Apply filters
                if (!string.IsNullOrEmpty(tenantId) && await _securityService.CanAccessTenantAsync(User, tenantId))
                {
                    query = query.Where(u => u.TenantId == tenantId);
                }

                if (branchId.HasValue && await _securityService.CanAccessBranchAsync(User, branchId.Value))
                {
                    query = query.Where(u => u.BranchId == branchId.Value);
                }

                if (!string.IsNullOrEmpty(role))
                {
                    query = query.Where(u => u.Role == role);
                }

                var users = await query
                    .Include(u => u.Tenant)
                    .Include(u => u.Branch)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(u => new UserDto
                    {
                        Id = u.Id,
                        UserId = u.UserId,
                        Email = u.Email,
                        FirstName = u.FirstName,
                        LastName = u.LastName,
                        Role = u.Role,
                        TenantId = u.TenantId,
                        TenantName = u.Tenant != null ? u.Tenant.Name : null,
                        BranchId = u.BranchId,
                        BranchName = u.Branch != null ? u.Branch.Name : null,
                        Status = u.Status,
                        IsActive = u.IsActive,
                        PhoneNumber = u.PhoneNumber,
                        Department = u.Department,
                        TwoFactorEnabled = u.TwoFactorEnabled,
                        LastLogin = u.LastLogin,
                        CreatedAt = u.CreatedAt,
                        UpdatedAt = u.UpdatedAt
                    })
                    .ToListAsync();

                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/usermanagement/users/{id}
        [HttpGet("users/{id}")]
        [RequirePermission(SystemPermissions.USER_READ)]
        public async Task<ActionResult<UserDto>> GetUser(string id)
        {
            try
            {
                var query = _context.Users.AsQueryable();
                query = await _securityService.ApplyTenantFilter(query, User) as IQueryable<UserAccount>;
                query = await _securityService.ApplyBranchFilter(query, User) as IQueryable<UserAccount>;

                var user = await query
                    .Include(u => u.Tenant)
                    .Include(u => u.Branch)
                    .FirstOrDefaultAsync(u => u.UserId == id);

                if (user == null)
                {
                    return NotFound();
                }

                return Ok(new UserDto
                {
                    Id = user.Id,
                    UserId = user.UserId,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Role = user.Role,
                    TenantId = user.TenantId,
                    TenantName = user.Tenant?.Name,
                    BranchId = user.BranchId,
                    BranchName = user.Branch?.Name,
                    Status = user.Status,
                    IsActive = user.IsActive,
                    PhoneNumber = user.PhoneNumber,
                    Department = user.Department,
                    TwoFactorEnabled = user.TwoFactorEnabled,
                    LastLogin = user.LastLogin,
                    CreatedAt = user.CreatedAt,
                    UpdatedAt = user.UpdatedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user {UserId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        // POST: api/usermanagement/users
        [HttpPost("users")]
        [RequirePermission(SystemPermissions.USER_CREATE)]
        public async Task<ActionResult<UserDto>> CreateUser(CreateUserRequest request)
        {
            try
            {
                var securityContext = await _securityService.GetSecurityContextAsync(User);
                
                // Check if the creator can assign the requested role
                if (!Enum.TryParse<UserRole>(request.Role, out var requestedRole))
                {
                    return BadRequest("Invalid role specified");
                }

                if (!securityContext.Role.CanManageRole(requestedRole))
                {
                    return Forbid($"You cannot create users with role {requestedRole.GetDisplayName()}");
                }

                // Check tenant access
                if (!string.IsNullOrEmpty(request.TenantId) && !await _securityService.CanAccessTenantAsync(User, request.TenantId))
                {
                    return Forbid("You cannot create users in this tenant");
                }

                // Check if user already exists
                if (await _context.Users.AnyAsync(u => u.Email == request.Email))
                {
                    return BadRequest("User with this email already exists");
                }

                var user = new UserAccount
                {
                    UserId = Guid.NewGuid().ToString(),
                    Email = request.Email,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Role = request.Role,
                    TenantId = request.TenantId ?? securityContext.TenantId,
                    BranchId = request.BranchId,
                    Status = "Active",
                    IsActive = true,
                    PhoneNumber = request.PhoneNumber,
                    Department = request.Department,
                    TwoFactorEnabled = request.TwoFactorEnabled ?? false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User {UserId} created by {CreatorUserId}", user.UserId, securityContext.UserId);

                return CreatedAtAction(nameof(GetUser), new { id = user.UserId }, new UserDto
                {
                    Id = user.Id,
                    UserId = user.UserId,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Role = user.Role,
                    TenantId = user.TenantId,
                    BranchId = user.BranchId,
                    Status = user.Status,
                    IsActive = user.IsActive,
                    PhoneNumber = user.PhoneNumber,
                    Department = user.Department,
                    TwoFactorEnabled = user.TwoFactorEnabled,
                    CreatedAt = user.CreatedAt,
                    UpdatedAt = user.UpdatedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                return StatusCode(500, "Internal server error");
            }
        }

        // PUT: api/usermanagement/users/{id}
        [HttpPut("users/{id}")]
        [RequirePermission(SystemPermissions.USER_UPDATE)]
        public async Task<IActionResult> UpdateUser(string id, UpdateUserRequest request)
        {
            try
            {
                var securityContext = await _securityService.GetSecurityContextAsync(User);
                
                var query = _context.Users.AsQueryable();
                query = await _securityService.ApplyTenantFilter(query, User) as IQueryable<UserAccount>;
                query = await _securityService.ApplyBranchFilter(query, User) as IQueryable<UserAccount>;

                var user = await query.FirstOrDefaultAsync(u => u.UserId == id);
                if (user == null)
                {
                    return NotFound();
                }

                // Check if trying to change role and if allowed
                if (!string.IsNullOrEmpty(request.Role) && request.Role != user.Role)
                {
                    if (!Enum.TryParse<UserRole>(request.Role, out var newRole))
                    {
                        return BadRequest("Invalid role specified");
                    }

                    if (!securityContext.Role.CanManageRole(newRole))
                    {
                        return Forbid($"You cannot assign role {newRole.GetDisplayName()}");
                    }

                    user.Role = request.Role;
                }

                // Check tenant access if changing tenant
                if (!string.IsNullOrEmpty(request.TenantId) && request.TenantId != user.TenantId)
                {
                    if (!await _securityService.CanAccessTenantAsync(User, request.TenantId))
                    {
                        return Forbid("You cannot move users to this tenant");
                    }

                    user.TenantId = request.TenantId;
                }

                // Update other fields
                if (!string.IsNullOrEmpty(request.FirstName)) user.FirstName = request.FirstName;
                if (!string.IsNullOrEmpty(request.LastName)) user.LastName = request.LastName;
                if (!string.IsNullOrEmpty(request.PhoneNumber)) user.PhoneNumber = request.PhoneNumber;
                if (!string.IsNullOrEmpty(request.Department)) user.Department = request.Department;
                if (request.BranchId.HasValue) user.BranchId = request.BranchId;
                if (request.TwoFactorEnabled.HasValue) user.TwoFactorEnabled = request.TwoFactorEnabled.Value;
                if (!string.IsNullOrEmpty(request.Status)) user.Status = request.Status;
                if (request.IsActive.HasValue) user.IsActive = request.IsActive.Value;

                user.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("User {UserId} updated by {UpdaterUserId}", user.UserId, securityContext.UserId);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        // DELETE: api/usermanagement/users/{id}
        [HttpDelete("users/{id}")]
        [RequirePermission(SystemPermissions.USER_DELETE)]
        public async Task<IActionResult> DeleteUser(string id)
        {
            try
            {
                var securityContext = await _securityService.GetSecurityContextAsync(User);
                
                var query = _context.Users.AsQueryable();
                query = await _securityService.ApplyTenantFilter(query, User) as IQueryable<UserAccount>;
                query = await _securityService.ApplyBranchFilter(query, User) as IQueryable<UserAccount>;

                var user = await query.FirstOrDefaultAsync(u => u.UserId == id);
                if (user == null)
                {
                    return NotFound();
                }

                // Check if can delete this role
                if (!Enum.TryParse<UserRole>(user.Role, out var userRole))
                {
                    return BadRequest("Invalid user role");
                }

                if (!securityContext.Role.CanManageRole(userRole))
                {
                    return Forbid($"You cannot delete users with role {userRole.GetDisplayName()}");
                }

                // Cannot delete self
                if (user.UserId == securityContext.UserId)
                {
                    return BadRequest("You cannot delete your own account");
                }

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User {UserId} deleted by {DeleterUserId}", user.UserId, securityContext.UserId);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        // POST: api/usermanagement/users/{id}/reset-password
        [HttpPost("users/{id}/reset-password")]
        [RequirePermission(SystemPermissions.USER_RESET_PASSWORD)]
        public async Task<IActionResult> ResetUserPassword(string id, ResetPasswordRequest request)
        {
            try
            {
                var securityContext = await _securityService.GetSecurityContextAsync(User);
                
                var query = _context.Users.AsQueryable();
                query = await _securityService.ApplyTenantFilter(query, User) as IQueryable<UserAccount>;
                query = await _securityService.ApplyBranchFilter(query, User) as IQueryable<UserAccount>;

                var user = await query.FirstOrDefaultAsync(u => u.UserId == id);
                if (user == null)
                {
                    return NotFound();
                }

                // Check if can manage this role
                if (!Enum.TryParse<UserRole>(user.Role, out var userRole))
                {
                    return BadRequest("Invalid user role");
                }

                if (!securityContext.Role.CanManageRole(userRole))
                {
                    return Forbid($"You cannot reset password for users with role {userRole.GetDisplayName()}");
                }

                // In a real implementation, this would generate a secure temporary password
                // and send it via email or other secure channel
                var tempPassword = GenerateTemporaryPassword();
                
                // This would typically be handled by a password hashing service
                // For now, we'll just log the action
                _logger.LogInformation("Password reset for user {UserId} by {ResetterUserId}. Temp password: {TempPassword}", 
                    user.UserId, securityContext.UserId, tempPassword);

                return Ok(new { Message = "Password reset successfully", TemporaryPassword = tempPassword });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password for user {UserId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/usermanagement/roles/available
        [HttpGet("roles/available")]
        [RequirePermission(SystemPermissions.USER_READ)]
        public async Task<ActionResult<IEnumerable<RoleDto>>> GetAvailableRoles()
        {
            try
            {
                var securityContext = await _securityService.GetSecurityContextAsync(User);
                var manageableRoles = securityContext.Role.GetManageableRoles();

                var roleDtos = manageableRoles.Select(role => new RoleDto
                {
                    Name = role.ToString(),
                    DisplayName = role.GetDisplayName(),
                    Level = role.GetHierarchyLevel().ToString(),
                    CanManage = securityContext.Role.CanManageRole(role),
                    CanImpersonate = securityContext.Role.CanImpersonateRole(role)
                }).ToList();

                return Ok(roleDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving available roles");
                return StatusCode(500, "Internal server error");
            }
        }

        private string GenerateTemporaryPassword()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
            var random = new Random();
            var password = new char[12];

            for (int i = 0; i < password.Length; i++)
            {
                password[i] = chars[random.Next(chars.Length)];
            }

            return new string(password);
        }
    }

    // DTOs for User Management
    public class UserDto
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string Role { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;
        public string? TenantName { get; set; }
        public int? BranchId { get; set; }
        public string? BranchName { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Department { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public DateTime? LastLogin { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class CreateUserRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Role { get; set; } = string.Empty;

        [StringLength(6)]
        public string? TenantId { get; set; }

        public int? BranchId { get; set; }

        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        [StringLength(100)]
        public string? Department { get; set; }

        public bool? TwoFactorEnabled { get; set; }
    }

    public class UpdateUserRequest
    {
        [EmailAddress]
        public string? Email { get; set; }

        [StringLength(100)]
        public string? FirstName { get; set; }

        [StringLength(100)]
        public string? LastName { get; set; }

        [StringLength(20)]
        public string? Role { get; set; }

        [StringLength(6)]
        public string? TenantId { get; set; }

        public int? BranchId { get; set; }

        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        [StringLength(100)]
        public string? Department { get; set; }

        public bool? TwoFactorEnabled { get; set; }

        [StringLength(20)]
        public string? Status { get; set; }

        public bool? IsActive { get; set; }
    }

    public class ResetPasswordRequest
    {
        [Required]
        public bool SendEmail { get; set; } = true;
    }

    public class RoleDto
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Level { get; set; } = string.Empty;
        public bool CanManage { get; set; }
        public bool CanImpersonate { get; set; }
    }
}

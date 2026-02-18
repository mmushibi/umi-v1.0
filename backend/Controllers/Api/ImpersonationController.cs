using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UmiHealthPOS.DTOs;
using UmiHealthPOS.Models;
using UmiHealthPOS.Security;
using UmiHealthPOS.Services;
using UmiHealthPOS.Data;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace UmiHealthPOS.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ImpersonationController : ControllerBase
    {
        private readonly IImpersonationService _impersonationService;
        private readonly ILogger<ImpersonationController> _logger;
        private readonly IRowLevelSecurityService _securityService;
        private readonly ApplicationDbContext _context;

        public ImpersonationController(
            IImpersonationService impersonationService,
            ILogger<ImpersonationController> logger,
            IRowLevelSecurityService securityService,
            ApplicationDbContext context)
        {
            _impersonationService = impersonationService;
            _logger = logger;
            _securityService = securityService;
            _context = context;
        }

        // POST: api/impersonation/start
        [HttpPost("start")]
        [RequirePermission(SystemPermissions.IMPERSONATE_USER)]
        public async Task<ActionResult<ImpersonationResponse>> StartImpersonation([FromBody] ImpersonationRequest request)
        {
            try
            {
                var securityContext = await _securityService.GetSecurityContextAsync(User);

                // Only Super Admin can impersonate
                if (securityContext.Role != UserRoleEnum.SuperAdmin)
                {
                    return Forbid("Only Super Admin users can impersonate other users");
                }

                // Check if can impersonate the target user
                if (!await _impersonationService.CanImpersonateUserAsync(User, request.TargetUserId))
                {
                    return BadRequest("Cannot impersonate the specified user");
                }

                var token = await _impersonationService.StartImpersonationAsync(User, request.TargetUserId, request.Reason);

                var activeSessions = await _impersonationService.GetActiveImpersonationSessionsAsync(User);
                var impersonationLog = activeSessions.FirstOrDefault(s => s.ImpersonatedUserId == request.TargetUserId);

                return Ok(new ImpersonationResponse
                {
                    Success = true,
                    Token = token,
                    Message = "Impersonation started successfully",
                    ImpersonationLog = impersonationLog
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized impersonation attempt");
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting impersonation for user {TargetUserId}", request.TargetUserId);
                return StatusCode(500, "Internal server error");
            }
        }

        // POST: api/impersonation/stop
        [HttpPost("stop")]
        public async Task<IActionResult> StopImpersonation([FromBody] EndImpersonationRequest request)
        {
            try
            {
                // Check if user is currently being impersonated
                if (!await _impersonationService.IsUserImpersonatedAsync(User))
                {
                    return BadRequest("No active impersonation session found");
                }

                var success = await _impersonationService.StopImpersonationAsync(User);

                if (success)
                {
                    return Ok(new { Message = "Impersonation ended successfully" });
                }
                else
                {
                    return BadRequest("Failed to end impersonation session");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping impersonation");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/impersonation/active
        [HttpGet("active")]
        [RequirePermission(SystemPermissions.IMPERSONATE_USER)]
        public async Task<ActionResult<IEnumerable<ImpersonationSessionDto>>> GetActiveImpersonationSessions()
        {
            try
            {
                var securityContext = await _securityService.GetSecurityContextAsync(User);

                // Only Super Admin can view active sessions
                if (securityContext.Role != UserRoleEnum.SuperAdmin)
                {
                    return Forbid("Only Super Admin users can view active impersonation sessions");
                }

                var activeSessions = await _impersonationService.GetActiveImpersonationSessionsAsync(User);

                var sessionDtos = activeSessions.Select(session => new ImpersonationSessionDto
                {
                    Id = session.Id,
                    ImpersonatedUserId = session.ImpersonatedUserId,
                    ImpersonatedUserName = $"{session.ImpersonatedUser?.FirstName} {session.ImpersonatedUser?.LastName}".Trim(),
                    ImpersonatedUserEmail = session.ImpersonatedUser?.Email ?? string.Empty,
                    ImpersonatedRole = session.ImpersonatedRole,
                    TenantName = session.Tenant?.Name,
                    StartedAt = session.StartedAt,
                    Reason = session.Reason,
                    IpAddress = session.IpAddress
                }).ToList();

                return Ok(sessionDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving active impersonation sessions");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/impersonation/history
        [HttpGet("history")]
        [RequirePermission(SystemPermissions.VIEW_IMPERSONATION_LOGS)]
        public async Task<ActionResult<IEnumerable<ImpersonationSessionDto>>> GetImpersonationHistory(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            try
            {
                var securityContext = await _securityService.GetSecurityContextAsync(User);

                // Only Super Admin can view impersonation logs
                if (securityContext.Role != UserRoleEnum.SuperAdmin)
                {
                    return Forbid("Only Super Admin users can view impersonation history");
                }

                var history = await _impersonationService.GetImpersonationHistoryAsync(User, fromDate, toDate);

                var historyDtos = history.Select(session => new ImpersonationSessionDto
                {
                    Id = session.Id,
                    ImpersonatedUserId = session.ImpersonatedUserId,
                    ImpersonatedUserName = $"{session.ImpersonatedUser?.FirstName} {session.ImpersonatedUser?.LastName}".Trim(),
                    ImpersonatedUserEmail = session.ImpersonatedUser?.Email ?? string.Empty,
                    ImpersonatedRole = session.ImpersonatedRole,
                    TenantName = session.Tenant?.Name,
                    StartedAt = session.StartedAt,
                    Reason = session.Reason,
                    IpAddress = session.IpAddress
                }).ToList();

                return Ok(historyDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving impersonation history");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/impersonation/status
        [HttpGet("status")]
        public async Task<ActionResult<object>> GetImpersonationStatus()
        {
            try
            {
                var isImpersonated = await _impersonationService.IsUserImpersonatedAsync(User);
                var impersonatingUserId = isImpersonated ? await _impersonationService.GetImpersonatingUserIdAsync(User) : null;

                return Ok(new
                {
                    IsImpersonated = isImpersonated,
                    ImpersonatingUserId = impersonatingUserId,
                    CurrentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking impersonation status");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/impersonation/users/search
        [HttpGet("users/search")]
        [RequirePermission(SystemPermissions.IMPERSONATE_USER)]
        public async Task<ActionResult<IEnumerable<object>>> SearchUsersForImpersonation(
            [FromQuery] string query,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var securityContext = await _securityService.GetSecurityContextAsync(User);

                // Only Super Admin can search for users to impersonate
                if (securityContext.Role != UserRoleEnum.SuperAdmin)
                {
                    return Forbid("Only Super Admin users can search for users to impersonate");
                }

                if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
                {
                    return BadRequest("Search query must be at least 2 characters long");
                }

                // Get all users (Super Admin can see all)
                var usersQuery = _context.Users.AsQueryable();

                // Apply search filters
                usersQuery = usersQuery.Where(u =>
                    (u.FirstName != null && u.FirstName.ToLower().Contains(query.ToLower())) ||
                    (u.LastName != null && u.LastName.ToLower().Contains(query.ToLower())) ||
                    u.Email.ToLower().Contains(query.ToLower()));

                var users = await usersQuery
                    .Include(u => u.Tenant)
                    .Include(u => u.Branch)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var userDtos = users.Select(u => new
                {
                    u.UserId,
                    u.Email,
                    FirstName = u.FirstName ?? "",
                    LastName = u.LastName ?? "",
                    FullName = (u.FirstName + " " + u.LastName).Trim(),
                    u.Role,
                    RoleDisplay = Enum.TryParse<UserRoleEnum>(u.Role, out var role) ? UserRoleExtensions.GetDisplayName(role) : u.Role,
                    u.TenantId,
                    TenantName = u.Tenant != null ? u.Tenant.Name : null,
                    u.BranchId,
                    BranchName = u.Branch != null ? u.Branch.Name : null,
                    u.Status,
                    u.IsActive,
                    CanImpersonate = Enum.TryParse<UserRoleEnum>(u.Role, out var parsedRole) && securityContext.Role.CanImpersonateRole(parsedRole)
                }).ToList();

                return Ok(userDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching users for impersonation");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}

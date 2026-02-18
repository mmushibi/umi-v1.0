using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using UmiHealthPOS.Models;
using UmiHealthPOS.Data;
using UmiHealthPOS.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace UmiHealthPOS.Services
{
    public interface IImpersonationService
    {
        Task<bool> CanImpersonateUserAsync(ClaimsPrincipal superAdmin, string targetUserId);
        Task<string> StartImpersonationAsync(ClaimsPrincipal superAdmin, string targetUserId, string? reason = null);
        Task<bool> StopImpersonationAsync(ClaimsPrincipal impersonatedUser);
        Task<List<ImpersonationLog>> GetActiveImpersonationSessionsAsync(ClaimsPrincipal superAdmin);
        Task<List<ImpersonationLog>> GetImpersonationHistoryAsync(ClaimsPrincipal superAdmin, DateTime? fromDate = null, DateTime? toDate = null);
        Task<bool> IsUserImpersonatedAsync(ClaimsPrincipal user);
        Task<string?> GetImpersonatingUserIdAsync(ClaimsPrincipal impersonatedUser);
    }

    public class ImpersonationService : IImpersonationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ImpersonationService> _logger;
        private readonly IJwtService _jwtService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ImpersonationService(
            ApplicationDbContext context,
            ILogger<ImpersonationService> logger,
            IJwtService jwtService,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _logger = logger;
            _jwtService = jwtService;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<bool> CanImpersonateUserAsync(ClaimsPrincipal superAdmin, string targetUserId)
        {
            try
            {
                var superAdminUserId = superAdmin.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(superAdminUserId))
                {
                    _logger.LogWarning("Impersonation attempt failed: Super Admin not authenticated");
                    return false;
                }

                // Get Super Admin user
                var superAdminUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.UserId == superAdminUserId);

                if (superAdminUser == null || superAdminUser.Role != UserRoleEnum.SuperAdmin.ToString())
                {
                    _logger.LogWarning("Impersonation attempt failed: User {UserId} is not a Super Admin", superAdminUserId);
                    return false;
                }

                // Get target user
                var targetUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.UserId == targetUserId);

                if (targetUser == null)
                {
                    _logger.LogWarning("Impersonation attempt failed: Target user {TargetUserId} not found", targetUserId);
                    return false;
                }

                // Cannot impersonate self
                if (superAdminUserId == targetUserId)
                {
                    _logger.LogWarning("Impersonation attempt failed: User cannot impersonate themselves");
                    return false;
                }

                // Cannot impersonate another Super Admin
                if (targetUser.Role == UserRoleEnum.SuperAdmin.ToString())
                {
                    _logger.LogWarning("Impersonation attempt failed: Cannot impersonate another Super Admin");
                    return false;
                }

                // Check if target user is already being impersonated
                var existingImpersonation = await _context.ImpersonationLogs
                    .FirstOrDefaultAsync(i => i.ImpersonatedUserId == targetUserId && i.IsActive);

                if (existingImpersonation != null)
                {
                    _logger.LogWarning("Impersonation attempt failed: User {TargetUserId} is already being impersonated", targetUserId);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking impersonation permissions for user {TargetUserId}", targetUserId);
                return false;
            }
        }

        public async Task<string> StartImpersonationAsync(ClaimsPrincipal superAdmin, string targetUserId, string? reason = null)
        {
            try
            {
                if (!await CanImpersonateUserAsync(superAdmin, targetUserId))
                {
                    throw new UnauthorizedAccessException("User does not have permission to impersonate the target user");
                }

                var superAdminUserId = superAdmin.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var superAdminUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.UserId == superAdminUserId);

                var targetUser = await _context.Users
                    .Include(u => u.Tenant)
                    .FirstOrDefaultAsync(u => u.UserId == targetUserId);

                if (superAdminUser == null || targetUser == null)
                {
                    throw new InvalidOperationException("Required user accounts not found");
                }

                var httpContext = _httpContextAccessor.HttpContext;
                var ipAddress = httpContext?.Connection?.RemoteIpAddress?.ToString();
                var userAgent = httpContext?.Request?.Headers["User-Agent"].ToString();

                // Create impersonation log
                var impersonationLog = new ImpersonationLog
                {
                    SuperAdminUserId = superAdminUserId!,
                    ImpersonatedUserId = targetUserId,
                    ImpersonatedRole = Enum.Parse<UserRoleEnum>(targetUser.Role),
                    TenantId = targetUser.TenantId,
                    StartedAt = DateTime.UtcNow,
                    Reason = reason,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    IsActive = true
                };

                _context.ImpersonationLogs.Add(impersonationLog);
                await _context.SaveChangesAsync();

                // Create enhanced user session for impersonated user
                var impersonationToken = await _jwtService.GenerateImpersonationTokenAsync(
                    targetUser,
                    superAdminUserId!,
                    impersonationLog.Id);

                var enhancedSession = new EnhancedUserSession
                {
                    UserId = targetUserId,
                    Token = impersonationToken,
                    Role = Enum.Parse<UserRoleEnum>(targetUser.Role),
                    TenantId = targetUser.TenantId,
                    BranchId = targetUser.BranchId,
                    DeviceInfo = "Impersonation Session",
                    Browser = httpContext?.Request?.Headers["User-Agent"].ToString(),
                    IpAddress = ipAddress,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddHours(8), // 8-hour impersonation limit
                    IsActive = true,
                    IsImpersonated = true,
                    ImpersonatedByUserId = superAdminUserId,
                    OriginalRole = Enum.Parse<UserRoleEnum>(superAdminUser.Role),
                    OriginalTenantId = superAdminUser.TenantId,
                    ImpersonatedAt = DateTime.UtcNow
                };

                _context.Set<EnhancedUserSession>().Add(enhancedSession);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Super Admin {SuperAdminUserId} started impersonating user {TargetUserId} at {Time}",
                    superAdminUserId, targetUserId, DateTime.UtcNow);

                return impersonationToken;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting impersonation for user {TargetUserId}", targetUserId);
                throw;
            }
        }

        public async Task<bool> StopImpersonationAsync(ClaimsPrincipal impersonatedUser)
        {
            try
            {
                var userId = impersonatedUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return false;
                }

                var enhancedSession = await _context.Set<EnhancedUserSession>()
                    .FirstOrDefaultAsync(s => s.UserId == userId && s.IsImpersonated && s.IsActive);

                if (enhancedSession == null)
                {
                    _logger.LogWarning("No active impersonation session found for user {UserId}", userId);
                    return false;
                }

                // End the impersonation log
                var impersonationLog = await _context.ImpersonationLogs
                    .FirstOrDefaultAsync(i => i.ImpersonatedUserId == userId && i.IsActive);

                if (impersonationLog != null)
                {
                    impersonationLog.EndedAt = DateTime.UtcNow;
                    impersonationLog.IsActive = false;
                }

                // Deactivate the enhanced session
                enhancedSession.IsActive = false;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Impersonation ended for user {UserId} by Super Admin {SuperAdminUserId} at {Time}",
                    userId, enhancedSession.ImpersonatedByUserId, DateTime.UtcNow);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping impersonation for user");
                return false;
            }
        }

        public async Task<List<ImpersonationLog>> GetActiveImpersonationSessionsAsync(ClaimsPrincipal superAdmin)
        {
            try
            {
                var superAdminUserId = superAdmin.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(superAdminUserId))
                {
                    return [];
                }

                return await _context.ImpersonationLogs
                    .Include(i => i.ImpersonatedUser)
                    .Include(i => i.Tenant)
                    .Where(i => i.SuperAdminUserId == superAdminUserId && i.IsActive)
                    .OrderByDescending(i => i.StartedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving active impersonation sessions");
                return new List<ImpersonationLog>();
            }
        }

        public async Task<List<ImpersonationLog>> GetImpersonationHistoryAsync(ClaimsPrincipal superAdmin, DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                var superAdminUserId = superAdmin.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(superAdminUserId))
                {
                    return [];
                }

                var query = _context.ImpersonationLogs
                    .Include(i => i.ImpersonatedUser)
                    .Include(i => i.Tenant)
                    .Where(i => i.SuperAdminUserId == superAdminUserId);

                if (fromDate.HasValue)
                {
                    query = query.Where(i => i.StartedAt >= fromDate.Value);
                }

                if (toDate.HasValue)
                {
                    query = query.Where(i => i.StartedAt <= toDate.Value);
                }

                return await query
                    .OrderByDescending(i => i.StartedAt)
                    .Take(1000) // Limit to last 1000 records
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving impersonation history");
                return new List<ImpersonationLog>();
            }
        }

        public async Task<bool> IsUserImpersonatedAsync(ClaimsPrincipal user)
        {
            try
            {
                var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return false;
                }

                var enhancedSession = await _context.Set<EnhancedUserSession>()
                    .FirstOrDefaultAsync(s => s.UserId == userId && s.IsImpersonated && s.IsActive);

                return enhancedSession != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if user is impersonated");
                return false;
            }
        }

        public async Task<string?> GetImpersonatingUserIdAsync(ClaimsPrincipal impersonatedUser)
        {
            try
            {
                var userId = impersonatedUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return null;
                }

                var enhancedSession = await _context.Set<EnhancedUserSession>()
                    .FirstOrDefaultAsync(s => s.UserId == userId && s.IsImpersonated && s.IsActive);

                return enhancedSession?.ImpersonatedByUserId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting impersonating user ID");
                return null;
            }
        }
    }

    // Impersonation DTOs
    public class ImpersonationRequest
    {
        [Required]
        [StringLength(450)]
        public string TargetUserId { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Reason { get; set; }
    }

    public class ImpersonationResponse
    {
        public bool Success { get; set; }
        public string? Token { get; set; }
        public string? Message { get; set; }
        public ImpersonationLog? ImpersonationLog { get; set; }
    }

    public class ImpersonationSessionDto
    {
        public int Id { get; set; }
        public string ImpersonatedUserId { get; set; } = string.Empty;
        public string ImpersonatedUserName { get; set; } = string.Empty;
        public string ImpersonatedUserEmail { get; set; } = string.Empty;
        public UserRoleEnum ImpersonatedRole { get; set; }
        public string? TenantName { get; set; }
        public DateTime StartedAt { get; set; }
        public string? Reason { get; set; }
        public string? IpAddress { get; set; }
    }

    public class EndImpersonationRequest
    {
        [Required]
        public string UserId { get; set; } = string.Empty;
    }
}

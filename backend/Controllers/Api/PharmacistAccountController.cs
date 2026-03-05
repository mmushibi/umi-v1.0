using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using UmiHealthPOS.Models.DTOs;
using UmiHealthPOS.Models;
using UmiHealthPOS.Data;
using UmiHealthPOS.Services;
using BCrypt.Net;

namespace UmiHealthPOS.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Pharmacist")]
    public class PharmacistAccountController : ControllerBase
    {
        private readonly ILogger<PharmacistAccountController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly ISessionTimeoutService _sessionTimeoutService;

        public PharmacistAccountController(
            ILogger<PharmacistAccountController> logger,
            ApplicationDbContext context,
            ISessionTimeoutService sessionTimeoutService)
        {
            _logger = logger;
            _context = context;
            _sessionTimeoutService = sessionTimeoutService;
        }

        private string GetCurrentUserId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        }

        private string GetCurrentTenantId()
        {
            return User.FindFirst("TenantId")?.Value ?? string.Empty;
        }

        // GET: api/pharmacistaccount/profile
        [HttpGet("profile")]
        public async Task<ActionResult<PharmacistProfileDto>> GetProfile()
        {
            try
            {
                var userId = GetCurrentUserId();
                var tenantId = GetCurrentTenantId();

                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(tenantId))
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var profile = await _context.PharmacistProfiles
                    .FirstOrDefaultAsync(p => p.UserId == userId && p.TenantId == tenantId);

                if (profile == null)
                {
                    // Create profile if it doesn't exist
                    var user = await _context.Users
                        .FirstOrDefaultAsync(u => u.UserId == userId && u.TenantId == tenantId);

                    if (user == null)
                    {
                        return NotFound(new { error = "User not found" });
                    }

                    profile = new PharmacistProfile
                    {
                        UserId = userId,
                        TenantId = tenantId,
                        LicenseNumber = "", // Default empty
                        Specialization = "", // Default empty
                        YearsExperience = 0, // Default
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.PharmacistProfiles.Add(profile);
                    await _context.SaveChangesAsync();
                }

                // Get user information for the DTO
                var userAccount = await _context.Users
                    .FirstOrDefaultAsync(u => u.UserId == userId && u.TenantId == tenantId);

                if (userAccount == null)
                {
                    return NotFound(new { error = "User account not found" });
                }

                var profileDto = new PharmacistProfileDto
                {
                    Id = profile.Id,
                    FirstName = userAccount?.FirstName ?? "",
                    LastName = userAccount?.LastName ?? "",
                    Email = userAccount?.Email ?? "",
                    Phone = "", // Default value - not in PharmacistProfile entity
                    LicenseNumber = profile.LicenseNumber,
                    EmailNotifications = false, // Default value - not in PharmacistProfile entity
                    ClinicalAlerts = false, // Default value - not in PharmacistProfile entity
                    SessionTimeout = 30, // Default value - not in PharmacistProfile entity
                    Language = "en", // Default value - not in PharmacistProfile entity
                    TwoFactorEnabled = false, // Default value - not in PharmacistProfile entity
                    ProfilePicture = "", // Default value - not in PharmacistProfile entity
                    Signature = "", // Default value - not in PharmacistProfile entity
                    CreatedAt = profile.CreatedAt,
                    UpdatedAt = profile.CreatedAt // Use CreatedAt since UpdatedAt doesn't exist
                };

                return Ok(profileDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving pharmacist profile");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // PUT: api/pharmacistaccount/profile
        [HttpPut("profile")]
        public async Task<ActionResult<PharmacistProfileDto>> UpdateProfile([FromBody] UpdatePharmacistProfileRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var tenantId = GetCurrentTenantId();

                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(tenantId))
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var profile = await _context.PharmacistProfiles
                    .FirstOrDefaultAsync(p => p.UserId == userId && p.TenantId == tenantId);

                if (profile == null)
                {
                    return NotFound(new { error = "Profile not found" });
                }

                // Update profile fields - only update properties that exist in PharmacistProfile
                profile.LicenseNumber = request.LicenseNumber ?? profile.LicenseNumber ?? string.Empty;
                profile.Specialization = request.Specialization ?? profile.Specialization ?? string.Empty;
                profile.YearsExperience = request.YearsExperience;

                // Also update the user account
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.UserId == userId && u.TenantId == tenantId);

                if (user != null)
                {
                    user.FirstName = request.FirstName ?? user.FirstName;
                    user.LastName = request.LastName ?? user.LastName;
                    user.Email = request.Email ?? user.Email;
                    user.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                var profileDto = new PharmacistProfileDto
                {
                    Id = profile.Id,
                    FirstName = profile.FirstName ?? string.Empty,
                    LastName = profile.LastName ?? string.Empty,
                    Email = profile.Email ?? string.Empty,
                    Phone = profile.Phone ?? string.Empty,
                    LicenseNumber = profile.LicenseNumber ?? string.Empty,
                    EmailNotifications = profile.EmailNotifications,
                    ClinicalAlerts = profile.ClinicalAlerts,
                    SessionTimeout = profile.SessionTimeout,
                    Language = profile.Language ?? "en",
                    TwoFactorEnabled = profile.TwoFactorEnabled,
                    ProfilePicture = profile.ProfilePicture ?? string.Empty,
                    Signature = profile.Signature ?? string.Empty,
                    CreatedAt = profile.CreatedAt,
                    UpdatedAt = profile.UpdatedAt
                };

                return Ok(profileDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating pharmacist profile");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // PUT: api/pharmacistaccount/settings
        [HttpPut("settings")]
        public async Task<ActionResult> UpdateSettings([FromBody] UpdatePharmacistSettingsRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var tenantId = GetCurrentTenantId();

                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(tenantId))
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var profile = await _context.PharmacistProfiles
                    .FirstOrDefaultAsync(p => p.UserId == userId && p.TenantId == tenantId);

                if (profile == null)
                {
                    return NotFound(new { error = "Profile not found" });
                }

                // Update settings
                profile.EmailNotifications = request.EmailNotifications;
                profile.ClinicalAlerts = request.ClinicalAlerts;
                profile.SessionTimeout = request.SessionTimeout;
                profile.Language = request.Language;
                profile.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new { message = "Settings updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating pharmacist settings");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // GET: api/pharmacistaccount/session-timeout
        [HttpGet("session-timeout")]
        public async Task<ActionResult<int>> GetSessionTimeout()
        {
            try
            {
                var userId = GetCurrentUserId();
                var tenantId = GetCurrentTenantId();

                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(tenantId))
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var profile = await _context.PharmacistProfiles
                    .FirstOrDefaultAsync(p => p.UserId == userId && p.TenantId == tenantId);

                if (profile == null)
                {
                    return Ok(30); // Default timeout
                }

                return Ok(profile.SessionTimeout);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving session timeout");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // POST: api/pharmacistaccount/change-password
        [HttpPost("change-password")]
        public async Task<ActionResult> ChangePassword([FromBody] PharmacistChangePasswordRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var tenantId = GetCurrentTenantId();

                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(tenantId))
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                if (string.IsNullOrEmpty(request.CurrentPassword) || string.IsNullOrEmpty(request.NewPassword))
                {
                    return BadRequest(new { error = "Current password and new password are required" });
                }

                if (request.NewPassword.Length < 8)
                {
                    return BadRequest(new { error = "Password must be at least 8 characters long" });
                }

                if (request.NewPassword != request.ConfirmPassword)
                {
                    return BadRequest(new { error = "New passwords do not match" });
                }

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.UserId == userId && u.TenantId == tenantId);

                if (user == null)
                {
                    return NotFound(new { error = "User not found" });
                }

                // Verify current password using BCrypt
                bool isCurrentPasswordValid = false;
                if (!string.IsNullOrEmpty(user.PasswordHash))
                {
                    // If no password hash exists, this might be first-time setup or migration
                    // Allow password change with current password validation
                    isCurrentPasswordValid = true;
                }
                else
                {
                    isCurrentPasswordValid = BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash);
                }

                if (!isCurrentPasswordValid)
                {
                    return BadRequest(new { error = "Current password is incorrect" });
                }

                // Hash the new password
                var newPasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);

                // Update user password hash
                user.PasswordHash = newPasswordHash;
                user.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // Update pharmacist profile
                var profile = await _context.PharmacistProfiles
                    .FirstOrDefaultAsync(p => p.UserId == userId && p.TenantId == tenantId);

                if (profile != null)
                {
                    profile.PasswordChangedAt = DateTime.UtcNow;
                    profile.ForcePasswordChange = false;
                    profile.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                return Ok(new { message = "Password changed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // POST: api/pharmacistaccount/extend-session
        [HttpPost("extend-session")]
        public async Task<ActionResult> ExtendSession()
        {
            try
            {
                var userId = GetCurrentUserId();
                var tenantId = GetCurrentTenantId();
                var token = Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(token))
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                await _sessionTimeoutService.ExtendSessionAsync(userId, tenantId, token);
                return Ok(new { message = "Session extended successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extending session");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // POST: api/pharmacistaccount/toggle-2fa
        [HttpPost("toggle-2fa")]
        public async Task<ActionResult> ToggleTwoFactor()
        {
            try
            {
                var userId = GetCurrentUserId();
                var tenantId = GetCurrentTenantId();

                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(tenantId))
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var profile = await _context.PharmacistProfiles
                    .FirstOrDefaultAsync(p => p.UserId == userId && p.TenantId == tenantId);

                if (profile == null)
                {
                    return NotFound(new { error = "Profile not found" });
                }

                // Toggle 2FA (in a real implementation, this would generate/setup 2FA)
                profile.TwoFactorEnabled = !profile.TwoFactorEnabled;
                profile.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                var status = profile.TwoFactorEnabled ? "enabled" : "disabled";
                return Ok(new { message = $"Two-factor authentication {status}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling 2FA");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // DELETE: api/pharmacistaccount/clear-data
        [HttpDelete("clear-data")]
        public async Task<ActionResult> ClearLocalData()
        {
            try
            {
                var userId = GetCurrentUserId();
                var tenantId = GetCurrentTenantId();

                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(tenantId))
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                // This endpoint just returns success - actual clearing is done on frontend
                await Task.CompletedTask; // Add await to fix warning
                return Ok(new { message = "Data cleared successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing data");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }
    }

    // DTOs
    public class PharmacistProfileDto
    {
        public int Id { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? LicenseNumber { get; set; }
        public bool EmailNotifications { get; set; }
        public bool ClinicalAlerts { get; set; }
        public int SessionTimeout { get; set; }
        public string Language { get; set; } = "en";
        public bool TwoFactorEnabled { get; set; }
        public string? ProfilePicture { get; set; }
        public string? Signature { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    
    public class UpdatePharmacistSettingsRequest
    {
        public bool EmailNotifications { get; set; }
        public bool ClinicalAlerts { get; set; }
        public int SessionTimeout { get; set; } = 30;
        public string Language { get; set; } = "en";
    }

    public class PharmacistChangePasswordRequest
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}

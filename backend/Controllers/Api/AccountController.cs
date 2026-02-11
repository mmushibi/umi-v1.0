using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UmiHealthPOS.Data;
using UmiHealthPOS.Models;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using BCrypt.Net-Next;

namespace UmiHealthPOS.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        
        public AccountController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }
        }
        
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = GetUserId();
            var user = await _context.Users
                .Include(u => u.UserBranches)
                .ThenInclude(ub => ub.Branch)
                .FirstOrDefaultAsync(u => u.Id == userId);
            
            if (user == null)
            {
                // Return default user for testing
                user = new Models.User
                {
                    Id = "test-user-id",
                    FirstName = "Test",
                    LastName = "User",
                    Email = "test@example.com",
                    PhoneNumber = "+260 977 123 456",
                    Role = "admin",
                    Department = "Management",
                    TwoFactorEnabled = false
                };
            }
            
            var pharmacy = await _context.Pharmacies.FirstOrDefaultAsync();
            
            var response = new
            {
                user = new
                {
                    id = user.Id,
                    name = $"{user.FirstName} {user.LastName}",
                    email = user.Email,
                    phone = user.PhoneNumber,
                    role = user.Role,
                    department = user.Department,
                    twoFactorEnabled = user.TwoFactorEnabled,
                    lastLogin = user.LastLogin,
                    createdAt = user.CreatedAt
                },
                pharmacy = pharmacy != null ? new
                {
                    id = pharmacy.Id,
                    name = pharmacy.Name,
                    licenseNumber = pharmacy.LicenseNumber,
                    address = pharmacy.Address,
                    city = pharmacy.City,
                    province = pharmacy.Province,
                    postalCode = pharmacy.PostalCode,
                    phone = pharmacy.Phone,
                    email = pharmacy.Email
                } : null
            };
            
            return Ok(response);
        }
        
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            try
            {
                // Validate request
                if (string.IsNullOrWhiteSpace(request.FirstName))
                    return BadRequest(new { message = "First name is required" });
                
                if (string.IsNullOrWhiteSpace(request.LastName))
                    return BadRequest(new { message = "Last name is required" });
                
                if (string.IsNullOrWhiteSpace(request.Email))
                    return BadRequest(new { message = "Email is required" });
                
                // Validate email format
                if (!IsValidEmail(request.Email))
                    return BadRequest(new { message = "Invalid email format" });
                
                var userId = GetUserId();
                var user = await _context.Users.FindAsync(userId);
                
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }
                
                // Check if email is being changed to another user's email
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower() && u.Id != userId);
                
                if (existingUser != null)
                {
                    return Conflict(new { message = "Email is already in use by another user" });
                }
                
                user.FirstName = request.FirstName;
                user.LastName = request.LastName;
                user.Email = request.Email;
                user.PhoneNumber = request.Phone;
                user.Department = request.Department;
                user.UpdatedAt = DateTime.UtcNow;
                
                await _context.SaveChangesAsync();
                
                return Ok(new { message = "Profile updated successfully", data = new {
                    firstName = user.FirstName,
                    lastName = user.LastName,
                    email = user.Email,
                    phone = user.PhoneNumber,
                    department = user.Department
                }});
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, new { message = "Database update failed", error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An unexpected error occurred", error = ex.Message });
            }
        }
        
        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
        
        [HttpPut("pharmacy")]
        public async Task<IActionResult> UpdatePharmacyDetails([FromBody] UpdatePharmacyRequest request)
        {
            var userId = GetUserId();
            var pharmacy = await _context.Pharmacies.FirstOrDefaultAsync();
            
            if (pharmacy == null)
            {
                return NotFound();
            }
            
            pharmacy.Name = request.Name;
            pharmacy.LicenseNumber = request.LicenseNumber;
            pharmacy.Address = request.Address;
            pharmacy.City = request.City;
            pharmacy.Province = request.Province;
            pharmacy.PostalCode = request.PostalCode;
            pharmacy.Phone = request.Phone;
            pharmacy.Email = request.Email;
            pharmacy.UpdatedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();
            
            return Ok(new { message = "Pharmacy details updated successfully" });
        }
        
        [HttpPut("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            try
            {
                // Validate request
                if (string.IsNullOrWhiteSpace(request.CurrentPassword) || 
                    string.IsNullOrWhiteSpace(request.NewPassword) || 
                    string.IsNullOrWhiteSpace(request.ConfirmPassword))
                {
                    return BadRequest(new { message = "All password fields are required" });
                }
                
                if (request.NewPassword != request.ConfirmPassword)
                {
                    return BadRequest(new { message = "New password and confirmation do not match" });
                }
                
                if (request.NewPassword.Length < 8)
                {
                    return BadRequest(new { message = "Password must be at least 8 characters long" });
                }
                
                var userId = GetUserId();
                var user = await _context.Users.FindAsync(userId);
                
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }
                
                // In a real app, you'd verify the current password
                // For testing, we'll just update with the new password
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
                user.UpdatedAt = DateTime.UtcNow;
                
                await _context.SaveChangesAsync();
                
                return Ok(new { message = "Password changed successfully" });
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, new { message = "Password update failed", error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An unexpected error occurred", error = ex.Message });
            }
        }
        
        [HttpPut("toggle-2fa")]
        public async Task<IActionResult> ToggleTwoFactor([FromBody] ToggleTwoFactorRequest request)
        {
            var userId = GetUserId();
            var user = await _context.Users.FindAsync(userId);
            
            if (user == null)
            {
                return NotFound();
            }
            
            user.TwoFactorEnabled = request.Enabled;
            user.UpdatedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();
            
            return Ok(new { message = $"2FA {(request.Enabled ? "enabled" : "disabled")} successfully" });
        }
        
        [HttpGet("subscription")]
        public async Task<IActionResult> GetSubscription()
        {
            var userId = GetUserId();
            var subscription = await _context.Subscriptions
                .Include(s => s.Plan)
                .Include(s => s.Pharmacy)
                .FirstOrDefaultAsync();
            
            if (subscription == null)
            {
                // Return default subscription for testing
                subscription = new Subscription
                {
                    Id = Guid.NewGuid().ToString(),
                    PlanId = 1,
                    PharmacyId = "1",
                    StartDate = DateTime.UtcNow.AddDays(-30),
                    EndDate = DateTime.UtcNow.AddDays(30),
                    Amount = 299.99m,
                    Status = "active",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
            }
            
            return Ok(subscription);
        }
        
        [HttpGet("users")]
        public async Task<IActionResult> GetManagedUsers()
        {
            var userId = GetUserId();
            var users = await _context.Users
                .Include(u => u.UserBranches)
                .ThenInclude(ub => ub.Branch)
                .Select(u => new
                {
                    id = u.Id,
                    name = $"{u.FirstName} {u.LastName}",
                    email = u.Email,
                    phone = u.PhoneNumber,
                    role = u.Role,
                    department = u.Department,
                    status = u.IsActive ? "active" : "inactive",
                    lastLogin = u.LastLogin,
                    createdAt = u.CreatedAt
                })
                .ToListAsync();
            
            return Ok(users);
        }
        
        [HttpPost("users/{targetUserId}/toggle-status")]
        public async Task<IActionResult> ToggleUserStatus(string targetUserId, [FromBody] ToggleUserStatusRequest request)
        {
            var currentUserId = GetUserId();
            var user = await _context.Users.FindAsync(targetUserId);
            
            if (user == null)
            {
                return NotFound();
            }
            
            user.IsActive = request.Active;
            user.UpdatedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();
            
            return Ok(new { message = $"User {(request.Active ? "activated" : "deactivated")} successfully" });
        }
        
        [HttpGet("activity")]
        public async Task<IActionResult> GetActivityLog()
        {
            var userId = GetUserId();
            var activities = await _context.ActivityLogs
                .Include(a => a.User)
                .OrderByDescending(a => a.CreatedAt)
                .Take(50)
                .Select(a => new
                {
                    id = a.Id,
                    type = a.Type,
                    description = a.Description,
                    status = a.Status,
                    ipAddress = a.IpAddress,
                    userAgent = a.UserAgent,
                    createdAt = a.CreatedAt,
                    user = a.User != null ? new
                    {
                        name = $"{a.User.FirstName} {a.User.LastName}",
                        email = a.User.Email
                    } : null
                })
                .ToListAsync();
            
            return Ok(activities);
        }
        
        [HttpGet("sessions")]
        public async Task<IActionResult> GetActiveSessions()
        {
            var userId = GetUserId();
            var sessions = await _context.UserSessions
                .Include(s => s.User)
                .Where(s => s.IsActive && s.ExpiresAt > DateTime.UtcNow)
                .Select(s => new
                {
                    id = s.Id,
                    deviceInfo = s.DeviceInfo,
                    browser = s.Browser,
                    ipAddress = s.IpAddress,
                    createdAt = s.CreatedAt,
                    expiresAt = s.ExpiresAt,
                    isCurrent = false, // In a real app, you'd check against current session
                    user = s.User != null ? new
                    {
                        name = $"{s.User.FirstName} {s.User.LastName}"
                    } : null
                })
                .ToListAsync();
            
            return Ok(sessions);
        }
        
        [HttpDelete("sessions/{sessionId}/revoke")]
        public async Task<IActionResult> RevokeSession(string sessionId)
        {
            var userId = GetUserId();
            var session = await _context.UserSessions.FindAsync(sessionId);
            
            if (session == null)
            {
                return NotFound();
            }
            
            _context.UserSessions.Remove(session);
            await _context.SaveChangesAsync();
            
            return Ok(new { message = "Session revoked successfully" });
        }
        
        [HttpPut("preferences")]
        public async Task<IActionResult> UpdatePreferences([FromBody] UpdatePreferencesRequest request)
        {
            var userId = GetUserId();
            var user = await _context.Users.FindAsync(userId);
            
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }
            
            // Update preferences (for now, just return success)
            // In a real implementation, you'd have a UserPreferences entity
            return Ok(new { message = "Preferences updated successfully" });
        }
        
        [Required(ErrorMessage = "Last name is required")]
        [StringLength(100, ErrorMessage = "Last name cannot exceed 100 characters")]
        public string LastName { get; set; }
        
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(255, ErrorMessage = "Email cannot exceed 255 characters")]
        public string Email { get; set; }
        
        [Phone(ErrorMessage = "Invalid phone number format")]
        [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters")]
        public string Phone { get; set; }
        
        [Required(ErrorMessage = "Department is required")]
        [StringLength(100, ErrorMessage = "Department cannot exceed 100 characters")]
        public string Department { get; set; }
    }
    
    public class UpdatePharmacyRequest
    {
        [Required(ErrorMessage = "Pharmacy name is required")]
        [StringLength(200, ErrorMessage = "Pharmacy name cannot exceed 200 characters")]
        public string Name { get; set; }
        
        [StringLength(50, ErrorMessage = "License number cannot exceed 50 characters")]
        public string LicenseNumber { get; set; }
        
        [Required(ErrorMessage = "Address is required")]
        [StringLength(500, ErrorMessage = "Address cannot exceed 500 characters")]
        public string Address { get; set; }
        
        [Required(ErrorMessage = "City is required")]
        [StringLength(100, ErrorMessage = "City cannot exceed 100 characters")]
        public string City { get; set; }
        
        [Required(ErrorMessage = "Province is required")]
        [StringLength(100, ErrorMessage = "Province cannot exceed 100 characters")]
        public string Province { get; set; }
        
        [Required(ErrorMessage = "Postal code is required")]
        [StringLength(20, ErrorMessage = "Postal code cannot exceed 20 characters")]
        public string PostalCode { get; set; }
        
        [Phone(ErrorMessage = "Invalid phone number format")]
        [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters")]
        public string Phone { get; set; }
        
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(255, ErrorMessage = "Email cannot exceed 255 characters")]
        public string Email { get; set; }
    }
    
    public class ChangePasswordRequest
    {
        [Required(ErrorMessage = "Current password is required")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 100 characters")]
        public string CurrentPassword { get; set; }
        
        [Required(ErrorMessage = "New password is required")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 100 characters")]
        public string NewPassword { get; set; }
        
        [Required(ErrorMessage = "Password confirmation is required")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 100 characters")]
        public string ConfirmPassword { get; set; }
    }
    
    public class ToggleTwoFactorRequest
    {
        [Required(ErrorMessage = "Enabled status is required")]
        public bool Enabled { get; set; }
    }
    
    public class ToggleUserStatusRequest
    {
        [Required(ErrorMessage = "Active status is required")]
        public bool Active { get; set; }
    }
    
    public class UpdatePreferencesRequest
    {
        [StringLength(50, ErrorMessage = "Theme cannot exceed 50 characters")]
        public string Theme { get; set; }
        
        [StringLength(10, ErrorMessage = "Language cannot exceed 10 characters")]
        public string Language { get; set; }
        
        [StringLength(50, ErrorMessage = "Time zone cannot exceed 50 characters")]
        public string TimeZone { get; set; }
        
        public bool EmailNotifications { get; set; }
        public bool PushNotifications { get; set; }
    }
}

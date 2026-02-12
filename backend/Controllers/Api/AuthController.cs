using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UmiHealthPOS.Data;
using UmiHealthPOS.Models;
using UmiHealthPOS.Services;
using System.Security.Cryptography;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using BCrypt.Net;
using Microsoft.Extensions.Configuration;

namespace UmiHealthPOS.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IJwtService _jwtService;
        private readonly IConfiguration _configuration;

        public AuthController(ApplicationDbContext context, IJwtService jwtService, IConfiguration configuration)
        {
            _context = context;
            _jwtService = jwtService;
            _configuration = configuration;
        }

        [HttpPost("signin")]
        public async Task<IActionResult> SignIn([FromBody] SignInRequest request)
        {
            if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest(new { message = "Email and password are required" });
            }

            var user = await _context.Users
                .Include(u => u.UserBranches)
                .ThenInclude(ub => ub.Branch)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

            if (user == null)
            {
                return Unauthorized(new { message = "Invalid email or password" });
            }

            if (user.Status != "active")
            {
                return Unauthorized(new { message = "Account is not active" });
            }

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return Unauthorized(new { message = "Invalid email or password" });
            }

            // Update last login
            user.LastLogin = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Generate JWT token
            var accessToken = _jwtService.GenerateAccessToken(user);
            var refreshToken = _jwtService.GenerateRefreshToken();

            // Store refresh token
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _context.SaveChangesAsync();

            var response = new SignInResponse
            {
                UserId = user.Id,
                Email = user.Email,
                Name = $"{user.FirstName} {user.LastName}",
                Role = user.Role,
                Branch = user.UserBranches.FirstOrDefault()?.Branch.Name,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                TokenType = "Bearer",
                ExpiresIn = 3600 // 1 hour
            };

            return Ok(response);
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.RefreshToken))
                {
                    return BadRequest(new { message = "Refresh token is required" });
                }

                // Find user with this refresh token
                var user = await _context.Users.FirstOrDefaultAsync(u => u.RefreshToken == request.RefreshToken);

                if (user == null || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
                {
                    return Unauthorized(new { message = "Invalid or expired refresh token" });
                }

                // Generate new tokens
                var accessToken = _jwtService.GenerateAccessToken(user);
                var newRefreshToken = _jwtService.GenerateRefreshToken();

                // Update refresh token
                user.RefreshToken = newRefreshToken;
                user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
                await _context.SaveChangesAsync();

                var response = new RefreshTokenResponse
                {
                    AccessToken = accessToken,
                    RefreshToken = newRefreshToken,
                    ExpiresIn = 3600
                };

                return Ok(response);
            }
            catch
            {
                return StatusCode(500, new { message = "Internal server error during token refresh" });
            }
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
        {
            try
            {
                // Get user ID from current user (from JWT token)
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!string.IsNullOrEmpty(userId))
                {
                    var user = await _context.Users.FindAsync(userId);
                    if (user != null)
                    {
                        // Invalidate refresh token
                        user.RefreshToken = null;
                        user.RefreshTokenExpiryTime = null;
                        await _context.SaveChangesAsync();
                    }
                }

                return Ok(new { message = "Logged out successfully" });
            }
            catch
            {
                return StatusCode(500, new { message = "Internal server error during logout" });
            }
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUser()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var user = await _context.Users
                    .Include(u => u.UserBranches)
                    .ThenInclude(ub => ub.Branch)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                var tenant = await _context.Pharmacies
                .Include(p => p.Subscriptions)
                .ThenInclude(s => s.Plan)
                .FirstOrDefaultAsync(p => p.Id == user.TenantId);

                var response = new
                {
                    userId = user.Id,
                    email = user.Email,
                    name = $"{user.FirstName} {user.LastName}",
                    role = user.Role,
                    tenantId = user.TenantId,
                    tenantName = tenant?.Name,
                    plan = tenant?.Subscriptions.FirstOrDefault()?.Plan?.Name ?? "starter",
                    lastLogin = user.LastLogin,
                    createdAt = user.CreatedAt
                };

                return Ok(response);
            }
            catch
            {
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPost("signup")]
        public async Task<IActionResult> Signup([FromBody] SignupRequest request)
        {
            try
            {
                // Validate required fields
                if (string.IsNullOrWhiteSpace(request.OrganizationName) ||
                    string.IsNullOrWhiteSpace(request.FirstName) ||
                    string.IsNullOrWhiteSpace(request.LastName) ||
                    string.IsNullOrWhiteSpace(request.Email) ||
                    string.IsNullOrWhiteSpace(request.Password))
                {
                    return BadRequest(new { message = "All required fields must be provided" });
                }

                // Validate email format
                if (!IsValidEmail(request.Email))
                {
                    return BadRequest(new { message = "Invalid email format" });
                }

                // Check if email already exists
                if (await _context.Users.AnyAsync(u => u.Email.ToLower() == request.Email.ToLower()))
                {
                    return BadRequest(new { message = "Email already exists." });
                }

                // Check if pharmacy name already exists
                if (await _context.Pharmacies.AnyAsync(p => p.Name.ToLower() == request.OrganizationName.ToLower()))
                {
                    return BadRequest(new { message = "A pharmacy with this name already exists. Please choose a different name." });
                }

                // Create user
                var user = new User
                {
                    Id = Guid.NewGuid().ToString(),
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Email = request.Email,
                    NormalizedEmail = request.Email.ToLower(),
                    PhoneNumber = request.Phone ?? "+260 000 000 000",
                    Role = request.Role ?? "admin",
                    Department = "Management",
                    Status = "active",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    EmailConfirmed = false,
                    PhoneNumberConfirmed = false
                };

                _context.Users.Add(user);

                // Create pharmacy/organization
                var pharmacy = new Pharmacy
                {
                    Name = request.OrganizationName,
                    LicenseNumber = $"ZMP-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}",
                    Address = "123 Main Street, Lusaka",
                    City = "Lusaka",
                    Province = "Lusaka Province",
                    PostalCode = "10101",
                    Phone = request.Phone ?? "+260 000 000 000",
                    Email = request.Email,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Pharmacies.Add(pharmacy);

                // Create 14-day trial subscription
                var subscription = new Subscription
                {
                    PharmacyId = pharmacy.Id,
                    PlanId = 1, // Basic plan ID
                    Status = "trial",
                    StartDate = DateTime.UtcNow,
                    EndDate = DateTime.UtcNow.AddDays(14),
                    AutoRenew = false,
                    TrialUsed = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Subscriptions.Add(subscription);
                await _context.SaveChangesAsync();

                // Assign user to first branch or create default branch
                var branch = await _context.Branches.FirstOrDefaultAsync();
                if (branch == null)
                {
                    branch = new Branch
                    {
                        Name = $"{request.OrganizationName} - Main Branch",
                        Address = "123 Main Street, Lusaka",
                        Region = "Lusaka Province",
                        Phone = request.Phone ?? "+260 000 000 000",
                        Email = request.Email,
                        ManagerName = $"{request.FirstName} {request.LastName}",
                        ManagerPhone = request.Phone ?? "+260 000 000 000",
                        OperatingHours = "08:00-18:00",
                        Status = "active",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.Branches.Add(branch);
                    await _context.SaveChangesAsync();
                }

                // Create user-branch relationship
                var userBranch = new UserBranch
                {
                    UserId = user.Id,
                    BranchId = branch.Id,
                    UserRole = user.Role,
                    Permission = user.Role == "admin" ? "admin" : "write",
                    IsActive = true,
                    AssignedAt = DateTime.UtcNow,
                    User = user,
                    Branch = branch
                };

                _context.UserBranches.Add(userBranch);
                await _context.SaveChangesAsync();

                // Generate JWT tokens
                var claims = new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
                    new Claim(ClaimTypes.Role, user.Role),
                    new Claim("branch", branch?.Name ?? string.Empty),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                };

                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(claims),
                    Expires = DateTime.UtcNow.AddMinutes(30), // 30 minutes inactivity timeout
                    Issuer = _configuration["Jwt:Issuer"],
                    Audience = _configuration["Jwt:Audience"],
                    SigningCredentials = new SigningCredentials(
                        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"])),
                        SecurityAlgorithms.HmacSha256Signature)
                };

                var tokenHandler = new JwtSecurityTokenHandler();
                var securityToken = tokenHandler.CreateToken(tokenDescriptor);
                var tokenString = tokenHandler.WriteToken(securityToken);

                var refreshToken = Guid.NewGuid().ToString();

                // Store refresh token with inactivity tracking
                var userSession = new UserSession
                {
                    UserId = user.Id,
                    Token = refreshToken,
                    DeviceInfo = "Web Browser",
                    Browser = "Unknown",
                    IpAddress = "127.0.0.1",
                    ExpiresAt = DateTime.UtcNow.AddMinutes(30), // 30 minutes inactivity
                    LastAccessAt = DateTime.UtcNow, // Track last activity
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.UserSessions.Add(userSession);
                await _context.SaveChangesAsync();

                // Return response matching frontend expectations
                return Ok(new
                {
                    tenantId = pharmacy.Id.ToString(),
                    userId = user.Id,
                    accessToken = tokenString,
                    refreshToken = refreshToken,
                    plan = "trial", // Always start with trial plan
                    trialEndDate = DateTime.UtcNow.AddDays(14).ToString("yyyy-MM-dd"),
                    isTrial = true,
                    message = "Account created successfully. Your 14-day trial has started!"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
            }
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] CreateUserRequest request)
        {
            // Validate role
            var validRoles = new[] { "admin", "pharmacist", "cashier" };
            if (!validRoles.Contains(request.Role.ToLower()))
            {
                return BadRequest(new { message = "Invalid role. Must be admin, pharmacist, or cashier." });
            }

            // Check if email already exists
            if (await _context.Users.AnyAsync(u => u.Email.ToLower() == request.Email.ToLower()))
            {
                return BadRequest(new { message = "Email already exists." });
            }

            var user = new User
            {
                Id = Guid.NewGuid().ToString(),
                // Split name into first and last name
                FirstName = request.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? request.Name,
                LastName = string.Join(" ", request.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Skip(1)),
                Email = request.Email,
                PhoneNumber = request.Phone,
                Role = request.Role,
                Status = "active",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);

            // Assign to branch if specified
            if (!string.IsNullOrEmpty(request.Branch))
            {
                var branch = await _context.Branches
                    .FirstOrDefaultAsync(b => b.Name == request.Branch || b.Id.ToString() == request.Branch);

                if (branch != null)
                {
                    var userBranch = new UserBranch
                    {
                        UserId = user.Id,
                        BranchId = branch.Id,
                        UserRole = request.Role,
                        IsActive = true,
                        AssignedAt = DateTime.UtcNow,
                        User = user
                    };
                    _context.UserBranches.Add(userBranch);
                }
            }

            await _context.SaveChangesAsync();

            var response = new UserResponse
            {
                Id = user.Id,
                Name = $"{user.FirstName} {user.LastName}",
                Email = user.Email,
                Phone = user.PhoneNumber,
                Role = user.Role,
                Branch = request.Branch,
                Status = user.Status,
                LastLogin = user.LastLogin,
                CreatedAt = user.CreatedAt
            };

            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, response);
        }

        [HttpGet("users/{id}")]
        public async Task<ActionResult<UserResponse>> GetUser(string id)
        {
            var user = await _context.Users
                .Include(u => u.UserBranches)
                .ThenInclude(ub => ub.Branch)
                .Where(u => u.Id == id)
                .Select(u => new UserResponse
                {
                    Id = u.Id,
                    Name = $"{u.FirstName} {u.LastName}",
                    Email = u.Email,
                    Phone = u.PhoneNumber,
                    Role = u.Role,
                    Branch = u.UserBranches.FirstOrDefault() != null ? u.UserBranches.FirstOrDefault().Branch.Name : null,
                    Status = u.Status,
                    LastLogin = u.LastLogin,
                    CreatedAt = u.CreatedAt
                })
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return NotFound();
            }

            return Ok(user);
        }

        private string GenerateJwtToken(User user)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var secretKey = jwtSettings["Key"] ?? throw new InvalidOperationException("JWT Key not configured");
            var issuer = jwtSettings["Issuer"] ?? "UmiHealthPOS";
            var audience = jwtSettings["Audience"] ?? "UmiHealthPOS";

            var key = Encoding.UTF8.GetBytes(secretKey);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("branch", user.UserBranches.FirstOrDefault()?.Branch.Name ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(24),
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }

        private bool VerifyPassword(string password, string hash)
        {
            var computedHash = HashPassword(password);
            return computedHash == hash;
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
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
    }

    public class SignInRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class SignInResponse
    {
        public string UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? Branch { get; set; }
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public string TokenType { get; set; } = string.Empty;
        public int ExpiresIn { get; set; }
    }

    public class CreateUserRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Branch { get; set; } = string.Empty;
    }

    public class UserResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? Branch { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? LastLogin { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class SignupRequest
    {
        public string OrganizationName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Role { get; set; } = "admin";
        public string Password { get; set; } = string.Empty;
    }

    public class RefreshTokenRequest
    {
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class RefreshTokenResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public int ExpiresIn { get; set; }
    }

    public class LogoutRequest
    {
        public string RefreshToken { get; set; } = string.Empty;
    }
}

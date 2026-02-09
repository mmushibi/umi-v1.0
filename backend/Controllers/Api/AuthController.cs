using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UmiHealthPOS.Data;
using UmiHealthPOS.Models;
using System.Security.Cryptography;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

namespace UmiHealthPOS.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        
        public AuthController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
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
            
            if (!VerifyPassword(request.Password, user.PasswordHash))
            {
                return Unauthorized(new { message = "Invalid email or password" });
            }
            
            // Update last login
            user.LastLogin = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            
            // Generate JWT token
            var token = GenerateJwtToken(user);
            
            var response = new SignInResponse
            {
                UserId = user.Id,
                Email = user.Email,
                Name = user.Name,
                Role = user.Role,
                Branch = user.UserBranches.FirstOrDefault()?.Branch.Name,
                AccessToken = token,
                TokenType = "Bearer"
            };
            
            return Ok(response);
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
                Name = request.Name,
                Email = request.Email,
                Phone = request.Phone,
                Role = request.Role,
                Status = "active",
                PasswordHash = HashPassword(request.Password),
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
                        AssignedAt = DateTime.UtcNow
                    };
                    _context.UserBranches.Add(userBranch);
                }
            }
            
            await _context.SaveChangesAsync();
            
            var response = new UserResponse
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Phone = user.Phone,
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
                    Name = u.Name,
                    Email = u.Email,
                    Phone = u.Phone,
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
                new Claim(ClaimTypes.Name, user.Name),
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
        public string TokenType { get; set; } = string.Empty;
    }
}

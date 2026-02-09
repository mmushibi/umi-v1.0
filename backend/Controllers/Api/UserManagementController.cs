using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UmiHealthPOS.Data;
using UmiHealthPOS.Models;
using System.Security.Cryptography;
using System.Text;

namespace UmiHealthPOS.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserManagementController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        
        public UserManagementController(ApplicationDbContext context)
        {
            _context = context;
        }
        
        [HttpGet("users")]
        public async Task<ActionResult<IEnumerable<UserResponse>>> GetUsers()
        {
            var users = await _context.Users
                .Include(u => u.UserBranches)
                .ThenInclude(ub => ub.Branch)
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
                .ToListAsync();
                
            return Ok(users);
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
        
        [HttpPost("users")]
        public async Task<ActionResult<UserResponse>> CreateUser([FromBody] CreateUserRequest request)
        {
            // Validate role
            var validRoles = new[] { "admin", "pharmacist", "cashier" };
            if (!validRoles.Contains(request.Role.ToLower()))
            {
                return BadRequest("Invalid role. Must be admin, pharmacist, or cashier.");
            }
            
            // Check if email already exists
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            {
                return BadRequest("Email already exists.");
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
        
        [HttpPut("users/{id}")]
        public async Task<IActionResult> UpdateUser(string id, [FromBody] CreateUserRequest request)
        {
            var user = await _context.Users
                .Include(u => u.UserBranches)
                .FirstOrDefaultAsync(u => u.Id == id);
                
            if (user == null)
            {
                return NotFound();
            }
            
            // Validate role
            var validRoles = new[] { "admin", "pharmacist", "cashier" };
            if (!validRoles.Contains(request.Role.ToLower()))
            {
                return BadRequest("Invalid role. Must be admin, pharmacist, or cashier.");
            }
            
            // Check if email already exists (excluding current user)
            if (await _context.Users.AnyAsync(u => u.Email == request.Email && u.Id != id))
            {
                return BadRequest("Email already exists.");
            }
            
            user.Name = request.Name;
            user.Email = request.Email;
            user.Phone = request.Phone;
            user.Role = request.Role;
            user.UpdatedAt = DateTime.UtcNow;
            
            // Update password if provided
            if (!string.IsNullOrEmpty(request.Password))
            {
                user.PasswordHash = HashPassword(request.Password);
            }
            
            // Update branch assignment
            if (!string.IsNullOrEmpty(request.Branch))
            {
                // Remove existing branch assignments
                _context.UserBranches.RemoveRange(user.UserBranches);
                
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
            
            return NoContent();
        }
        
        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            
            // Remove user branch assignments
            var userBranches = await _context.UserBranches
                .Where(ub => ub.UserId == id)
                .ToListAsync();
            _context.UserBranches.RemoveRange(userBranches);
            
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            
            return NoContent();
        }
        
        [HttpPost("users/import-csv")]
        public async Task<ActionResult<IEnumerable<UserResponse>>> ImportUsers([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }
            
            var users = new List<UserResponse>();
            var validRoles = new[] { "admin", "pharmacist", "cashier" };
            
            using (var reader = new StreamReader(file.OpenReadStream()))
            {
                var header = await reader.ReadLineAsync(); // Skip header
                if (header == null)
                {
                    return BadRequest("Empty file.");
                }
                
                int lineNum = 2;
                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync();
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    
                    var values = line.Split(',').Select(v => v.Trim().Replace("\"", "")).ToArray();
                    if (values.Length < 6)
                    {
                        return BadRequest($"Invalid format at line {lineNum}. Expected 6 columns.");
                    }
                    
                    var name = values[0];
                    var email = values[1];
                    var phone = values[2];
                    var role = values[3].ToLower();
                    var branch = values[4];
                    var status = values[5].ToLower();
                    
                    // Validate
                    if (!validRoles.Contains(role))
                    {
                        return BadRequest($"Invalid role '{role}' at line {lineNum}. Must be admin, pharmacist, or cashier.");
                    }
                    
                    if (await _context.Users.AnyAsync(u => u.Email == email))
                    {
                        return BadRequest($"Email '{email}' already exists at line {lineNum}.");
                    }
                    
                    var user = new User
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = name,
                        Email = email,
                        Phone = phone,
                        Role = role,
                        Status = status == "active" ? "active" : "inactive",
                        PasswordHash = HashPassword("Temp123!"), // Default password
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    
                    _context.Users.Add(user);
                    
                    // Assign to branch if specified
                    if (!string.IsNullOrEmpty(branch))
                    {
                        var branchEntity = await _context.Branches
                            .FirstOrDefaultAsync(b => b.Name == branch || b.Id.ToString() == branch);
                            
                        if (branchEntity != null)
                        {
                            var userBranch = new UserBranch
                            {
                                UserId = user.Id,
                                BranchId = branchEntity.Id,
                                UserRole = role,
                                IsActive = true,
                                AssignedAt = DateTime.UtcNow
                            };
                            _context.UserBranches.Add(userBranch);
                        }
                    }
                    
                    users.Add(new UserResponse
                    {
                        Id = user.Id,
                        Name = user.Name,
                        Email = user.Email,
                        Phone = user.Phone,
                        Role = user.Role,
                        Branch = branch,
                        Status = user.Status,
                        LastLogin = user.LastLogin,
                        CreatedAt = user.CreatedAt
                    });
                    
                    lineNum++;
                }
            }
            
            await _context.SaveChangesAsync();
            return Ok(users);
        }
        
        [HttpGet("users/export-csv")]
        public async Task<IActionResult> ExportUsers()
        {
            var users = await _context.Users
                .Include(u => u.UserBranches)
                .ThenInclude(ub => ub.Branch)
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
                .ToListAsync();
            
            var csv = new StringBuilder();
            csv.AppendLine("Name,Email,Phone,Role,Branch,Status,Last Login,Created Date");
            
            foreach (var user in users)
            {
                csv.AppendLine($"\"{user.Name}\",\"{user.Email}\",\"{user.Phone}\",\"{user.Role}\",\"{user.Branch ?? "All Branches"}\",\"{user.Status}\",\"{user.LastLogin?.ToString() ?? "Never"}\",\"{user.CreatedAt:yyyy-MM-dd HH:mm:ss}\"");
            }
            
            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", $"users_export_{DateTime.UtcNow:yyyy-MM-dd}.csv");
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
}

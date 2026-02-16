using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UmiHealthPOS.Data;
using UmiHealthPOS.Models;
using System.Security.Cryptography;
using System.Text;

namespace UmiHealthPOS.Controllers.Api
{
    [ApiController]
    [Route("api/usermanagement")]
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
                    Name = $"{u.FirstName} {u.LastName}",
                    Email = u.Email,
                    Phone = u.PhoneNumber,
                    Address = u.Address,
                    Role = u.Role,
                    PharmacyId = u.TenantId ?? "",
                    PharmacyName = "Default Pharmacy",
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
                    Name = $"{u.FirstName} {u.LastName}",
                    Email = u.Email,
                    Phone = u.PhoneNumber,
                    Address = u.Address,
                    Role = u.Role,
                    PharmacyId = u.TenantId ?? "",
                    PharmacyName = "Default Pharmacy",
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
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Hash the password
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(request.Password));
                var passwordHash = Convert.ToBase64String(hashedBytes);

                var user = new User
                {
                    Id = Guid.NewGuid().ToString(),
                    FirstName = request.Name.Split(' ')[0],
                    LastName = request.Name.Split(' ').Length > 1 ? request.Name.Split(' ')[1] : "",
                    Email = request.Email,
                    NormalizedEmail = request.Email.ToUpper(),
                    PhoneNumber = request.Phone,
                    Address = request.Address,
                    Role = request.Role.ToString(),
                    PasswordHash = passwordHash,
                    Status = "active",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    TenantId = request.PharmacyId
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Create user branch assignment if branch is specified
                if (!string.IsNullOrEmpty(request.Branch))
                {
                    var userBranch = new UserBranch
                    {
                        UserId = user.Id,
                        BranchId = 1, // Default branch for now
                        UserRole = request.Role.ToString(),
                        Permission = "read",
                        IsActive = true,
                        AssignedAt = DateTime.UtcNow
                    };
                    _context.UserBranches.Add(userBranch);
                }

                await _context.SaveChangesAsync();

                var response = new UserResponse
                {
                    Id = user.Id,
                    Name = $"{user.FirstName} {user.LastName}",
                    Email = user.Email,
                    Phone = user.PhoneNumber,
                    Address = user.Address,
                    Role = user.Role,
                    PharmacyId = user.TenantId ?? "",
                    PharmacyName = "Default Pharmacy",
                    Branch = request.Branch,
                    Status = user.Status,
                    LastLogin = user.LastLogin,
                    CreatedAt = user.CreatedAt
                };

                return CreatedAtAction(nameof(GetUser), new { id = user.Id }, response);
            }
        }

        [HttpPut("users/{id}")]
        public async Task<IActionResult> UpdateUser(string id, [FromBody] CreateUserRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Update user properties
            var nameParts = request.Name.Split(' ');
            user.FirstName = nameParts[0];
            user.LastName = nameParts.Length > 1 ? nameParts[1] : "";
            user.Email = request.Email;
            user.NormalizedEmail = request.Email.ToUpper();
            user.PhoneNumber = request.Phone;
            user.Address = request.Address;
            user.Role = request.Role.ToString();
            user.UpdatedAt = DateTime.UtcNow;
            user.TenantId = request.PharmacyId;

            // Hash the password if provided
            if (!string.IsNullOrEmpty(request.Password))
            {
                using (var sha256 = SHA256.Create())
                {
                    var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(request.Password));
                    user.PasswordHash = Convert.ToBase64String(hashedBytes);
                }
            }

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            // Update user branch assignment
            if (!string.IsNullOrEmpty(request.Branch))
            {
                var existingBranch = await _context.UserBranches
                    .FirstOrDefaultAsync(ub => ub.UserId == id);

                if (existingBranch == null)
                {
                    var userBranch = new UserBranch
                    {
                        UserId = id,
                        BranchId = 1, // Default branch for now
                        UserRole = request.Role.ToString(),
                        Permission = "read",
                        IsActive = true,
                        AssignedAt = DateTime.UtcNow
                    };
                    _context.UserBranches.Add(userBranch);
                }
            }

            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPatch("users/{id}/status")]
        public async Task<IActionResult> UpdateUserStatus(string id, [FromBody] UpdateStatusRequest request)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            user.Status = request.Status.ToString();
            user.UpdatedAt = DateTime.UtcNow;

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

        [HttpGet("pharmacies/search")]
        public async Task<ActionResult<IEnumerable<PharmacySearchResult>>> SearchPharmacies([FromQuery] string query)
        {
            if (string.IsNullOrEmpty(query) || query.Length < 2)
            {
                return BadRequest("Search query must be at least 2 characters long.");
            }

            var pharmacies = await _context.Pharmacies
                .Include(p => p.Subscriptions)
                .Where(p => p.IsActive &&
                           (p.Name.ToLower().Contains(query.ToLower()) ||
                            p.Address.ToLower().Contains(query.ToLower()) ||
                            p.City.ToLower().Contains(query.ToLower())))
                .Select(p => new PharmacySearchResult
                {
                    Id = p.Id,
                    Name = p.Name,
                    Address = p.Address,
                    City = p.City,
                    Province = p.Province,
                    Phone = p.Phone,
                    Email = p.Email,
                    Branches = new List<PharmacyBranchInfo>() // Empty list for now
                })
                .Take(10) // Limit results for performance
                .ToListAsync();

            return Ok(pharmacies);
        }

        [HttpPost("users/import-csv")]
        public async Task<ActionResult<IEnumerable<UserResponse>>> ImportUsers([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            var users = new List<UserResponse>();
            // TODO: Implement CSV import logic

            return Ok(users);
        }

        [HttpGet("users/export-csv")]
        public async Task<IActionResult> ExportUsers()
        {
            var users = await _context.Users
                .Select(u => new UserResponse
                {
                    Id = u.Id,
                    Name = $"{u.FirstName} {u.LastName}",
                    Email = u.Email,
                    Phone = u.PhoneNumber,
                    Address = u.Address,
                    Role = u.Role,
                    PharmacyId = u.TenantId ?? "",
                    PharmacyName = "Default Pharmacy",
                    Branch = u.UserBranches.FirstOrDefault() != null ? u.UserBranches.FirstOrDefault().Branch.Name : null,
                    Status = u.Status,
                    LastLogin = u.LastLogin,
                    CreatedAt = u.CreatedAt
                })
                .ToListAsync();

            var csv = new StringBuilder();
            csv.AppendLine("Id,Name,Email,Phone,Address,Role,Pharmacy,Branch,Status,CreatedAt");

            foreach (var user in users)
            {
                csv.AppendLine($"{user.Id},{user.Name},{user.Email},{user.Phone},{user.Address},{user.Role},{user.PharmacyName},{user.Branch},{user.Status},{user.CreatedAt:yyyy-MM-dd HH:mm:ss}");
            }

            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", "users.csv");
        }
    }
}



using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UmiHealthPOS.Data;
using UmiHealthPOS.Models;
using UmiHealthPOS.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace UmiHealthPOS.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BranchController : ControllerBase
    {
        private readonly IBranchService _branchService;
        private readonly ApplicationDbContext _context;

        public BranchController(IBranchService branchService, ApplicationDbContext context)
        {
            _branchService = branchService;
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Branch>>> GetAllBranches()
        {
            try
            {
                var branches = await _branchService.GetAllBranchesAsync();
                return Ok(branches);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving branches", error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Branch>> GetBranch(int id)
        {
            try
            {
                var branch = await _branchService.GetBranchByIdAsync(id);
                if (branch == null)
                {
                    return NotFound(new { message = "Branch not found" });
                }

                if (!await CanAccessBranch(id))
                {
                    return Forbid();
                }

                return Ok(branch);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving branch", error = ex.Message });
            }
        }

        [HttpPost]
        [Authorize(Roles = "TenantAdmin")]
        public async Task<ActionResult<Branch>> CreateBranch(Branch branch)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var createdBranch = await _branchService.CreateBranchAsync(branch);
                return CreatedAtAction(nameof(GetBranch), new { id = createdBranch.Id }, createdBranch);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error creating branch", error = ex.Message });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "TenantAdmin")]
        public async Task<ActionResult<Branch>> UpdateBranch(int id, Branch branch)
        {
            try
            {
                if (id != branch.Id)
                {
                    return BadRequest(new { message = "Branch ID mismatch" });
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var updatedBranch = await _branchService.UpdateBranchAsync(id, branch);
                return Ok(updatedBranch);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error updating branch", error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "TenantAdmin")]
        public async Task<ActionResult> DeleteBranch(int id)
        {
            try
            {
                var success = await _branchService.DeleteBranchAsync(id);
                if (!success)
                {
                    return NotFound(new { message = "Branch not found" });
                }

                return Ok(new { message = "Branch deleted successfully" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error deleting branch", error = ex.Message });
            }
        }

        [HttpGet("{id}/users")]
        public async Task<ActionResult<IEnumerable<User>>> GetBranchUsers(int id)
        {
            try
            {
                if (!await CanAccessBranch(id))
                {
                    return Forbid();
                }

                var users = await _branchService.GetBranchUsersAsync(id);
                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving branch users", error = ex.Message });
            }
        }

        [HttpGet("{id}/users/available")]
        [Authorize(Roles = "TenantAdmin")]
        public async Task<ActionResult<IEnumerable<User>>> GetAvailableUsersForBranch(int id)
        {
            try
            {
                var users = await _branchService.GetUsersNotInBranchAsync(id);
                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving available users", error = ex.Message });
            }
        }

        [HttpPost("{branchId}/users/{userId}")]
        [Authorize(Roles = "TenantAdmin")]
        public async Task<ActionResult> AssignUserToBranch(int branchId, string userId, [FromBody] AssignUserRequest request)
        {
            try
            {
                var success = await _branchService.AssignUserToBranchAsync(userId, branchId, request?.Permission ?? "read");
                if (!success)
                {
                    return BadRequest(new { message = "Failed to assign user to branch" });
                }

                return Ok(new { message = "User assigned to branch successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error assigning user to branch", error = ex.Message });
            }
        }

        [HttpDelete("{branchId}/users/{userId}")]
        [Authorize(Roles = "TenantAdmin")]
        public async Task<ActionResult> RemoveUserFromBranch(int branchId, string userId)
        {
            try
            {
                var success = await _branchService.RemoveUserFromBranchAsync(userId, branchId);
                if (!success)
                {
                    return BadRequest(new { message = "Failed to remove user from branch" });
                }

                return Ok(new { message = "User removed from branch successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error removing user from branch", error = ex.Message });
            }
        }

        [HttpGet("{id}/statistics")]
        public async Task<ActionResult<BranchStatistics>> GetBranchStatistics(int id)
        {
            try
            {
                if (!await CanAccessBranch(id))
                {
                    return Forbid();
                }

                var statistics = await _branchService.GetBranchStatisticsAsync(id);
                return Ok(statistics);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving branch statistics", error = ex.Message });
            }
        }

        [HttpGet("dashboard")]
        public async Task<ActionResult<BranchDashboardData>> GetBranchDashboard()
        {
            try
            {
                // Get all branches
                var allBranches = await _branchService.GetAllBranchesAsync();
                
                // Get current user's branch assignments
                var userBranches = await _context.UserBranches
                    .Where(ub => ub.UserId == GetUserId() && ub.IsActive)
                    .Select(ub => ub.BranchId)
                    .ToListAsync();
                
                // Filter branches based on user role and assignments
                var accessibleBranches = allBranches.Where(b => 
                    GetUserRole() == "TenantAdmin" || userBranches.Contains(b.Id)).ToList();
                
                var totalBranches = accessibleBranches.Count;
                var activeBranches = accessibleBranches.Count(b => b.Status == "active");
                var totalStaff = await _context.UserBranches
                    .Where(ub => accessibleBranches.Select(b => b.Id).Contains(ub.BranchId) && ub.IsActive)
                    .CountAsync();
                var monthlyRevenue = accessibleBranches.Sum(b => b.MonthlyRevenue);

                return Ok(new BranchDashboardData
                {
                    TotalBranches = totalBranches,
                    ActiveBranches = activeBranches,
                    TotalStaff = totalStaff,
                    MonthlyRevenue = monthlyRevenue,
                    Branches = accessibleBranches
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving dashboard data", error = ex.Message });
            }
        }

        [HttpGet("export")]
        [Authorize(Roles = "TenantAdmin")]
        public async Task<IActionResult> ExportBranches()
        {
            try
            {
                var branches = await _branchService.GetAllBranchesAsync();
                var csv = new System.Text.StringBuilder();
                
                // CSV Header
                csv.AppendLine("ID,Name,Address,City,Region,Phone,Email,ManagerName,ManagerPhone,OperatingHours,Status,MonthlyRevenue,StaffCount,CreatedAt");
                
                // CSV Data
                foreach (var branch in branches)
                {
                    csv.AppendLine($"{branch.Id}," +
                               $"\"{branch.Name}\"," +
                               $"\"{branch.Address}\"," +
                               $"\"{branch.City}\"," +
                               $"\"{branch.Region}\"," +
                               $"\"{branch.Phone}\"," +
                               $"\"{branch.Email}\"," +
                               $"\"{branch.ManagerName}\"," +
                               $"\"{branch.ManagerPhone}\"," +
                               $"\"{branch.OperatingHours}\"," +
                               $"{branch.Status}," +
                               $"{branch.MonthlyRevenue}," +
                               $"{branch.StaffCount}," +
                               $"{branch.CreatedAt:yyyy-MM-dd HH:mm:ss}");
                }
                
                var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
                return File(bytes, "text/csv", $"branches_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error exporting branches", error = ex.Message });
            }
        }

        private async Task<bool> CanAccessBranch(int branchId)
        {
            var userRole = GetUserRole();
            
            // Tenant admins can access all branches
            if (userRole == "TenantAdmin")
                return true;

            var userId = GetUserId();
            
            // Check if user is assigned to this branch
            return await _context.UserBranches
                .AnyAsync(ub => ub.UserId == userId && ub.BranchId == branchId && ub.IsActive);
        }

        private string GetUserRole()
        {
            return User.FindFirst("role")?.Value ?? "Cashier";
        }

        private string GetUserId()
        {
            return User.FindFirst("nameid")?.Value ?? throw new UnauthorizedAccessException("User ID not found in token");
        }
    }

    public class AssignUserRequest
    {
        public string Permission { get; set; } = "read";
    }

    public class BranchDashboardData
    {
        public int TotalBranches { get; set; }
        public int ActiveBranches { get; set; }
        public int TotalStaff { get; set; }
        public decimal MonthlyRevenue { get; set; }
        public List<Branch> Branches { get; set; } = new();
    }
}

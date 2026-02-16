using Microsoft.EntityFrameworkCore;
using UmiHealthPOS.Data;
using UmiHealthPOS.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace UmiHealthPOS.Services
{
    public interface IBranchService
    {
        Task<IEnumerable<Branch>> GetAllBranchesAsync();
        Task<Branch?> GetBranchByIdAsync(int id);
        Task<Branch> CreateBranchAsync(Branch branch);
        Task<Branch> UpdateBranchAsync(int id, Branch branch);
        Task<bool> DeleteBranchAsync(int id);
        Task<IEnumerable<UserAccount>> GetBranchUsersAsync(int branchId);
        Task<bool> AssignUserToBranchAsync(string userId, int branchId, string permission = "read");
        Task<bool> RemoveUserFromBranchAsync(string userId, int branchId);
        Task<IEnumerable<UserAccount>> GetUsersNotInBranchAsync(int branchId);
        Task<BranchStatistics> GetBranchStatisticsAsync(int branchId);
    }

    public class BranchService : IBranchService
    {
        private readonly ApplicationDbContext _context;

        public BranchService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Branch>> GetAllBranchesAsync()
        {
            return await _context.Branches
                .Where(b => b.IsActive)
                .Include(b => b.Users)
                .OrderBy(b => b.Name)
                .ToListAsync();
        }

        public async Task<Branch?> GetBranchByIdAsync(int id)
        {
            return await _context.Branches
                .Include(b => b.Users)
                .Include(b => b.UserBranches)
                    .ThenInclude(ub => ub.User)
                .FirstOrDefaultAsync(b => b.Id == id && b.IsActive);
        }

        public async Task<Branch> CreateBranchAsync(Branch branch)
        {
            branch.CreatedAt = DateTime.UtcNow;
            branch.UpdatedAt = DateTime.UtcNow;
            branch.IsActive = true;
            branch.Status = "active";

            _context.Branches.Add(branch);
            await _context.SaveChangesAsync();

            return branch;
        }

        public async Task<Branch> UpdateBranchAsync(int id, Branch branch)
        {
            var existingBranch = await _context.Branches.FindAsync(id);
            if (existingBranch == null)
                throw new KeyNotFoundException($"Branch with ID {id} not found");

            existingBranch.Name = branch.Name;
            existingBranch.Address = branch.Address;
            existingBranch.City = branch.City;
            existingBranch.Region = branch.Region;
            existingBranch.PostalCode = branch.PostalCode;
            existingBranch.Phone = branch.Phone;
            existingBranch.Email = branch.Email;
            existingBranch.ManagerName = branch.ManagerName;
            existingBranch.ManagerPhone = branch.ManagerPhone;
            existingBranch.OperatingHours = branch.OperatingHours;
            existingBranch.Status = branch.Status;
            existingBranch.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return existingBranch;
        }

        public async Task<bool> DeleteBranchAsync(int id)
        {
            var branch = await _context.Branches.FindAsync(id);
            if (branch == null)
                return false;

            // Check if branch has users
            var hasUsers = await _context.UserBranches
                .AnyAsync(ub => ub.BranchId == id && ub.IsActive);

            if (hasUsers)
                throw new InvalidOperationException("Cannot delete branch with assigned users. Please reassign users first.");

            branch.IsActive = false;
            branch.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<UserAccount>> GetBranchUsersAsync(int branchId)
        {
            return await _context.UserBranches
                .Where(ub => ub.BranchId == branchId && ub.IsActive)
                .Include(ub => ub.User)
                .Select(ub => ub.User)
                .Where(u => u.IsActive)
                .ToListAsync();
        }

        public async Task<bool> AssignUserToBranchAsync(string userId, int branchId, string permission = "read")
        {
            // Check if user exists
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return false;

            // Check if branch exists
            var branch = await _context.Branches.FindAsync(branchId);
            if (branch == null)
                return false;

            // Check if assignment already exists
            var existingAssignment = await _context.UserBranches
                .FirstOrDefaultAsync(ub => ub.UserId == userId && ub.BranchId == branchId);

            if (existingAssignment != null)
            {
                // Reactivate existing assignment
                existingAssignment.IsActive = true;
            }
            else
            {
                // Create new assignment
                var userBranch = new UserBranch
                {
                    UserId = userId,
                    BranchId = branchId,
                    IsActive = true,
                    AssignedAt = DateTime.UtcNow,
                    User = user
                };

                _context.UserBranches.Add(userBranch);
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RemoveUserFromBranchAsync(string userId, int branchId)
        {
            var assignment = await _context.UserBranches
                .FirstOrDefaultAsync(ub => ub.UserId == userId && ub.BranchId == branchId && ub.IsActive);

            if (assignment == null)
                return false;

            assignment.IsActive = false;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<UserAccount>> GetUsersNotInBranchAsync(int branchId)
        {
            var branchUserIds = await _context.UserBranches
                .Where(ub => ub.BranchId == branchId && ub.IsActive)
                .Select(ub => ub.UserId)
                .ToListAsync();

            return await _context.Users
                .Where(u => u.IsActive && !branchUserIds.Contains(u.UserId))
                .ToListAsync();
        }

        public async Task<BranchStatistics> GetBranchStatisticsAsync(int branchId)
        {
            var branch = await _context.Branches.FindAsync(branchId);
            if (branch == null)
                throw new KeyNotFoundException($"Branch with ID {branchId} not found");

            var userCount = await _context.UserBranches
                .CountAsync(ub => ub.BranchId == branchId && ub.IsActive);

            var inventoryCount = await _context.InventoryItems
                .CountAsync(ii => ii.BranchId == branchId && ii.IsActive);

            var currentMonthSales = await _context.Sales
                .Where(s => s.BranchId == branchId &&
                           s.CreatedAt.Month == DateTime.UtcNow.Month &&
                           s.CreatedAt.Year == DateTime.UtcNow.Year &&
                           s.Status == "completed")
                .SumAsync(s => s.Total);

            var currentMonthPrescriptions = await _context.Prescriptions
                .CountAsync(p => p.BranchId == branchId &&
                                p.CreatedAt.Month == DateTime.UtcNow.Month &&
                                p.CreatedAt.Year == DateTime.UtcNow.Year);

            return new BranchStatistics
            {
                BranchId = branchId,
                BranchName = branch.Name,
                UserCount = userCount,
                InventoryCount = inventoryCount,
                CurrentMonthRevenue = currentMonthSales,
                CurrentMonthPrescriptions = currentMonthPrescriptions,
                MonthlyRevenue = branch.MonthlyRevenue,
                StaffCount = branch.StaffCount,
                Status = branch.Status
            };
        }
    }

    public class BranchStatistics
    {
        public int BranchId { get; set; }
        public string BranchName { get; set; } = string.Empty;
        public int UserCount { get; set; }
        public int InventoryCount { get; set; }
        public decimal CurrentMonthRevenue { get; set; }
        public int CurrentMonthPrescriptions { get; set; }
        public decimal MonthlyRevenue { get; set; }
        public int StaffCount { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}

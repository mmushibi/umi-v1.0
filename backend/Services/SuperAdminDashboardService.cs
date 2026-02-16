using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UmiHealthPOS.Models.Dashboard;
using UmiHealthPOS.Data;
using UmiHealthPOS.Models;
using System.Linq;

namespace UmiHealthPOS.Services
{
    public interface ISuperAdminDashboardService
    {
        Task<SuperAdminDashboardStats> GetSuperAdminDashboardStatsAsync();
        Task<List<RecentActivity>> GetRecentActivityAsync(int limit = 10);
        Task<SuperAdminChartData> GetRevenueChartDataAsync(string period = "monthly");
        Task<SuperAdminChartData> GetUserGrowthChartDataAsync(string period = "monthly");
        Task<List<TenantStats>> GetTenantStatsAsync();
        Task<SystemHealth> GetSystemHealthAsync();
        Task<List<TopPerformer>> GetTopPerformersAsync(string metric = "revenue", int limit = 10);
    }

    public class SuperAdminDashboardService : ISuperAdminDashboardService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SuperAdminDashboardService> _logger;

        public SuperAdminDashboardService(ApplicationDbContext context, ILogger<SuperAdminDashboardService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<SuperAdminDashboardStats> GetSuperAdminDashboardStatsAsync()
        {
            try
            {
                var now = DateTime.UtcNow;
                var startOfMonth = new DateTime(now.Year, now.Month, 1);
                var startOfLastMonth = startOfMonth.AddMonths(-1);

                // Get tenant statistics
                var totalTenants = await _context.Tenants.CountAsync();
                var activeTenants = await _context.Tenants
                    .Where(t => t.IsActive && t.Users.Any(u => u.LastLoginAt.HasValue && u.LastLoginAt.Value >= startOfMonth))
                    .CountAsync();

                // Get user statistics
                var totalUsers = await _context.Users.CountAsync();
                var activeUsers = await _context.Users
                    .Where(u => u.IsActive && u.LastLoginAt.HasValue && u.LastLoginAt.Value >= startOfMonth)
                    .CountAsync();

                // Get revenue statistics
                var monthlyRevenue = await _context.Sales
                    .Where(s => s.CreatedAt >= startOfMonth && s.Status == "completed")
                    .SumAsync(s => s.Total);

                var totalRevenue = await _context.Sales
                    .Where(s => s.Status == "completed")
                    .SumAsync(s => s.Total);

                // Calculate revenue growth
                var lastMonthRevenue = await _context.Sales
                    .Where(s => s.CreatedAt >= startOfLastMonth && s.CreatedAt < startOfMonth && s.Status == "completed")
                    .SumAsync(s => s.Total);
                
                var revenueGrowth = lastMonthRevenue > 0 
                    ? ((monthlyRevenue - lastMonthRevenue) / lastMonthRevenue) * 100 
                    : 0;

                // Get API calls (simplified - in real implementation, this would come from API logs)
                var totalApiCalls = await _context.Sales.CountAsync(); // Using sales as proxy for activity
                var lastMonthApiCalls = await _context.Sales
                    .Where(s => s.CreatedAt >= startOfLastMonth && s.CreatedAt < startOfMonth)
                    .CountAsync();
                var apiCallsGrowth = lastMonthApiCalls > 0 
                    ? ((double)(totalApiCalls - lastMonthApiCalls) / lastMonthApiCalls) * 100 
                    : 0;

                // Get inventory statistics
                var totalInventoryItems = await _context.InventoryItems.CountAsync();
                var lowStockItems = await _context.InventoryItems
                    .Where(i => i.Quantity <= i.ReorderLevel)
                    .CountAsync();

                // Get prescription statistics
                var totalPrescriptions = await _context.Prescriptions.CountAsync();
                var pendingPrescriptions = await _context.Prescriptions
                    .Where(p => p.Status == "pending")
                    .CountAsync();

                return new SuperAdminDashboardStats
                {
                    TotalTenants = totalTenants,
                    ActiveTenants = activeTenants,
                    TotalUsers = totalUsers,
                    ActiveUsers = activeUsers,
                    MonthlyRevenue = monthlyRevenue,
                    TotalRevenue = totalRevenue,
                    TotalApiCalls = totalApiCalls,
                    ApiCallsGrowth = Math.Round(apiCallsGrowth, 2),
                    TotalInventoryItems = totalInventoryItems,
                    LowStockItems = lowStockItems,
                    TotalPrescriptions = totalPrescriptions,
                    PendingPrescriptions = pendingPrescriptions,
                    RevenueGrowth = Math.Round(revenueGrowth, 2)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Super Admin dashboard stats");
                throw;
            }
        }

        public async Task<List<RecentActivity>> GetRecentActivityAsync(int limit = 10)
        {
            try
            {
                var activities = new List<RecentActivity>();

                // Get recent sales
                var recentSales = await _context.Sales
                    .Include(s => s.Customer)
                    .OrderByDescending(s => s.CreatedAt)
                    .Take(limit / 3)
                    .Select(s => new RecentActivity
                    {
                        Id = s.Id,
                        Type = "Sale",
                        Message = $"Sale completed: {s.ReceiptNumber} - {(s.Customer != null ? s.Customer.Name : "Walk-in")} - K{s.Total:N2}",
                        Timestamp = GetRelativeTime(s.CreatedAt),
                        CreatedAt = s.CreatedAt
                    })
                    .ToListAsync();

                activities.AddRange(recentSales);

                // Get recent prescription activity
                var recentPrescriptions = await _context.Prescriptions
                    .Include(p => p.Patient)
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(limit / 3)
                    .Select(p => new RecentActivity
                    {
                        Id = p.Id,
                        Type = "Prescription",
                        Message = $"Prescription {p.Status.ToLower()}: {(p.Patient != null ? p.Patient.Name : "Unknown Patient")} - {p.Medication}",
                        Timestamp = GetRelativeTime(p.CreatedAt),
                        CreatedAt = p.CreatedAt
                    })
                    .ToListAsync();

                activities.AddRange(recentPrescriptions);

                // Get recent user activity
                var recentUsers = await _context.Users
                    .Where(u => u.LastLoginAt.HasValue)
                    .OrderByDescending(u => u.LastLoginAt)
                    .Take(limit / 3)
                    .Select(u => new RecentActivity
                    {
                        Id = int.Parse(u.Id),
                        Type = "User",
                        Message = $"User {u.Email} logged in",
                        Timestamp = GetRelativeTime(u.LastLoginAt.Value),
                        CreatedAt = u.LastLoginAt.Value
                    })
                    .ToListAsync();

                activities.AddRange(recentUsers);

                // Sort by date and take the limit
                return activities
                    .OrderByDescending(a => a.CreatedAt)
                    .Take(limit)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent activity");
                throw;
            }
        }

        public async Task<SuperAdminChartData> GetRevenueChartDataAsync(string period = "monthly")
        {
            try
            {
                var chartData = new SuperAdminChartData
                {
                    Title = "Revenue Overview",
                    Type = "line"
                };

                var now = DateTime.UtcNow;
                var startDate = period.ToLower() switch
                {
                    "daily" => now.AddDays(-30),
                    "weekly" => now.AddDays(-12 * 7), // 12 weeks
                    "monthly" => now.AddMonths(-12), // 12 months
                    "yearly" => now.AddYears(-5), // 5 years
                    _ => now.AddMonths(-12)
                };

                var salesData = await _context.Sales
                    .Where(s => s.CreatedAt >= startDate && s.Status == "completed")
                    .OrderBy(s => s.CreatedAt)
                    .ToListAsync();

                // Group in memory based on period
                var groupedData = period.ToLower() switch
                {
                    "daily" => salesData.GroupBy(s => s.CreatedAt.Date),
                    "weekly" => salesData.GroupBy(s => GetWeekStart(s.CreatedAt)),
                    "monthly" => salesData.GroupBy(s => new DateTime(s.CreatedAt.Year, s.CreatedAt.Month, 1)),
                    "yearly" => salesData.GroupBy(s => new DateTime(s.CreatedAt.Year, 1, 1)),
                    _ => salesData.GroupBy(s => new DateTime(s.CreatedAt.Year, s.CreatedAt.Month, 1))
                };

                var chartDataList = groupedData
                    .Select(g => new { Period = g.Key, Revenue = g.Sum(s => s.Total) })
                    .OrderBy(g => g.Period)
                    .ToList();

                chartData.Labels = chartDataList.Select(d => d.Period.ToString("MMM dd")).ToList();
                chartData.Data = chartDataList.Select(d => d.Revenue).ToList();

                return chartData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting revenue chart data");
                throw;
            }
        }

        public async Task<SuperAdminChartData> GetUserGrowthChartDataAsync(string period = "monthly")
        {
            try
            {
                var chartData = new SuperAdminChartData
                {
                    Title = "User Growth",
                    Type = "bar"
                };

                var now = DateTime.UtcNow;
                var startDate = period.ToLower() switch
                {
                    "daily" => now.AddDays(-30),
                    "weekly" => now.AddDays(-12 * 7),
                    "monthly" => now.AddMonths(-12),
                    "yearly" => now.AddYears(-5),
                    _ => now.AddMonths(-12)
                };

                var userData = await _context.Users
                    .Where(u => u.CreatedAt >= startDate)
                    .OrderBy(u => u.CreatedAt)
                    .ToListAsync();

                // Group in memory based on period
                var groupedUserData = period.ToLower() switch
                {
                    "daily" => userData.GroupBy(u => u.CreatedAt.Date),
                    "weekly" => userData.GroupBy(u => GetWeekStart(u.CreatedAt)),
                    "monthly" => userData.GroupBy(u => new DateTime(u.CreatedAt.Year, u.CreatedAt.Month, 1)),
                    "yearly" => userData.GroupBy(u => new DateTime(u.CreatedAt.Year, 1, 1)),
                    _ => userData.GroupBy(u => new DateTime(u.CreatedAt.Year, u.CreatedAt.Month, 1))
                };

                var chartUserDataList = groupedUserData
                    .Select(g => new { Period = g.Key, Count = g.Count() })
                    .OrderBy(g => g.Period)
                    .ToList();

                chartData.Labels = chartUserDataList.Select(d => d.Period.ToString("MMM dd")).ToList();
                chartData.Data = chartUserDataList.Select(d => (decimal)d.Count).ToList();

                return chartData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user growth chart data");
                throw;
            }
        }

        public async Task<List<TenantStats>> GetTenantStatsAsync()
        {
            try
            {
                var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

                var tenantStats = await _context.Tenants
                    .Select(t => new TenantStats
                    {
                        Id = t.Id,
                        Name = t.Name,
                        Status = t.IsActive ? "Active" : "Inactive",
                        UserCount = t.Users.Count(u => u.IsActive),
                        MonthlyRevenue = t.Users
                            .SelectMany(u => u.Sales)
                            .Where(s => s.CreatedAt >= startOfMonth && s.Status == "completed")
                            .Sum(s => s.Total),
                        CreatedAt = t.CreatedAt,
                        LastActiveAt = t.Users
                            .Where(u => u.LastLoginAt.HasValue)
                            .Max(u => u.LastLoginAt) ?? t.CreatedAt
                    })
                    .OrderByDescending(t => t.MonthlyRevenue)
                    .ToListAsync();

                return tenantStats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tenant stats");
                throw;
            }
        }

        public async Task<SystemHealth> GetSystemHealthAsync()
        {
            try
            {
                // Simplified system health - in real implementation, this would check actual system metrics
                var systemHealth = new SystemHealth
                {
                    CpuUsage = 45.2, // Placeholder - would get from system monitoring
                    MemoryUsage = 62.8, // Placeholder - would get from system monitoring
                    DiskUsage = 38.5, // Placeholder - would get from system monitoring
                    ActiveConnections = await _context.Users.CountAsync(u => u.IsActive),
                    DatabaseStatus = "Healthy",
                    LastBackup = DateTime.UtcNow.AddHours(-2), // Placeholder - would get from backup logs
                    IsHealthy = true
                };

                return systemHealth;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting system health");
                throw;
            }
        }

        public async Task<List<TopPerformer>> GetTopPerformersAsync(string metric = "revenue", int limit = 10)
        {
            try
            {
                var performers = new List<TopPerformer>();

                switch (metric.ToLower())
                {
                    case "revenue":
                        var topTenants = await _context.Tenants
                            .Select(t => new TopPerformer
                            {
                                Name = t.Name,
                                Type = "tenant",
                                Value = t.Users.SelectMany(u => u.Sales).Sum(s => s.Total),
                                Metric = "Revenue",
                                Growth = 12.5 // Placeholder - would calculate actual growth
                            })
                            .OrderByDescending(p => p.Value)
                            .Take(limit)
                            .ToListAsync();
                        performers.AddRange(topTenants);
                        break;

                    case "users":
                        var topUserCount = await _context.Tenants
                            .Select(t => new TopPerformer
                            {
                                Name = t.Name,
                                Type = "tenant",
                                Value = t.Users.Count(u => u.IsActive),
                                Metric = "Active Users",
                                Growth = 8.2 // Placeholder
                            })
                            .OrderByDescending(p => p.Value)
                            .Take(limit)
                            .ToListAsync();
                        performers.AddRange(topUserCount);
                        break;

                    case "sales":
                        var topSales = await _context.Products
                            .Select(p => new TopPerformer
                            {
                                Name = p.Name,
                                Type = "product",
                                Value = p.SaleItems.Sum(si => si.Quantity),
                                Metric = "Units Sold",
                                Growth = 15.3 // Placeholder
                            })
                            .OrderByDescending(p => p.Value)
                            .Take(limit)
                            .ToListAsync();
                        performers.AddRange(topSales);
                        break;
                }

                return performers;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top performers");
                throw;
            }
        }

        private string GetRelativeTime(DateTime dateTime)
        {
            var timeSpan = DateTime.UtcNow - dateTime;

            if (timeSpan.TotalMinutes < 1)
                return "Just now";
            if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes} minute{(timeSpan.TotalMinutes >= 2 ? "s" : "")} ago";
            if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours} hour{(timeSpan.TotalHours >= 2 ? "s" : "")} ago";
            if (timeSpan.TotalDays < 7)
                return $"{(int)timeSpan.TotalDays} day{(timeSpan.TotalDays >= 2 ? "s" : "")} ago";

            return dateTime.ToString("MMM dd, yyyy");
        }

        private DateTime GetWeekStart(DateTime date)
        {
            var diff = (7 + (date.DayOfWeek - DayOfWeek.Sunday)) % 7;
            return date.AddDays(-diff).Date;
        }
    }
}


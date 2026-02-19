using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using UmiHealthPOS.Models.Dashboard;
using UmiHealthPOS.Data;
using Microsoft.Extensions.Logging;

namespace UmiHealthPOS.Services
{
    public interface IDashboardService
    {
        Task<DashboardStats> GetDashboardStatsAsync(string tenantId);
        Task<List<RecentActivity>> GetRecentActivityAsync(string tenantId, int limit = 10);
    }

    public class DashboardService : IDashboardService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DashboardService> _logger;

        public DashboardService(ApplicationDbContext context, ILogger<DashboardService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<DashboardStats> GetDashboardStatsAsync(string tenantId)
        {
            try
            {
                var today = DateTime.UtcNow.Date;
                var tomorrow = today.AddDays(1);

                // Real database implementation with parallel queries for performance
                var statsTask = Task.Run(async () =>
                {
                    // Prescription stats
                    var prescriptionTasks = new Task<int>[]
                    {
                        _context.Prescriptions.CountAsync(p => p.TenantId == tenantId),
                        _context.Prescriptions.CountAsync(p => p.TenantId == tenantId && p.Status == "pending"),
                        _context.Prescriptions.CountAsync(p => p.TenantId == tenantId && p.Status == "filled"),
                        _context.Prescriptions.CountAsync(p => p.TenantId == tenantId && p.CreatedAt >= today)
                    };

                    var prescriptionResults = await Task.WhenAll(prescriptionTasks);
                    var totalPrescriptions = prescriptionResults[0];
                    var pendingPrescriptions = prescriptionResults[1];
                    var filledPrescriptions = prescriptionResults[2];
                    var todayPrescriptions = prescriptionResults[3];

                    // Patient stats
                    var totalPatients = await _context.Patients.CountAsync(p => p.TenantId == tenantId);
                    var newPatientsToday = await _context.Patients.CountAsync(p => p.TenantId == tenantId && p.CreatedAt >= today);

                    // Inventory stats
                    var lowStockItems = await _context.Products
                        .CountAsync(p => p.TenantId == tenantId && p.Stock <= p.ReorderLevel);

                    // Sales stats for today
                    var todaySalesTask = _context.Sales.CountAsync(s => s.TenantId == tenantId && s.CreatedAt >= today);
                    var todayRevenueTask = _context.Sales.Where(s => s.TenantId == tenantId && s.CreatedAt >= today)
                        .SumAsync(s => s.Total);
                    var pendingSalesTask = _context.Sales.CountAsync(s => s.TenantId == tenantId && s.CreatedAt >= today && s.Status == "pending");

                    await Task.WhenAll(todaySalesTask, todayRevenueTask, pendingSalesTask);
                    var todaySales = await todaySalesTask;
                    var todayRevenue = await todayRevenueTask;
                    var pendingTransactions = await pendingSalesTask;

                    return new DashboardStats
                    {
                        TotalPrescriptions = totalPrescriptions,
                        PendingPrescriptions = pendingPrescriptions,
                        FilledPrescriptions = filledPrescriptions,
                        TodayPrescriptions = todayPrescriptions,
                        TotalPatients = totalPatients,
                        NewPatientsToday = newPatientsToday,
                        LowStockItems = lowStockItems,
                        TodaySales = todaySales,
                        TodayRevenue = todayRevenue,
                        PendingTransactions = pendingTransactions,
                        RecentActivity = new List<RecentActivity>() // Will be populated separately
                    };
                });

                var stats = await statsTask;
                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving dashboard stats for tenant: {TenantId}", tenantId);
                return new DashboardStats
                {
                    TotalPrescriptions = 0,
                    PendingPrescriptions = 0,
                    FilledPrescriptions = 0,
                    TodayPrescriptions = 0,
                    TotalPatients = 0,
                    NewPatientsToday = 0,
                    LowStockItems = 0,
                    TodaySales = 0,
                    TodayRevenue = 0,
                    PendingTransactions = 0,
                    RecentActivity = new List<RecentActivity>()
                };
            }
        }

        public async Task<List<RecentActivity>> GetRecentActivityAsync(string tenantId, int limit = 10)
        {
            try
            {
                var activities = new List<RecentActivity>();

                // Get recent prescriptions
                var recentPrescriptions = await _context.Prescriptions
                    .Where(p => p.TenantId == tenantId)
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(limit / 2)
                    .Select(p => new RecentActivity
                    {
                        Id = p.Id,
                        Type = "prescription",
                        Description = $"New prescription: {p.Medication} for {p.PatientName}",
                        Timestamp = p.CreatedAt,
                        Status = p.Status,
                        Priority = p.IsUrgent ? "high" : "normal"
                    })
                    .ToListAsync();

                activities.AddRange(recentPrescriptions);

                // Get recent sales
                var recentSales = await _context.Sales
                    .Where(s => s.TenantId == tenantId)
                    .OrderByDescending(s => s.CreatedAt)
                    .Take(limit / 2)
                    .Select(s => new RecentActivity
                    {
                        Id = s.Id,
                        Type = "sale",
                        Description = $"Sale completed: {s.ReceiptNumber} - ZMK {s.Total:N2}",
                        Timestamp = s.CreatedAt,
                        Status = s.Status,
                        Priority = "normal"
                    })
                    .ToListAsync();

                activities.AddRange(recentSales);

                // Get recent patient registrations
                var recentPatients = await _context.Patients
                    .Where(p => p.TenantId == tenantId)
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(limit / 4)
                    .Select(p => new RecentActivity
                    {
                        Id = p.Id,
                        Type = "patient",
                        Description = $"New patient registered: {p.Name}",
                        Timestamp = p.CreatedAt,
                        Status = "active",
                        Priority = "normal"
                    })
                    .ToListAsync();

                activities.AddRange(recentPatients);

                // Combine and order by timestamp
                var allActivities = activities
                    .OrderByDescending(a => a.Timestamp)
                    .Take(limit)
                    .ToList();

                // Format relative time
                foreach (var activity in allActivities)
                {
                    activity.RelativeTime = GetRelativeTime(activity.Timestamp);
                }

                return allActivities;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving recent activity for tenant: {TenantId}", tenantId);
                return new List<RecentActivity>();
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
    }
}

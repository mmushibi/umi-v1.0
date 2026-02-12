using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UmiHealthPOS.Models.Dashboard;

namespace UmiHealthPOS.Services
{
    public interface IDashboardService
    {
        Task<DashboardStats> GetDashboardStatsAsync();
        Task<List<RecentActivity>> GetRecentActivityAsync(int limit = 10);
    }

    public class DashboardService : IDashboardService
    {
        public async Task<DashboardStats> GetDashboardStatsAsync()
        {
            // Return empty stats - no mock data
            // When database is implemented, this will query real data

            return new DashboardStats
            {
                TotalPrescriptions = 0,
                PendingPrescriptions = 0,
                FilledPrescriptions = 0,
                TotalPatients = 0,
                LowStockItems = 0,
                RecentActivity = new List<RecentActivity>()
            };
        }

        public async Task<List<RecentActivity>> GetRecentActivityAsync(int limit = 10)
        {
            // Return empty list - no mock data
            // When database is implemented, this will query real activities

            return new List<RecentActivity>();
        }

        private string GetRelativeTime(DateTime dateTime)
        {
            var timeSpan = DateTime.Now - dateTime;

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

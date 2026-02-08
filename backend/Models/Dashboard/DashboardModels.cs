using System;

namespace UmiHealthPOS.Models.Dashboard
{
    public class DashboardStats
    {
        public int TotalStaff { get; set; }
        public int ActivePrescriptions { get; set; }
        public int LowStockItems { get; set; }
        public string MonthlyRevenue { get; set; }
    }

    public class RecentActivity
    {
        public int Id { get; set; }
        public string Type { get; set; }
        public string Message { get; set; }
        public string Timestamp { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public enum ActivityType
    {
        Sale,
        Inventory,
        User,
        Prescription,
        Payment,
        System
    }
}

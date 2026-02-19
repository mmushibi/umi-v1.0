using System;
using System.Collections.Generic;

namespace UmiHealthPOS.Models.Dashboard
{
    public class DashboardStats
    {
        public int TotalPrescriptions { get; set; }
        public int PendingPrescriptions { get; set; }
        public int FilledPrescriptions { get; set; }
        public int TodayPrescriptions { get; set; }
        public int TotalPatients { get; set; }
        public int NewPatientsToday { get; set; }
        public int LowStockItems { get; set; }
        public int TodaySales { get; set; }
        public decimal TodayRevenue { get; set; }
        public int PendingTransactions { get; set; }
        public List<RecentActivity> RecentActivity { get; set; } = new List<RecentActivity>();
    }

    public class RecentActivity
    {
        public int Id { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }
        public DateTime Timestamp { get; set; }
        public string Status { get; set; }
        public string Priority { get; set; }
        public string RelativeTime { get; set; }
    }

    public enum ActivityType
    {
        Sale,
        Inventory,
        User,
        Prescription,
        Payment,
        System,
        Patient
    }
}

using System;
using System.Collections.Generic;

namespace UmiHealthPOS.Models.Dashboard
{
    public class SuperAdminDashboardStats
    {
        public int TotalTenants { get; set; }
        public int ActiveTenants { get; set; }
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public decimal MonthlyRevenue { get; set; }
        public decimal TotalRevenue { get; set; }
        public double RevenueGrowth { get; set; }
        public int TotalApiCalls { get; set; }
        public double ApiCallsGrowth { get; set; }
        public int TotalInventoryItems { get; set; }
        public int LowStockItems { get; set; }
        public int TotalPrescriptions { get; set; }
        public int PendingPrescriptions { get; set; }
        public List<RecentActivity> RecentActivity { get; set; } = new List<RecentActivity>();
    }

    public class SuperAdminChartData
    {
        public List<string> Labels { get; set; } = new List<string>();
        public List<decimal> Data { get; set; } = new List<decimal>();
        public string Title { get; set; } = "";
        public string Type { get; set; } = "line";
    }

    public class TenantStats
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Status { get; set; } = "";
        public int UserCount { get; set; }
        public decimal MonthlyRevenue { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastActiveAt { get; set; }
    }

    public class SystemHealth
    {
        public double CpuUsage { get; set; }
        public double MemoryUsage { get; set; }
        public double DiskUsage { get; set; }
        public int ActiveConnections { get; set; }
        public string DatabaseStatus { get; set; } = "Healthy";
        public DateTime LastBackup { get; set; }
        public bool IsHealthy { get; set; }
    }

    public class TopPerformer
    {
        public string Name { get; set; } = "";
        public string Type { get; set; } = ""; // tenant, product, user
        public decimal Value { get; set; }
        public string Metric { get; set; } = "";
        public double Growth { get; set; }
    }
}

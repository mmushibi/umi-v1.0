using Microsoft.EntityFrameworkCore;
using UmiHealthPOS.Data;
using UmiHealthPOS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace UmiHealthPOS.Services
{
    public class ReportsService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ReportsService> _logger;

        public ReportsService(ApplicationDbContext context, ILogger<ReportsService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<Branch>> GetUserBranchesAsync(string userId, string userRole)
        {
            try
            {
                var query = _context.UserBranches
                    .Where(ub => ub.UserId == userId && ub.IsActive && ub.Branch.IsActive);

                // Tenant Admin can see all branches they're assigned to
                // Other roles can only see their assigned branches
                var userBranches = await query.ToListAsync();

                return userBranches.Select(ub => ub.Branch).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user branches for user {UserId}", userId);
                throw;
            }
        }

        public async Task<ReportData> GenerateReportAsync(string reportType, string dateRange, string branchId, string userId, string userRole)
        {
            try
            {
                // Validate user permissions for report type
                if (!HasReportTypePermission(userRole, reportType))
                {
                    throw new UnauthorizedAccessException($"User role {userRole} does not have permission for report type {reportType}");
                }

                // Get user's accessible branches
                var userBranches = await GetUserBranchesAsync(userId, userRole);
                var accessibleBranchIds = userBranches.Select(b => b.Id.ToString()).ToList();

                // Validate branch access
                if (branchId != "all" && !accessibleBranchIds.Contains(branchId))
                {
                    throw new UnauthorizedAccessException($"User does not have access to branch {branchId}");
                }

                var (startDate, endDate) = ParseDateRange(dateRange);

                return reportType.ToLower() switch
                {
                    "sales" => await GenerateSalesReportAsync(startDate, endDate, branchId, accessibleBranchIds),
                    "inventory" => await GenerateInventoryReportAsync(startDate, endDate, branchId, accessibleBranchIds),
                    "prescriptions" => await GeneratePrescriptionsReportAsync(startDate, endDate, branchId, accessibleBranchIds),
                    "financial" => await GenerateFinancialReportAsync(startDate, endDate, branchId, accessibleBranchIds),
                    "patients" => await GeneratePatientsReportAsync(startDate, endDate, branchId, accessibleBranchIds),
                    "staff" => userRole == "TenantAdmin" ? await GenerateStaffReportAsync(startDate, endDate, branchId, accessibleBranchIds) : throw new UnauthorizedAccessException("Only Tenant Admin can access staff reports"),
                    _ => throw new ArgumentException($"Unknown report type: {reportType}")
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating {ReportType} report for user {UserId}", reportType, userId);
                throw;
            }
        }

        private bool HasReportTypePermission(string userRole, string reportType)
        {
            return userRole switch
            {
                "TenantAdmin" => true, // Full access
                "Pharmacist" => new[] { "inventory", "prescriptions", "patients" }.Contains(reportType.ToLower()),
                "Cashier" => new[] { "sales", "patients" }.Contains(reportType.ToLower()),
                _ => false
            };
        }

        public async Task<ReportData> GenerateSalesReportAsync(DateTime startDate, DateTime endDate, string branchId, List<string> accessibleBranchIds)
        {
            var salesQuery = _context.Sales
                .Include(s => s.SaleItems)
                .Include(s => s.Customer)
                .Where(s => s.CreatedAt >= startDate && s.CreatedAt <= endDate);

            // Apply branch filtering
            if (branchId != "all")
            {
                salesQuery = salesQuery.Where(s => s.BranchId.ToString() == branchId);
            }
            else
            {
                // Filter by user's accessible branches
                salesQuery = salesQuery.Where(s => accessibleBranchIds.Contains(s.BranchId.ToString()));
            }

            var sales = await salesQuery.ToListAsync();

            // Calculate metrics
            var totalRevenue = sales.Sum(s => s.Total);
            var totalSales = sales.Count;
            var newCustomers = sales.Where(s => s.Customer != null && 
                s.Customer.CreatedAt >= startDate && s.Customer.CreatedAt <= endDate)
                .Select(s => s.CustomerId).Distinct().Count();
            var avgOrderValue = totalSales > 0 ? totalRevenue / totalSales : 0;

            // Calculate growth (compare with previous period)
            var previousStartDate = startDate.AddDays(-(endDate - startDate).Days);
            var previousEndDate = startDate;
            var previousRevenue = await GetPreviousPeriodRevenue(previousStartDate, previousEndDate, branchId, accessibleBranchIds);
            var revenueGrowth = previousRevenue > 0 ? ((totalRevenue - previousRevenue) / previousRevenue) * 100 : 0;

            // Top products
            var topProducts = sales
                .SelectMany(s => s.SaleItems)
                .GroupBy(si => new { si.ProductId, si.Product.Name })
                .Select(g => new TopProductData
                {
                    Name = g.Key.Name,
                    QuantitySold = g.Sum(si => si.Quantity),
                    Revenue = g.Sum(si => si.TotalPrice),
                    Growth = CalculateProductGrowth(g.Key.ProductId, startDate, endDate, branchId, accessibleBranchIds)
                })
                .OrderByDescending(p => p.Revenue)
                .Take(10)
                .ToList();

            // Chart data
            var revenueChart = GenerateRevenueChartData(sales, startDate, endDate);
            var paymentMethods = GeneratePaymentMethodsChart(sales);

            return new ReportData
            {
                ReportType = "sales",
                Metrics = new SalesMetrics
                {
                    TotalRevenue = totalRevenue,
                    TotalSales = totalSales,
                    NewCustomers = newCustomers,
                    AvgOrderValue = avgOrderValue,
                    RevenueGrowth = revenueGrowth
                },
                TopProducts = topProducts,
                Charts = new ChartData
                {
                    Revenue = revenueChart,
                    PaymentMethods = paymentMethods
                }
            };
        }

        public async Task<ReportData> GenerateInventoryReportAsync(DateTime startDate, DateTime endDate, string branchId, List<string> accessibleBranchIds)
        {
            var inventoryQuery = _context.InventoryItems.Where(ii => ii.IsActive);

            // Apply branch filtering
            if (branchId != "all")
            {
                inventoryQuery = inventoryQuery.Where(ii => ii.BranchId.ToString() == branchId);
            }
            else
            {
                inventoryQuery = inventoryQuery.Where(ii => accessibleBranchIds.Contains(ii.BranchId.ToString()));
            }

            var inventoryItems = await inventoryQuery.ToListAsync();

            var totalItems = inventoryItems.Count;
            var lowStockItems = inventoryItems.Where(ii => ii.Quantity <= ii.ReorderLevel).Count();
            var totalValue = inventoryItems.Sum(ii => ii.Quantity * ii.SellingPrice);
            var categories = inventoryItems.GroupBy(ii => ii.PackingType)
                .ToDictionary(g => g.Key, g => g.Count());

            return new ReportData
            {
                ReportType = "inventory",
                Metrics = new InventoryMetrics
                {
                    TotalItems = totalItems,
                    LowStockItems = lowStockItems,
                    TotalValue = totalValue,
                    Categories = categories.Count
                },
                Charts = new ChartData
                {
                    Categories = categories
                }
            };
        }

        public async Task<ReportData> GeneratePrescriptionsReportAsync(DateTime startDate, DateTime endDate, string branchId, List<string> accessibleBranchIds)
        {
            var prescriptionsQuery = _context.Prescriptions
                .Include(p => p.PrescriptionItems)
                .Include(p => p.Patient)
                .Where(p => p.PrescriptionDate >= startDate && p.PrescriptionDate <= endDate);

            // Apply branch filtering
            if (branchId != "all")
            {
                prescriptionsQuery = prescriptionsQuery.Where(p => p.BranchId.ToString() == branchId);
            }
            else
            {
                prescriptionsQuery = prescriptionsQuery.Where(p => accessibleBranchIds.Contains(p.BranchId.ToString()));
            }

            var prescriptions = await prescriptionsQuery.ToListAsync();

            var totalPrescriptions = prescriptions.Count;
            var pendingPrescriptions = prescriptions.Count(p => p.Status == "Pending");
            var completedToday = prescriptions.Count(p => p.Status == "Completed" && p.FilledDate?.Date == DateTime.Today);
            var totalValue = prescriptions.Sum(p => p.TotalCost);

            // Medications breakdown
            var medications = prescriptions
                .SelectMany(p => p.PrescriptionItems)
                .GroupBy(pi => pi.MedicationName)
                .Select(g => new MedicationData
                {
                    Name = g.Key,
                    Count = g.Count(),
                    TotalValue = g.Sum(pi => pi.TotalPrice)
                })
                .OrderByDescending(m => m.Count)
                .Take(10)
                .ToList();

            return new ReportData
            {
                ReportType = "prescriptions",
                Metrics = new PrescriptionMetrics
                {
                    TotalPrescriptions = totalPrescriptions,
                    PendingPrescriptions = pendingPrescriptions,
                    CompletedToday = completedToday,
                    TotalValue = totalValue
                },
                Charts = new ChartData
                {
                    Medications = medications.ToDictionary(m => m.Name, m => m.Count)
                }
            };
        }

        public async Task<ReportData> GenerateFinancialReportAsync(DateTime startDate, DateTime endDate, string branchId, List<string> accessibleBranchIds)
        {
            var salesQuery = _context.Sales
                .Where(s => s.CreatedAt >= startDate && s.CreatedAt <= endDate && s.Status == "Completed");

            // Apply branch filtering
            if (branchId != "all")
            {
                salesQuery = salesQuery.Where(s => s.BranchId.ToString() == branchId);
            }
            else
            {
                salesQuery = salesQuery.Where(s => accessibleBranchIds.Contains(s.BranchId.ToString()));
            }

            var sales = await salesQuery.ToListAsync();

            var revenue = sales.Sum(s => s.Total);
            var tax = sales.Sum(s => s.Tax);
            var netRevenue = revenue - tax;
            var transactions = sales.Count;

            return new ReportData
            {
                ReportType = "financial",
                Metrics = new FinancialMetrics
                {
                    Revenue = revenue,
                    Tax = tax,
                    NetRevenue = netRevenue,
                    Transactions = transactions
                }
            };
        }

        public async Task<ReportData> GeneratePatientsReportAsync(DateTime startDate, DateTime endDate, string branchId, List<string> accessibleBranchIds)
        {
            var patientsQuery = _context.Patients
                .Include(p => p.Prescriptions)
                .Where(p => p.CreatedAt >= startDate && p.CreatedAt <= endDate);

            // Apply branch filtering
            if (branchId != "all")
            {
                patientsQuery = patientsQuery.Where(p => p.BranchId.ToString() == branchId);
            }
            else
            {
                patientsQuery = patientsQuery.Where(p => accessibleBranchIds.Contains(p.BranchId.ToString()));
            }

            var patients = await patientsQuery.ToListAsync();

            var activePatients = patients.Count(p => p.Prescriptions.Any());
            var newPatients = patients.Count;
            var totalVisits = patients.Sum(p => p.Prescriptions.Count);
            var avgVisitsPerPatient = activePatients > 0 ? (double)totalVisits / activePatients : 0;

            // Age groups
            var ageGroups = patients
                .GroupBy(p => CalculateAgeGroup(p.DateOfBirth))
                .ToDictionary(g => g.Key, g => g.Count());

            return new ReportData
            {
                ReportType = "patients",
                Metrics = new PatientMetrics
                {
                    ActivePatients = activePatients,
                    NewPatients = newPatients,
                    TotalVisits = totalVisits,
                    AvgVisitsPerPatient = avgVisitsPerPatient
                },
                Charts = new ChartData
                {
                    AgeGroups = ageGroups
                }
            };
        }

        public async Task<ReportData> GenerateStaffReportAsync(DateTime startDate, DateTime endDate, string branchId, List<string> accessibleBranchIds)
        {
            // Staff performance metrics would be implemented here
            // For now, return placeholder data
            return new ReportData
            {
                ReportType = "staff",
                Metrics = new StaffMetrics
                {
                    TotalStaff = 0,
                    ActiveStaff = 0,
                    AvgPerformance = 0
                }
            };
        }

        private (DateTime startDate, DateTime endDate) ParseDateRange(string dateRange)
        {
            var today = DateTime.Today;
            return dateRange.ToLower() switch
            {
                "today" => (today, today.AddDays(1).AddTicks(-1)),
                "week" => (today.AddDays(-(int)today.DayOfWeek), today.AddDays(1).AddTicks(-1)),
                "month" => (new DateTime(today.Year, today.Month, 1), today.AddDays(1).AddTicks(-1)),
                "quarter" => (new DateTime(today.Year, ((today.Month - 1) / 3) * 3 + 1, 1), today.AddDays(1).AddTicks(-1)),
                "year" => (new DateTime(today.Year, 1, 1), today.AddDays(1).AddTicks(-1)),
                _ => (today.AddDays(-30), today)
            };
        }

        private async Task<decimal> GetPreviousPeriodRevenue(DateTime startDate, DateTime endDate, string branchId, List<string> accessibleBranchIds)
        {
            var salesQuery = _context.Sales
                .Where(s => s.CreatedAt >= startDate && s.CreatedAt <= endDate && s.Status == "Completed");

            if (branchId != "all")
            {
                salesQuery = salesQuery.Where(s => s.BranchId.ToString() == branchId);
            }
            else
            {
                salesQuery = salesQuery.Where(s => accessibleBranchIds.Contains(s.BranchId.ToString()));
            }

            return await salesQuery.SumAsync(s => s.Total);
        }

        private decimal CalculateProductGrowth(int productId, DateTime startDate, DateTime endDate, string branchId, List<string> accessibleBranchIds)
        {
            // Simplified growth calculation - would need more complex logic for real implementation
            return 0;
        }

        private List<RevenueChartData> GenerateRevenueChartData(List<Sale> sales, DateTime startDate, DateTime endDate)
        {
            // Group by day for simplicity
            return sales
                .GroupBy(s => s.CreatedAt.Date)
                .Select(g => new RevenueChartData
                {
                    Date = g.Key.ToString("yyyy-MM-dd"),
                    Value = g.Sum(s => s.Total)
                })
                .OrderBy(d => d.Date)
                .ToList();
        }

        private Dictionary<string, int> GeneratePaymentMethodsChart(List<Sale> sales)
        {
            return sales
                .GroupBy(s => s.PaymentMethod)
                .ToDictionary(g => g.Key, g => g.Count());
        }

        private string CalculateAgeGroup(DateTime? dateOfBirth)
        {
            if (!dateOfBirth.HasValue) return "Unknown";
            
            var age = DateTime.Today.Year - dateOfBirth.Value.Year;
            if (dateOfBirth.Value > DateTime.Today.AddYears(-age)) age--;

            return age switch
            {
                < 18 => "Under 18",
                < 30 => "18-29",
                < 45 => "30-44",
                < 60 => "45-59",
                _ => "60+"
            };
        }
    }

    // Data transfer objects
    public class ReportData
    {
        public string ReportType { get; set; }
        public object Metrics { get; set; }
        public List<TopProductData> TopProducts { get; set; }
        public ChartData Charts { get; set; }
    }

    public class ChartData
    {
        public List<RevenueChartData> Revenue { get; set; }
        public Dictionary<string, int> PaymentMethods { get; set; }
        public Dictionary<string, int> Categories { get; set; }
        public Dictionary<string, int> Medications { get; set; }
        public Dictionary<string, int> AgeGroups { get; set; }
    }

    public class RevenueChartData
    {
        public string Date { get; set; }
        public decimal Value { get; set; }
    }

    public class TopProductData
    {
        public string Name { get; set; }
        public int QuantitySold { get; set; }
        public decimal Revenue { get; set; }
        public decimal Growth { get; set; }
    }

    public class MedicationData
    {
        public string Name { get; set; }
        public int Count { get; set; }
        public decimal TotalValue { get; set; }
    }

    // Metrics classes
    public class SalesMetrics
    {
        public decimal TotalRevenue { get; set; }
        public int TotalSales { get; set; }
        public int NewCustomers { get; set; }
        public decimal AvgOrderValue { get; set; }
        public decimal RevenueGrowth { get; set; }
    }

    public class InventoryMetrics
    {
        public int TotalItems { get; set; }
        public int LowStockItems { get; set; }
        public decimal TotalValue { get; set; }
        public int Categories { get; set; }
    }

    public class PrescriptionMetrics
    {
        public int TotalPrescriptions { get; set; }
        public int PendingPrescriptions { get; set; }
        public int CompletedToday { get; set; }
        public decimal TotalValue { get; set; }
    }

    public class FinancialMetrics
    {
        public decimal Revenue { get; set; }
        public decimal Tax { get; set; }
        public decimal NetRevenue { get; set; }
        public int Transactions { get; set; }
    }

    public class PatientMetrics
    {
        public int ActivePatients { get; set; }
        public int NewPatients { get; set; }
        public int TotalVisits { get; set; }
        public double AvgVisitsPerPatient { get; set; }
    }

    public class StaffMetrics
    {
        public int TotalStaff { get; set; }
        public int ActiveStaff { get; set; }
        public double AvgPerformance { get; set; }
    }
}

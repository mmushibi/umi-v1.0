using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UmiHealthPOS.Data;
using UmiHealthPOS.Models;

namespace UmiHealthPOS.Services
{
    public interface ISubscriptionHistoryService
    {
        Task<List<SubscriptionHistoryDto>> GetSubscriptionHistoryAsync(
            string searchQuery = "",
            string action = "",
            string plan = "",
            string dateRange = "");
        Task<SubscriptionHistoryDto?> GetSubscriptionHistoryByIdAsync(int id);
        Task<SubscriptionHistory> CreateSubscriptionHistoryAsync(CreateSubscriptionHistoryRequest request);
        Task<SubscriptionHistory?> UpdateSubscriptionHistoryAsync(int id, UpdateSubscriptionHistoryRequest request);
        Task<bool> DeleteSubscriptionHistoryAsync(int id);
        Task<byte[]> ExportSubscriptionHistoryToCsvAsync(
            string searchQuery = "",
            string action = "",
            string plan = "",
            string dateRange = "");
        Task<byte[]> ExportTenantDetailsToCsvAsync(int subscriptionId);
        Task<List<SubscriptionHistoryDto>> GetTenantHistoryAsync(string tenantName);
    }

    public class SubscriptionHistoryService : ISubscriptionHistoryService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SubscriptionHistoryService> _logger;

        public SubscriptionHistoryService(
            ApplicationDbContext context,
            ILogger<SubscriptionHistoryService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<SubscriptionHistoryDto>> GetSubscriptionHistoryAsync(
            string searchQuery = "",
            string action = "",
            string plan = "",
            string dateRange = "")
        {
            try
            {
                var query = _context.SubscriptionHistories
                    .Include(sh => sh.Subscription)
                        .ThenInclude(s => s.Pharmacy)
                    .Include(sh => sh.Subscription)
                        .ThenInclude(s => s.Plan)
                    .Include(sh => sh.User)
                    .AsQueryable();

                // Apply search filter
                if (!string.IsNullOrEmpty(searchQuery))
                {
                    query = query.Where(sh =>
                        sh.Subscription.Pharmacy.Name.Contains(searchQuery) ||
                        sh.Subscription.Pharmacy.Email.Contains(searchQuery));
                }

                // Apply action filter
                if (!string.IsNullOrEmpty(action))
                {
                    query = query.Where(sh => sh.Action == action);
                }

                // Apply plan filter
                if (!string.IsNullOrEmpty(plan))
                {
                    query = query.Where(sh => sh.NewPlan == plan);
                }

                // Apply date range filter
                if (!string.IsNullOrEmpty(dateRange))
                {
                    var today = DateTime.UtcNow.Date;
                    var startDate = dateRange.ToLower() switch
                    {
                        "7" => today.AddDays(-7),
                        "30" => today.AddDays(-30),
                        "90" => today.AddDays(-90),
                        "365" => today.AddDays(-365),
                        _ => (DateTime?)null
                    };

                    if (startDate.HasValue)
                    {
                        query = query.Where(sh => sh.CreatedAt >= startDate.Value);
                    }
                }

                var history = await query
                    .OrderByDescending(sh => sh.CreatedAt)
                    .Select(sh => new SubscriptionHistoryDto
                    {
                        Id = sh.Id,
                        Date = sh.CreatedAt.ToString("yyyy-MM-dd"),
                        Tenant = sh.Subscription.Pharmacy.Name,
                        Email = sh.Subscription.Pharmacy.Email,
                        Phone = sh.Subscription.Pharmacy.Phone,
                        BusinessType = GetBusinessType(sh.Subscription.Pharmacy.Name),
                        RegistrationNumber = $"ZAM/{GetBusinessType(sh.Subscription.Pharmacy.Name).ToUpper()}/{sh.CreatedAt:yyyy}/{sh.Id:D3}",
                        Action = sh.Action,
                        Plan = sh.NewPlan,
                        Amount = sh.Amount.ToString("F2"),
                        User = $"{sh.User.FirstName} {sh.User.LastName}",
                        Notes = sh.Notes,
                        PaymentMethod = GetPaymentMethod(sh.Subscription.Pharmacy.Name),
                        NextBilling = sh.Subscription.EndDate.ToString("yyyy-MM-dd"),
                        Address = sh.Subscription.Pharmacy.Address,
                        City = sh.Subscription.Pharmacy.City,
                        PostalCode = sh.Subscription.Pharmacy.PostalCode,
                        TotalUsers = GetTotalUsers(sh.Subscription.Pharmacy.Id).ToString(),
                        TotalTransactions = GetTotalTransactions(sh.Subscription.Pharmacy.Id).ToString(),
                        TotalProducts = GetTotalProducts(sh.Subscription.Pharmacy.Id).ToString(),
                        StorageUsed = GetStorageUsed(sh.Subscription.Pharmacy.Id)
                    })
                    .ToListAsync();

                return history;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving subscription history");
                throw;
            }
        }

        public async Task<SubscriptionHistoryDto?> GetSubscriptionHistoryByIdAsync(int id)
        {
            try
            {
                var history = await _context.SubscriptionHistories
                    .Include(sh => sh.Subscription)
                        .ThenInclude(s => s.Pharmacy)
                    .Include(sh => sh.Subscription)
                        .ThenInclude(s => s.Plan)
                    .Include(sh => sh.User)
                    .Where(sh => sh.Id == id)
                    .Select(sh => new SubscriptionHistoryDto
                    {
                        Id = sh.Id,
                        Date = sh.CreatedAt.ToString("yyyy-MM-dd"),
                        Tenant = sh.Subscription.Pharmacy.Name,
                        Email = sh.Subscription.Pharmacy.Email,
                        Phone = sh.Subscription.Pharmacy.Phone,
                        BusinessType = GetBusinessType(sh.Subscription.Pharmacy.Name),
                        RegistrationNumber = $"ZAM/{GetBusinessType(sh.Subscription.Pharmacy.Name).ToUpper()}/{sh.CreatedAt:yyyy}/{sh.Id:D3}",
                        Action = sh.Action,
                        Plan = sh.NewPlan,
                        Amount = sh.Amount.ToString("F2"),
                        User = $"{sh.User.FirstName} {sh.User.LastName}",
                        Notes = sh.Notes,
                        PaymentMethod = GetPaymentMethod(sh.Subscription.Pharmacy.Name),
                        NextBilling = sh.Subscription.EndDate.ToString("yyyy-MM-dd"),
                        Address = sh.Subscription.Pharmacy.Address,
                        City = sh.Subscription.Pharmacy.City,
                        PostalCode = sh.Subscription.Pharmacy.PostalCode,
                        TotalUsers = GetTotalUsers(sh.Subscription.Pharmacy.Id).ToString(),
                        TotalTransactions = GetTotalTransactions(sh.Subscription.Pharmacy.Id).ToString(),
                        TotalProducts = GetTotalProducts(sh.Subscription.Pharmacy.Id).ToString(),
                        StorageUsed = GetStorageUsed(sh.Subscription.Pharmacy.Id)
                    })
                    .FirstOrDefaultAsync();

                return history;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving subscription history with ID: {Id}", id);
                throw;
            }
        }

        public async Task<SubscriptionHistory> CreateSubscriptionHistoryAsync(CreateSubscriptionHistoryRequest request)
        {
            try
            {
                var history = new SubscriptionHistory
                {
                    SubscriptionId = request.SubscriptionId,
                    Action = request.Action,
                    PreviousPlan = request.PreviousPlan,
                    NewPlan = request.NewPlan,
                    Amount = request.Amount,
                    Notes = request.Notes,
                    UserId = request.UserId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.SubscriptionHistories.Add(history);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created subscription history record for subscription {SubscriptionId}", request.SubscriptionId);
                return history;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating subscription history");
                throw;
            }
        }

        public async Task<SubscriptionHistory?> UpdateSubscriptionHistoryAsync(int id, UpdateSubscriptionHistoryRequest request)
        {
            try
            {
                var history = await _context.SubscriptionHistories.FindAsync(id);
                if (history == null)
                {
                    return null;
                }

                if (!string.IsNullOrEmpty(request.Action))
                    history.Action = request.Action;

                if (!string.IsNullOrEmpty(request.PreviousPlan))
                    history.PreviousPlan = request.PreviousPlan;

                if (!string.IsNullOrEmpty(request.NewPlan))
                    history.NewPlan = request.NewPlan;

                if (request.Amount.HasValue)
                    history.Amount = request.Amount.Value;

                if (!string.IsNullOrEmpty(request.Notes))
                    history.Notes = request.Notes;

                history.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated subscription history record with ID: {Id}", id);
                return history;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating subscription history with ID: {Id}", id);
                throw;
            }
        }

        public async Task<bool> DeleteSubscriptionHistoryAsync(int id)
        {
            try
            {
                var history = await _context.SubscriptionHistories.FindAsync(id);
                if (history == null)
                {
                    return false;
                }

                _context.SubscriptionHistories.Remove(history);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted subscription history record with ID: {Id}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting subscription history with ID: {Id}", id);
                throw;
            }
        }

        public async Task<byte[]> ExportSubscriptionHistoryToCsvAsync(
            string searchQuery = "",
            string action = "",
            string plan = "",
            string dateRange = "")
        {
            try
            {
                var history = await GetSubscriptionHistoryAsync(searchQuery, action, plan, dateRange);

                var csv = new StringBuilder();
                csv.AppendLine("Subscription History Report");
                csv.AppendLine($"Generated:, {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                csv.AppendLine("Currency:, Zambian Kwacha (ZMW)");
                csv.AppendLine();
                csv.AppendLine("Date,Tenant Name,Email,Phone,Business Type,Registration Number,Action,Plan,Amount (ZMW),User,Notes,Payment Method,Next Billing Date");

                foreach (var record in history)
                {
                    csv.AppendLine($"\"{record.Date}\",\"{record.Tenant}\",\"{record.Email}\",\"{record.Phone}\",\"{record.BusinessType}\",\"{record.RegistrationNumber}\",\"{record.Action}\",\"{record.Plan}\",\"{record.Amount}\",\"{record.User}\",\"{record.Notes}\",\"{record.PaymentMethod}\",\"{record.NextBilling}\"");
                }

                return Encoding.UTF8.GetBytes(csv.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting subscription history to CSV");
                throw;
            }
        }

        public async Task<byte[]> ExportTenantDetailsToCsvAsync(int subscriptionId)
        {
            try
            {
                var subscription = await _context.Subscriptions
                    .Include(s => s.Pharmacy)
                    .Include(s => s.Plan)
                    .Include(s => s.SubscriptionHistory)
                        .ThenInclude(sh => sh.User)
                    .FirstOrDefaultAsync(s => s.Id == subscriptionId);

                if (subscription == null)
                {
                    throw new ArgumentException("Subscription not found");
                }

                var csv = new StringBuilder();
                csv.AppendLine($"Detailed Report - {subscription.Pharmacy.Name}");
                csv.AppendLine($"Generated:, {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                csv.AppendLine("Currency:, Zambian Kwacha (ZMW)");
                csv.AppendLine();
                csv.AppendLine("BUSINESS INFORMATION");
                csv.AppendLine($"Business Name:, {subscription.Pharmacy.Name}");
                csv.AppendLine($"Email:, {subscription.Pharmacy.Email}");
                csv.AppendLine($"Phone:, {subscription.Pharmacy.Phone}");
                csv.AppendLine($"Business Type:, {GetBusinessType(subscription.Pharmacy.Name)}");
                csv.AppendLine($"Registration Number:, ZAM/{GetBusinessType(subscription.Pharmacy.Name).ToUpper()}/{DateTime.Now:yyyy}/{subscription.Id:D3}");
                csv.AppendLine($"Physical Address:, {subscription.Pharmacy.Address}");
                csv.AppendLine($"City:, {subscription.Pharmacy.City}");
                csv.AppendLine($"Postal Code:, {subscription.Pharmacy.PostalCode}");
                csv.AppendLine();
                csv.AppendLine("SUBSCRIPTION DETAILS");
                csv.AppendLine($"Current Plan:, {subscription.Plan.Name}");
                csv.AppendLine($"Monthly Amount (ZMW):, {subscription.Amount:F2}");
                csv.AppendLine($"Next Billing Date:, {subscription.EndDate:yyyy-MM-dd}");
                csv.AppendLine($"Payment Method:, {await GetPaymentMethodAsync(subscription.Pharmacy.Id)}");
                csv.AppendLine($"Status:, {subscription.Status}");
                csv.AppendLine();
                csv.AppendLine("USAGE STATISTICS");
                csv.AppendLine($"Total Users:, {await GetTotalUsersAsync(subscription.Pharmacy.Id)}");
                csv.AppendLine($"Total Transactions:, {await GetTotalTransactionsAsync(subscription.Pharmacy.Id)}");
                csv.AppendLine($"Total Products:, {await GetTotalProductsAsync(subscription.Pharmacy.Id)}");
                csv.AppendLine($"Storage Used:, {await GetStorageUsedAsync(subscription.Pharmacy.Id)}");
                csv.AppendLine();
                csv.AppendLine("ACTIVITY HISTORY");
                csv.AppendLine("Date,Action,Plan,Amount (ZMW),User,Notes");

                foreach (var history in subscription.SubscriptionHistories.OrderByDescending(sh => sh.CreatedAt))
                {
                    var userName = $"{history.User.FirstName} {history.User.LastName}";
                    csv.AppendLine($"\"{history.CreatedAt:yyyy-MM-dd}\",\"{history.Action}\",\"{history.NewPlan}\",\"{history.Amount:F2}\",\"{userName}\",\"{history.Notes}\"");
                }

                return Encoding.UTF8.GetBytes(csv.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting tenant details to CSV");
                throw;
            }
        }

        public async Task<List<SubscriptionHistoryDto>> GetTenantHistoryAsync(string tenantName)
        {
            try
            {
                var history = await _context.SubscriptionHistories
                    .Include(sh => sh.Subscription)
                        .ThenInclude(s => s.Pharmacy)
                    .Include(sh => sh.Subscription)
                        .ThenInclude(s => s.Plan)
                    .Include(sh => sh.User)
                    .Where(sh => sh.Subscription.Pharmacy.Name == tenantName)
                    .OrderByDescending(sh => sh.CreatedAt)
                    .Select(sh => new SubscriptionHistoryDto
                    {
                        Id = sh.Id,
                        Date = sh.CreatedAt.ToString("yyyy-MM-dd"),
                        Tenant = sh.Subscription.Pharmacy.Name,
                        Email = sh.Subscription.Pharmacy.Email,
                        Phone = sh.Subscription.Pharmacy.Phone,
                        BusinessType = GetBusinessType(sh.Subscription.Pharmacy.Name),
                        RegistrationNumber = $"ZAM/{GetBusinessType(sh.Subscription.Pharmacy.Name).ToUpper()}/{sh.CreatedAt:yyyy}/{sh.Id:D3}",
                        Action = sh.Action,
                        Plan = sh.NewPlan,
                        Amount = sh.Amount.ToString("F2"),
                        User = $"{sh.User.FirstName} {sh.User.LastName}",
                        Notes = sh.Notes,
                        PaymentMethod = GetPaymentMethod(sh.Subscription.Pharmacy.Name),
                        NextBilling = sh.Subscription.EndDate.ToString("yyyy-MM-dd"),
                        Address = sh.Subscription.Pharmacy.Address,
                        City = sh.Subscription.Pharmacy.City,
                        PostalCode = sh.Subscription.Pharmacy.PostalCode,
                        TotalUsers = GetTotalUsers(sh.Subscription.Pharmacy.Id).ToString(),
                        TotalTransactions = GetTotalTransactions(sh.Subscription.Pharmacy.Id).ToString(),
                        TotalProducts = GetTotalProducts(sh.Subscription.Pharmacy.Id).ToString(),
                        StorageUsed = GetStorageUsed(sh.Subscription.Pharmacy.Id)
                    })
                    .ToListAsync();

                return history;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving tenant history for {TenantName}", tenantName);
                throw;
            }
        }

        private string GetBusinessType(string pharmacyName)
        {
            if (pharmacyName.ToLower().Contains("hospital"))
                return "Hospital";
            if (pharmacyName.ToLower().Contains("clinic"))
                return "Clinic";
            return "Pharmacy";
        }

        private async Task<string> GetPaymentMethodAsync(int pharmacyId)
        {
            try
            {
                // Get payment method from pharmacy's profile or payment history
                var pharmacy = await _context.Pharmacies.FirstOrDefaultAsync(p => p.Id == pharmacyId);
                if (pharmacy?.PreferredPaymentMethod != null)
                {
                    return pharmacy.PreferredPaymentMethod;
                }

                // Check most recent payment method
                var recentPayment = await _context.Payments
                    .Where(p => p.Subscription.TenantId == pharmacyId.ToString())
                    .OrderByDescending(p => p.CreatedAt)
                    .FirstOrDefaultAsync();

                return recentPayment?.PaymentMethod ?? "Mobile Money";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment method for pharmacy {PharmacyId}", pharmacyId);
                return "Mobile Money";
            }
        }

        private async Task<int> GetTotalUsersAsync(int pharmacyId)
        {
            try
            {
                // Count actual active users for the pharmacy
                return await _context.Users
                    .Where(u => u.TenantId == pharmacyId.ToString() && u.IsActive)
                    .CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error counting users for pharmacy {PharmacyId}", pharmacyId);
                return 1;
            }
        }

        private async Task<int> GetTotalTransactionsAsync(int pharmacyId)
        {
            try
            {
                // Count actual transactions for the current month
                var currentMonth = DateTime.UtcNow.Date.AddDays(-DateTime.UtcNow.Day + 1);
                return await _context.Sales
                    .Where(s => s.TenantId == pharmacyId.ToString() && s.CreatedAt >= currentMonth)
                    .CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error counting transactions for pharmacy {PharmacyId}", pharmacyId);
                return 0;
            }
        }

        private async Task<int> GetTotalProductsAsync(int pharmacyId)
        {
            try
            {
                // Count actual products in inventory
                return await _context.InventoryItems
                    .Where(i => i.TenantId == pharmacyId.ToString())
                    .CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error counting products for pharmacy {PharmacyId}", pharmacyId);
                return 0;
            }
        }

        private async Task<string> GetStorageUsedAsync(int pharmacyId)
        {
            try
            {
                // Calculate actual storage usage from database size and attachments
                var dbSize = await GetDatabaseSizeAsync();
                var attachmentsSize = await GetAttachmentsSizeAsync(pharmacyId);
                var totalSize = dbSize + attachmentsSize;
                
                return $"{totalSize / 1024.0:F1}GB";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating storage usage for pharmacy {PharmacyId}", pharmacyId);
                return "0.0GB";
            }
        }

        private async Task<double> GetDatabaseSizeAsync()
        {
            // In production, this would query actual database size
            // For now, simulate based on record count
            var recordCount = await _context.InventoryItems.CountAsync();
            return recordCount * 0.001; // Rough estimate: 1KB per record
        }

        private async Task<double> GetAttachmentsSizeAsync(int pharmacyId)
        {
            // In production, this would calculate actual attachment sizes
            // For now, simulate based on prescription count
            var prescriptionCount = await _context.Prescriptions
                .Where(p => p.TenantId == pharmacyId.ToString())
                .CountAsync();
            return prescriptionCount * 0.05; // Rough estimate: 50KB per prescription
        }
    }
}

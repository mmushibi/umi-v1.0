using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UmiHealthPOS.Data;
using UmiHealthPOS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace UmiHealthPOS.Services
{
    public interface IBillingService
    {
        Task<IEnumerable<Invoice>> GetAllInvoicesAsync();
        Task<Invoice?> GetInvoiceByIdAsync(int id);
        Task<IEnumerable<Invoice>> GetInvoicesByTenantIdAsync(int tenantId);
        Task<Invoice> CreateInvoiceAsync(Invoice invoice);
        Task<Invoice> UpdateInvoiceAsync(Invoice invoice);
        Task<bool> DeleteInvoiceAsync(int id);
        Task<CreditNote> CreateCreditNoteAsync(CreditNote creditNote);
        Task<Payment> ProcessPaymentAsync(Payment payment);
        Task<IEnumerable<Tenant>> GetAllTenantsAsync();
        Task<Tenant?> GetTenantByIdAsync(int id);
        Task<BillingSummary> GetBillingSummaryAsync();
        Task<BillingSummary> GetTenantBillingSummaryAsync(int tenantId);
    }

    public class BillingService : IBillingService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<BillingService> _logger;

        public BillingService(ApplicationDbContext context, ILogger<BillingService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<Invoice>> GetAllInvoicesAsync()
        {
            try
            {
                return await _context.Invoices
                    .Include(i => i.Tenant)
                    .OrderByDescending(i => i.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all invoices");
                throw;
            }
        }

        public async Task<Invoice?> GetInvoiceByIdAsync(int id)
        {
            try
            {
                return await _context.Invoices
                    .Include(i => i.Tenant)
                    .Include(i => i.CreditNotes)
                    .Include(i => i.Payments)
                    .FirstOrDefaultAsync(i => i.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving invoice with ID: {id}");
                throw;
            }
        }

        public async Task<IEnumerable<Invoice>> GetInvoicesByTenantIdAsync(int tenantId)
        {
            try
            {
                return await _context.Invoices
                    .Include(i => i.Tenant)
                    .Include(i => i.CreditNotes)
                    .Include(i => i.Payments)
                    .Where(i => i.TenantId == tenantId)
                    .OrderByDescending(i => i.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving invoices for tenant ID: {tenantId}");
                throw;
            }
        }

        public async Task<Invoice> CreateInvoiceAsync(Invoice invoice)
        {
            try
            {
                // Generate invoice number
                var lastInvoice = await _context.Invoices
                    .OrderByDescending(i => i.Id)
                    .FirstOrDefaultAsync();
                
                var nextNumber = (lastInvoice?.Id ?? 0) + 1;
                invoice.Number = $"INV-{DateTime.Now:yyyy}-{nextNumber:D3}";
                invoice.Status = "Pending";
                invoice.CreatedAt = DateTime.UtcNow;
                invoice.UpdatedAt = DateTime.UtcNow;

                _context.Invoices.Add(invoice);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Created invoice {invoice.Number} for tenant {invoice.TenantId}");
                return invoice;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating invoice");
                throw;
            }
        }

        public async Task<Invoice> UpdateInvoiceAsync(Invoice invoice)
        {
            try
            {
                invoice.UpdatedAt = DateTime.UtcNow;
                _context.Invoices.Update(invoice);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Updated invoice {invoice.Number}");
                return invoice;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating invoice {invoice.Number}");
                throw;
            }
        }

        public async Task<bool> DeleteInvoiceAsync(int id)
        {
            try
            {
                var invoice = await _context.Invoices.FindAsync(id);
                if (invoice == null)
                    return false;

                _context.Invoices.Remove(invoice);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Deleted invoice {invoice.Number}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting invoice with ID: {id}");
                throw;
            }
        }

        public async Task<CreditNote> CreateCreditNoteAsync(CreditNote creditNote)
        {
            try
            {
                // Generate credit note number
                var lastCreditNote = await _context.CreditNotes
                    .OrderByDescending(cn => cn.Id)
                    .FirstOrDefaultAsync();
                
                var nextNumber = (lastCreditNote?.Id ?? 0) + 1;
                creditNote.Number = $"CN-{DateTime.Now:yyyy}-{nextNumber:D3}";
                creditNote.CreatedAt = DateTime.UtcNow;
                creditNote.UpdatedAt = DateTime.UtcNow;

                _context.CreditNotes.Add(creditNote);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Created credit note {creditNote.Number} for invoice {creditNote.InvoiceId}");
                return creditNote;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating credit note");
                throw;
            }
        }

        public async Task<Payment> ProcessPaymentAsync(Payment payment)
        {
            try
            {
                payment.CreatedAt = DateTime.UtcNow;
                payment.UpdatedAt = DateTime.UtcNow;
                payment.PaymentDate = DateTime.UtcNow;

                _context.Payments.Add(payment);
                
                // Update invoice status if payment is successful
                if (payment.Status == "Completed")
                {
                    var invoice = await _context.Invoices.FindAsync(payment.InvoiceId);
                    if (invoice != null)
                    {
                        invoice.Status = "Paid";
                        invoice.PaymentDate = payment.PaymentDate;
                        invoice.PaymentMethod = payment.PaymentMethod;
                        invoice.UpdatedAt = DateTime.UtcNow;
                        _context.Invoices.Update(invoice);
                    }
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Processed payment for invoice {payment.InvoiceId}");
                return payment;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment");
                throw;
            }
        }

        public async Task<IEnumerable<Tenant>> GetAllTenantsAsync()
        {
            try
            {
                return await _context.Tenants
                    .Where(t => t.IsActive)
                    .OrderBy(t => t.Company)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all tenants");
                throw;
            }
        }

        public async Task<Tenant?> GetTenantByIdAsync(int id)
        {
            try
            {
                return await _context.Tenants
                    .Include(t => t.Invoices)
                    .FirstOrDefaultAsync(t => t.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving tenant with ID: {id}");
                throw;
            }
        }

        public async Task<BillingSummary> GetBillingSummaryAsync()
        {
            try
            {
                var invoices = await _context.Invoices.ToListAsync();
                
                var totalInvoices = invoices.Count;
                var paidInvoices = invoices.Where(i => i.Status == "Paid").ToList();
                var pendingInvoices = invoices.Where(i => i.Status == "Pending").ToList();
                var overdueInvoices = invoices.Where(i => i.Status == "Overdue").ToList();

                return new BillingSummary
                {
                    TotalInvoices = totalInvoices,
                    TotalPaid = paidInvoices.Sum(i => i.Amount),
                    TotalPending = pendingInvoices.Sum(i => i.Amount),
                    TotalOverdue = overdueInvoices.Sum(i => i.Amount),
                    MonthlyRevenue = paidInvoices
                        .Where(i => i.PaymentDate.HasValue && i.PaymentDate.Value.Month == DateTime.Now.Month)
                        .Sum(i => i.Amount),
                    PaymentSuccessRate = totalInvoices > 0 ? (double)paidInvoices.Count / totalInvoices * 100 : 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving billing summary");
                throw;
            }
        }

        public async Task<BillingSummary> GetTenantBillingSummaryAsync(int tenantId)
        {
            try
            {
                var invoices = await _context.Invoices
                    .Where(i => i.TenantId == tenantId)
                    .ToListAsync();
                
                var totalInvoices = invoices.Count;
                var paidInvoices = invoices.Where(i => i.Status == "Paid").ToList();
                var pendingInvoices = invoices.Where(i => i.Status == "Pending").ToList();
                var overdueInvoices = invoices.Where(i => i.Status == "Overdue").ToList();

                return new BillingSummary
                {
                    TotalInvoices = totalInvoices,
                    TotalPaid = paidInvoices.Sum(i => i.Amount),
                    TotalPending = pendingInvoices.Sum(i => i.Amount),
                    TotalOverdue = overdueInvoices.Sum(i => i.Amount),
                    MonthlyRevenue = paidInvoices
                        .Where(i => i.PaymentDate.HasValue && i.PaymentDate.Value.Month == DateTime.Now.Month)
                        .Sum(i => i.Amount),
                    PaymentSuccessRate = totalInvoices > 0 ? (double)paidInvoices.Count / totalInvoices * 100 : 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving billing summary for tenant {tenantId}");
                throw;
            }
        }
    }

    public class BillingSummary
    {
        public int TotalInvoices { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal TotalPending { get; set; }
        public decimal TotalOverdue { get; set; }
        public decimal MonthlyRevenue { get; set; }
        public double PaymentSuccessRate { get; set; }
    }
}

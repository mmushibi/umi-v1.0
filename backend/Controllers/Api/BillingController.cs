using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using UmiHealthPOS.Models;
using UmiHealthPOS.Services;
using UmiHealthPOS.Data;

namespace UmiHealthPOS.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "superadmin")]
    public class BillingController : ControllerBase
    {
        private readonly IBillingService _billingService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<BillingController> _logger;

        public BillingController(
            IBillingService billingService,
            ApplicationDbContext context,
            ILogger<BillingController> logger)
        {
            _billingService = billingService;
            _context = context;
            _logger = logger;
        }

        [HttpGet("invoices")]
        public async Task<ActionResult<IEnumerable<Invoice>>> GetAllInvoices()
        {
            try
            {
                var invoices = await _billingService.GetAllInvoicesAsync();
                return Ok(invoices);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all invoices");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("invoices/{id}")]
        public async Task<ActionResult<Invoice>> GetInvoiceById(int id)
        {
            try
            {
                var invoice = await _billingService.GetInvoiceByIdAsync(id);
                if (invoice == null)
                    return NotFound();

                return Ok(invoice);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving invoice with ID: {id}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("invoices/tenant/{tenantId}")]
        public async Task<ActionResult<IEnumerable<Invoice>>> GetInvoicesByTenant(int tenantId)
        {
            try
            {
                var invoices = await _billingService.GetInvoicesByTenantIdAsync(tenantId);
                return Ok(invoices);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving invoices for tenant ID: {tenantId}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("invoices")]
        public async Task<ActionResult<Invoice>> CreateInvoice([FromBody] CreateInvoiceRequest request)
        {
            try
            {
                var invoice = new Invoice
                {
                    TenantId = request.TenantId,
                    Plan = request.Plan,
                    Amount = request.Amount,
                    IssueDate = request.IssueDate,
                    DueDate = request.DueDate,
                    Notes = request.Notes
                };

                var createdInvoice = await _billingService.CreateInvoiceAsync(invoice);
                
                // Broadcast real-time update
                await BroadcastInvoiceUpdate(createdInvoice, "created");
                
                return CreatedAtAction(nameof(GetInvoiceById), new { id = createdInvoice.Id }, createdInvoice);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating invoice");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPut("invoices/{id}")]
        public async Task<ActionResult<Invoice>> UpdateInvoice(int id, [FromBody] UpdateInvoiceRequest request)
        {
            try
            {
                var existingInvoice = await _billingService.GetInvoiceByIdAsync(id);
                if (existingInvoice == null)
                    return NotFound();

                existingInvoice.Plan = request.Plan;
                existingInvoice.Amount = request.Amount;
                existingInvoice.DueDate = request.DueDate;
                existingInvoice.Notes = request.Notes;
                existingInvoice.Status = request.Status;

                var updatedInvoice = await _billingService.UpdateInvoiceAsync(existingInvoice);
                
                // Broadcast real-time update
                await BroadcastInvoiceUpdate(updatedInvoice, "updated");
                
                return Ok(updatedInvoice);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating invoice with ID: {id}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpDelete("invoices/{id}")]
        public async Task<ActionResult> DeleteInvoice(int id)
        {
            try
            {
                var success = await _billingService.DeleteInvoiceAsync(id);
                if (!success)
                    return NotFound();

                // Broadcast real-time update
                await BroadcastInvoiceUpdate(new Invoice { Id = id }, "deleted");
                
                return Ok(new { message = "Invoice deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting invoice with ID: {id}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("invoices/{id}/credit-note")]
        public async Task<ActionResult<CreditNote>> CreateCreditNote(int id, [FromBody] CreateCreditNoteRequest request)
        {
            try
            {
                var creditNote = new CreditNote
                {
                    InvoiceId = id,
                    Amount = request.Amount,
                    Reason = request.Reason,
                    Notes = request.Notes
                };

                var createdCreditNote = await _billingService.CreateCreditNoteAsync(creditNote);
                
                // Broadcast real-time update
                await BroadcastInvoiceUpdate(new Invoice { Id = id }, "credit_issued");
                
                return Ok(createdCreditNote);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating credit note for invoice ID: {id}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("invoices/{id}/payment")]
        public async Task<ActionResult<Models.Payment>> ProcessPayment(int id, [FromBody] ProcessPaymentRequest request)
        {
            try
            {
                var payment = new Models.Payment
                {
                    InvoiceId = id,
                    Amount = request.Amount,
                    PaymentMethod = request.PaymentMethod,
                    TransactionId = request.TransactionId,
                    Status = request.Status,
                    FailureReason = request.FailureReason
                };

                var processedPayment = await _billingService.ProcessPaymentAsync(payment);
                
                // Broadcast real-time update
                await BroadcastInvoiceUpdate(new Invoice { Id = id }, "payment_processed");
                
                return Ok(processedPayment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing payment for invoice ID: {id}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("tenants")]
        public async Task<ActionResult<IEnumerable<Tenant>>> GetAllTenants()
        {
            try
            {
                var tenants = await _billingService.GetAllTenantsAsync();
                return Ok(tenants);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all tenants");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("tenants/{id}")]
        public async Task<ActionResult<Tenant>> GetTenantById(int id)
        {
            try
            {
                var tenant = await _billingService.GetTenantByIdAsync(id);
                if (tenant == null)
                    return NotFound();

                return Ok(tenant);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving tenant with ID: {id}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("summary")]
        public async Task<ActionResult<BillingSummary>> GetBillingSummary()
        {
            try
            {
                var summary = await _billingService.GetBillingSummaryAsync();
                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving billing summary");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("summary/tenant/{tenantId}")]
        public async Task<ActionResult<BillingSummary>> GetTenantBillingSummary(int tenantId)
        {
            try
            {
                var summary = await _billingService.GetTenantBillingSummaryAsync(tenantId);
                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving billing summary for tenant ID: {tenantId}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("export/invoices")]
        public async Task<ActionResult> ExportInvoices([FromQuery] int? tenantId = null)
        {
            try
            {
                var invoices = tenantId.HasValue 
                    ? await _billingService.GetInvoicesByTenantIdAsync(tenantId.Value)
                    : await _billingService.GetAllInvoicesAsync();

                var csvContent = GenerateInvoiceCsv(invoices);
                var fileName = tenantId.HasValue 
                    ? $"tenant_{tenantId.Value}_invoices.csv"
                    : "all_invoices.csv";

                var bytes = System.Text.Encoding.UTF8.GetBytes(csvContent);
                return File(bytes, "text/csv", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting invoices");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        private async Task BroadcastInvoiceUpdate(Invoice invoice, string action)
        {
            // This would integrate with SignalR for real-time updates
            // For now, we'll just log the action
            _logger.LogInformation($"Invoice {action}: {invoice.Number} for tenant {invoice.TenantId}");
        }

        private string GenerateInvoiceCsv(IEnumerable<Invoice> invoices)
        {
            var headers = new[] { "Invoice Number", "Tenant", "Plan", "Amount", "Issue Date", "Due Date", "Status", "Payment Date" };
            var rows = invoices.Select(inv => new[]
            {
                inv.Number,
                inv.Tenant?.Company ?? "",
                inv.Plan,
                inv.Amount.ToString(),
                inv.IssueDate.ToString("yyyy-MM-dd"),
                inv.DueDate.ToString("yyyy-MM-dd"),
                inv.Status,
                inv.PaymentDate?.ToString("yyyy-MM-dd") ?? ""
            });

            return string.Join("\n", headers.Concat(rows).Select(row => string.Join(",", row)));
        }
    }

    // Request DTOs
    public class CreateInvoiceRequest
    {
        public int TenantId { get; set; }
        public string Plan { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime IssueDate { get; set; }
        public DateTime DueDate { get; set; }
        public string? Notes { get; set; }
    }

    public class UpdateInvoiceRequest
    {
        public string Plan { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime DueDate { get; set; }
        public string? Notes { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class CreateCreditNoteRequest
    {
        public decimal Amount { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }

    public class ProcessPaymentRequest
    {
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string? TransactionId { get; set; }
        public string Status { get; set; } = "Completed";
        public string? FailureReason { get; set; }
    }
}

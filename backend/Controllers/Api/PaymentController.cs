using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using UmiHealthPOS.Models;
using UmiHealthPOS.Data;

namespace UmiHealthPOS.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PaymentController : ControllerBase
    {
        private readonly ILogger<PaymentController> _logger;
        private readonly ApplicationDbContext _context;

        public PaymentController(
            ILogger<PaymentController> logger,
            ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        // Quotations
        [HttpGet("quotations")]
        public async Task<ActionResult<List<QuotationDto>>> GetQuotations(
            [FromQuery] string search = "",
            [FromQuery] string status = "")
        {
            try
            {
                var query = _context.Quotations
                    .Include(q => q.QuotationItems)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(q =>
                        q.QuotationNumber.Contains(search) ||
                        (q.PatientName != null && q.PatientName.Contains(search)) ||
                        (q.Email != null && q.Email.Contains(search)));
                }

                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(q => q.Status == status);
                }

                var quotations = await query
                    .OrderByDescending(q => q.Date)
                    .Select(q => new QuotationDto
                    {
                        Id = q.Id,
                        QuotationNumber = q.QuotationNumber,
                        Date = q.Date,
                        PatientName = q.PatientName,
                        Email = q.Email,
                        Phone = q.Phone,
                        Subtotal = q.Subtotal,
                        Tax = q.Tax,
                        Total = q.Total,
                        Status = q.Status,
                        ValidUntil = q.ValidUntil,
                        Notes = q.Notes,
                        ItemCount = q.QuotationItems.Count
                    })
                    .ToListAsync();

                return Ok(quotations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving quotations");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("quotations")]
        public async Task<ActionResult<QuotationDto>> CreateQuotation([FromBody] CreateQuotationRequest request)
        {
            try
            {
                var quotationNumber = $"Q{DateTime.UtcNow:yyyyMMddHHmmss}";

                var quotation = new Quotation
                {
                    QuotationNumber = quotationNumber,
                    Date = DateTime.UtcNow,
                    PatientName = request.PatientName,
                    Email = request.Email,
                    Phone = request.Phone,
                    Subtotal = request.Items.Sum(i => i.Quantity * i.Price),
                    Tax = request.Items.Sum(i => i.Quantity * i.Price) * 0.16m, // 16% tax
                    Status = "Pending",
                    ValidUntil = DateTime.UtcNow.AddDays(30),
                    Notes = request.Notes
                };

                quotation.Total = quotation.Subtotal + quotation.Tax;

                foreach (var item in request.Items)
                {
                    quotation.QuotationItems.Add(new QuotationItem
                    {
                        Description = item.Description,
                        Quantity = item.Quantity,
                        UnitPrice = item.Price,
                        Total = item.Quantity * item.Price
                    });
                }

                _context.Quotations.Add(quotation);
                await _context.SaveChangesAsync();

                var quotationDto = new QuotationDto
                {
                    Id = quotation.Id,
                    QuotationNumber = quotation.QuotationNumber,
                    Date = quotation.Date,
                    PatientName = quotation.PatientName,
                    Email = quotation.Email,
                    Phone = quotation.Phone,
                    Subtotal = quotation.Subtotal,
                    Tax = quotation.Tax,
                    Total = quotation.Total,
                    Status = quotation.Status,
                    ValidUntil = quotation.ValidUntil,
                    Notes = quotation.Notes,
                    ItemCount = quotation.QuotationItems.Count
                };

                return CreatedAtAction(nameof(GetQuotations), new { id = quotation.Id }, quotationDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating quotation");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // Invoices
        [HttpGet("invoices")]
        public async Task<ActionResult<List<InvoiceDto>>> GetInvoices(
            [FromQuery] string search = "",
            [FromQuery] string status = "")
        {
            try
            {
                var query = _context.SalesInvoices
                    .Include(si => si.SalesInvoiceItems)
                    .Include(si => si.Payments)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(si =>
                        si.InvoiceNumber.Contains(search) ||
                        (si.PatientName != null && si.PatientName.Contains(search)) ||
                        (si.Email != null && si.Email.Contains(search)));
                }

                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(si => si.Status == status);
                }

                var invoices = await query
                    .OrderByDescending(si => si.Date)
                    .Select(si => new InvoiceDto
                    {
                        Id = si.Id,
                        InvoiceNumber = si.InvoiceNumber,
                        Date = si.Date,
                        PatientName = si.PatientName,
                        Email = si.Email,
                        Phone = si.Phone,
                        Subtotal = si.Subtotal,
                        Tax = si.Tax,
                        Total = si.Total,
                        AmountPaid = si.AmountPaid,
                        Balance = si.Balance,
                        Status = si.Status,
                        PaymentMethod = si.PaymentMethod,
                        PaymentReference = si.PaymentReference,
                        DueDate = si.DueDate,
                        ItemCount = si.SalesInvoiceItems.Count,
                        PaymentCount = si.Payments.Count
                    })
                    .ToListAsync();

                return Ok(invoices);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving invoices");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("invoices")]
        public async Task<ActionResult<InvoiceDto>> CreateInvoice([FromBody] CreateInvoiceRequest request)
        {
            try
            {
                var invoiceNumber = $"INV{DateTime.UtcNow:yyyyMMddHHmmss}";

                var invoice = new SalesInvoice
                {
                    InvoiceNumber = invoiceNumber,
                    Date = DateTime.UtcNow,
                    PatientName = request.PatientName,
                    Email = request.Email,
                    Phone = request.Phone,
                    Subtotal = request.Items.Sum(i => i.Quantity * i.Price),
                    Tax = request.Items.Sum(i => i.Quantity * i.Price) * 0.16m, // 16% tax
                    AmountPaid = 0,
                    Status = "Unpaid",
                    DueDate = DateTime.UtcNow.AddDays(30)
                };

                invoice.Total = invoice.Subtotal + invoice.Tax;
                invoice.Balance = invoice.Total;

                foreach (var item in request.Items)
                {
                    invoice.SalesInvoiceItems.Add(new SalesInvoiceItem
                    {
                        Description = item.Description,
                        Quantity = item.Quantity,
                        UnitPrice = item.Price,
                        Total = item.Quantity * item.Price
                    });
                }

                _context.SalesInvoices.Add(invoice);
                await _context.SaveChangesAsync();

                var invoiceDto = new InvoiceDto
                {
                    Id = invoice.Id,
                    InvoiceNumber = invoice.InvoiceNumber,
                    Date = invoice.Date,
                    PatientName = invoice.PatientName,
                    Email = invoice.Email,
                    Phone = invoice.Phone,
                    Subtotal = invoice.Subtotal,
                    Tax = invoice.Tax,
                    Total = invoice.Total,
                    AmountPaid = invoice.AmountPaid,
                    Balance = invoice.Balance,
                    Status = invoice.Status,
                    PaymentMethod = invoice.PaymentMethod,
                    PaymentReference = invoice.PaymentReference,
                    DueDate = invoice.DueDate,
                    ItemCount = invoice.SalesInvoiceItems.Count,
                    PaymentCount = invoice.Payments.Count
                };

                return CreatedAtAction(nameof(GetInvoices), new { id = invoice.Id }, invoiceDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating invoice");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("invoices/{id}/payments")]
        public async Task<ActionResult> AddInvoicePayment(int id, [FromBody] AddPaymentRequest request)
        {
            try
            {
                var invoice = await _context.SalesInvoices
                    .Include(si => si.Payments)
                    .FirstOrDefaultAsync(si => si.Id == id);

                if (invoice == null)
                {
                    return NotFound(new { error = "Invoice not found" });
                }

                if (request.Amount > invoice.Balance)
                {
                    return BadRequest(new { error = "Payment amount exceeds balance" });
                }

                var payment = new SalesInvoicePayment
                {
                    SalesInvoiceId = id,
                    Amount = request.Amount,
                    PaymentMethod = request.PaymentMethod,
                    Reference = request.Reference,
                    Notes = request.Notes
                };

                invoice.Payments.Add(payment);
                invoice.AmountPaid += request.Amount;
                invoice.Balance -= request.Amount;

                if (invoice.Balance <= 0)
                {
                    invoice.Status = "Paid";
                    invoice.PaymentMethod = request.PaymentMethod;
                    invoice.PaymentReference = request.Reference;
                }
                else
                {
                    invoice.Status = "PartiallyPaid";
                }

                await _context.SaveChangesAsync();

                return Ok(new { message = "Payment added successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding payment to invoice {InvoiceId}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // Credit Notes
        [HttpGet("credit-notes")]
        public async Task<ActionResult<List<CreditNoteDto>>> GetCreditNotes(
            [FromQuery] string search = "",
            [FromQuery] string status = "")
        {
            try
            {
                var query = _context.SalesCreditNotes
                    .Include(scn => scn.SalesCreditNoteItems)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(scn =>
                        scn.CreditNoteNumber.Contains(search) ||
                        (scn.PatientName != null && scn.PatientName.Contains(search)) ||
                        (scn.Email != null && scn.Email.Contains(search)));
                }

                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(scn => scn.Status == status);
                }

                var creditNotes = await query
                    .OrderByDescending(scn => scn.Date)
                    .Select(scn => new CreditNoteDto
                    {
                        Id = scn.Id,
                        CreditNoteNumber = scn.CreditNoteNumber,
                        Date = scn.Date,
                        PatientName = scn.PatientName,
                        Email = scn.Email,
                        OriginalInvoiceId = scn.OriginalSalesInvoiceId,
                        Reason = scn.Reason,
                        TotalAmount = scn.TotalAmount,
                        Status = scn.Status,
                        ItemCount = scn.SalesCreditNoteItems.Count
                    })
                    .ToListAsync();

                return Ok(creditNotes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving credit notes");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // Insurance Claims
        [HttpGet("insurance-claims")]
        public async Task<ActionResult<List<InsuranceClaimDto>>> GetInsuranceClaims(
            [FromQuery] string search = "",
            [FromQuery] string status = "")
        {
            try
            {
                var query = _context.InsuranceClaims.AsQueryable();

                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(ic =>
                        ic.ClaimNumber.Contains(search) ||
                        (ic.PatientName != null && ic.PatientName.Contains(search)) ||
                        (ic.InsuranceProvider != null && ic.InsuranceProvider.Contains(search)));
                }

                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(ic => ic.Status == status);
                }

                var claims = await query
                    .OrderByDescending(ic => ic.Date)
                    .Select(ic => new InsuranceClaimDto
                    {
                        Id = ic.Id,
                        ClaimNumber = ic.ClaimNumber,
                        Date = ic.Date,
                        PatientName = ic.PatientName,
                        InsuranceProvider = ic.InsuranceProvider,
                        PolicyNumber = ic.PolicyNumber,
                        SalesInvoiceId = ic.SalesInvoiceId,
                        ClaimAmount = ic.ClaimAmount,
                        ApprovedAmount = ic.ApprovedAmount,
                        Status = ic.Status,
                        Notes = ic.Notes,
                        ApprovedDate = ic.ApprovedDate
                    })
                    .ToListAsync();

                return Ok(claims);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving insurance claims");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }
    }

    // DTOs
    public class QuotationDto
    {
        public int Id { get; set; }
        public string QuotationNumber { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string? PatientName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Tax { get; set; }
        public decimal Total { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime ValidUntil { get; set; }
        public string? Notes { get; set; }
        public int ItemCount { get; set; }
    }

    public class CreateQuotationRequest
    {
        public string? PatientName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public List<QuotationItemRequest> Items { get; set; } = new();
        public string? Notes { get; set; }
    }

    public class QuotationItemRequest
    {
        public string? Description { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }

    public class InvoiceDto
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string? PatientName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Tax { get; set; }
        public decimal Total { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal Balance { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? PaymentMethod { get; set; }
        public string? PaymentReference { get; set; }
        public DateTime? DueDate { get; set; }
        public int ItemCount { get; set; }
        public int PaymentCount { get; set; }
    }

    public class CreateInvoiceRequest
    {
        public string? PatientName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public List<InvoiceItemRequest> Items { get; set; } = new();
    }

    public class InvoiceItemRequest
    {
        public string? Description { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }

    public class AddPaymentRequest
    {
        public decimal Amount { get; set; }
        public string? PaymentMethod { get; set; }
        public string? Reference { get; set; }
        public string? Notes { get; set; }
    }

    public class CreditNoteDto
    {
        public int Id { get; set; }
        public string CreditNoteNumber { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string? PatientName { get; set; }
        public string? Email { get; set; }
        public int? OriginalInvoiceId { get; set; }
        public string? Reason { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public int ItemCount { get; set; }
    }

    public class InsuranceClaimDto
    {
        public int Id { get; set; }
        public string ClaimNumber { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string? PatientName { get; set; }
        public string? InsuranceProvider { get; set; }
        public string? PolicyNumber { get; set; }
        public int? SalesInvoiceId { get; set; }
        public decimal ClaimAmount { get; set; }
        public decimal ApprovedAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public DateTime? ApprovedDate { get; set; }
    }
}

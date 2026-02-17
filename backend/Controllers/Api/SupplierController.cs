using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using UmiHealthPOS.Models;
using UmiHealthPOS.Data;

namespace UmiHealthPOS.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SupplierController : ControllerBase
    {
        private readonly ILogger<SupplierController> _logger;
        private readonly ApplicationDbContext _context;

        public SupplierController(
            ILogger<SupplierController> logger,
            ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        private string GetCurrentUserId()
        {
            return User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value ?? "";
        }

        private string GetCurrentTenantId()
        {
            return User.FindFirst("TenantId")?.Value ?? "";
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Supplier>>> GetSuppliers(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null,
            [FromQuery] string? status = null,
            [FromQuery] string? category = null)
        {
            try
            {
                var tenantId = GetCurrentTenantId();
                if (string.IsNullOrEmpty(tenantId))
                {
                    return Unauthorized(new { error = "Tenant not found" });
                }

                var query = _context.Suppliers
                    .Where(s => s.TenantId == tenantId && s.IsActive)
                    .Include(s => s.Contacts)
                    .Include(s => s.Products)
                    .AsNoTracking();

                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(s =>
                        s.BusinessName.Contains(search) ||
                        (s.TradeName != null && s.TradeName.Contains(search)) ||
                        (s.ContactPerson != null && s.ContactPerson.Contains(search)) ||
                        (s.Email != null && s.Email.Contains(search)) ||
                        (s.PrimaryPhoneNumber != null && s.PrimaryPhoneNumber.Contains(search)) ||
                        (s.RegistrationNumber != null && s.RegistrationNumber.Contains(search)));
                }

                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(s => s.SupplierStatus == status);
                }

                if (!string.IsNullOrEmpty(category))
                {
                    query = query.Where(s => s.SupplierCategory == category);
                }

                var totalCount = await query.CountAsync();
                var suppliers = await query
                    .OrderByDescending(s => s.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return Ok(new
                {
                    suppliers,
                    totalCount,
                    currentPage = page,
                    pageSize,
                    totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving suppliers");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Supplier>> GetSupplier(int id)
        {
            try
            {
                var tenantId = GetCurrentTenantId();
                if (string.IsNullOrEmpty(tenantId))
                {
                    return Unauthorized(new { error = "Tenant not found" });
                }

                var supplier = await _context.Suppliers
                    .AsNoTracking()
                    .Where(s => s.Id == id && s.TenantId == tenantId && s.IsActive)
                    .Include(s => s.Contacts)
                    .Include(s => s.Products)
                        .ThenInclude(p => p.Product)
                    .Include(s => s.Products)
                        .ThenInclude(p => p.InventoryItem)
                    .FirstOrDefaultAsync();

                if (supplier == null)
                {
                    return NotFound(new { error = "Supplier not found" });
                }

                return Ok(supplier);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving supplier {SupplierId}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost]
        public async Task<ActionResult<Supplier>> CreateSupplier([FromBody] CreateSupplierRequest request)
        {
            try
            {
                var tenantId = GetCurrentTenantId();
                var userId = GetCurrentUserId();

                if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Check for duplicate supplier code or registration number
                var existingSupplier = await _context.Suppliers
                    .FirstOrDefaultAsync(s =>
                        (s.SupplierCode == request.SupplierCode && s.TenantId == tenantId) ||
                        (s.RegistrationNumber == request.RegistrationNumber && s.TenantId == tenantId && !string.IsNullOrEmpty(request.RegistrationNumber)));

                if (existingSupplier != null)
                {
                    return Conflict(new { error = "Supplier with this code or registration number already exists" });
                }

                // Generate supplier code if not provided
                var supplierCode = !string.IsNullOrEmpty(request.SupplierCode)
                    ? await GenerateSupplierCode(tenantId)
                    : request.SupplierCode;

                var supplier = new Supplier
                {
                    SupplierCode = supplierCode,
                    BusinessName = request.BusinessName,
                    TradeName = request.TradeName,
                    RegistrationNumber = request.RegistrationNumber,
                    TaxIdentificationNumber = request.TaxIdentificationNumber,
                    PharmacyLicenseNumber = request.PharmacyLicenseNumber,
                    DrugSupplierLicense = request.DrugSupplierLicense,
                    ContactPerson = request.ContactPerson,
                    ContactPersonTitle = request.ContactPersonTitle,
                    PrimaryPhoneNumber = request.PrimaryPhoneNumber,
                    SecondaryPhoneNumber = request.SecondaryPhoneNumber,
                    Email = request.Email,
                    AlternativeEmail = request.AlternativeEmail,
                    Website = request.Website,
                    PhysicalAddress = request.PhysicalAddress,
                    PostalAddress = request.PostalAddress,
                    City = request.City,
                    Province = request.Province,
                    Country = request.Country ?? "Zambia",
                    PostalCode = request.PostalCode,
                    BusinessType = request.BusinessType,
                    Industry = request.Industry,
                    YearsInOperation = request.YearsInOperation,
                    NumberOfEmployees = request.NumberOfEmployees,
                    AnnualRevenue = request.AnnualRevenue,
                    BankName = request.BankName,
                    BankAccountNumber = request.BankAccountNumber,
                    BankAccountName = request.BankAccountName,
                    BankBranch = request.BankBranch,
                    BankCode = request.BankCode,
                    SwiftCode = request.SwiftCode,
                    PaymentTerms = request.PaymentTerms ?? "Net 30",
                    CreditLimit = request.CreditLimit ?? 0.00m,
                    CreditPeriod = request.CreditPeriod ?? 30,
                    DiscountTerms = request.DiscountTerms,
                    EarlyPaymentDiscount = request.EarlyPaymentDiscount ?? 0.00m,
                    SupplierCategory = request.SupplierCategory,
                    SupplierStatus = request.SupplierStatus ?? "Active",
                    PriorityLevel = request.PriorityLevel ?? "Medium",
                    IsPreferred = request.IsPreferred ?? false,
                    IsBlacklisted = request.IsBlacklisted ?? false,
                    BlacklistReason = request.IsBlacklisted == true ? request.BlacklistReason : null,
                    ZambianRegistered = request.ZambianRegistered ?? false,
                    GmpCertified = request.GmpCertified ?? false,
                    IsoCertified = request.IsoCertified ?? false,
                    CertificationExpiryDate = request.CertificationExpiryDate,
                    RegulatoryComplianceStatus = request.RegulatoryComplianceStatus ?? "Pending",
                    TenantId = tenantId,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Suppliers.Add(supplier);
                await _context.SaveChangesAsync();

                // Add contacts if provided
                if (request.Contacts != null && request.Contacts.Any())
                {
                    foreach (var contactRequest in request.Contacts.Where(c => c.IsPrimary))
                    {
                        var contact = new SupplierContact
                        {
                            SupplierId = supplier.Id,
                            ContactName = contactRequest.ContactName,
                            ContactTitle = contactRequest.ContactTitle,
                            Department = contactRequest.Department,
                            PhoneNumber = contactRequest.PhoneNumber,
                            MobileNumber = contactRequest.MobileNumber,
                            Email = contactRequest.Email,
                            IsPrimary = contactRequest.IsPrimary,
                            IsOrderContact = contactRequest.IsOrderContact,
                            IsBillingContact = contactRequest.IsBillingContact,
                            IsTechnicalContact = contactRequest.IsTechnicalContact,
                            Notes = contactRequest.Notes,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };

                        _context.SupplierContacts.Add(contact);
                    }

                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation("Supplier {SupplierId} created by user {UserId}", supplier.Id, userId);

                return CreatedAtAction(nameof(GetSupplier), new { id = supplier.Id }, supplier);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating supplier");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<Supplier>> UpdateSupplier(int id, [FromBody] UpdateSupplierRequest request)
        {
            try
            {
                var tenantId = GetCurrentTenantId();
                var userId = GetCurrentUserId();

                if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var supplier = await _context.Suppliers
                    .Include(s => s.Contacts)
                    .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == tenantId && s.IsActive);

                if (supplier == null)
                {
                    return NotFound(new { error = "Supplier not found" });
                }

                // Check for duplicate supplier code or registration number (excluding current supplier)
                var duplicateSupplier = await _context.Suppliers
                    .FirstOrDefaultAsync(s =>
                        s.Id != id &&
                        s.TenantId == tenantId &&
                        (s.RegistrationNumber == request.RegistrationNumber && !string.IsNullOrEmpty(request.RegistrationNumber)));

                if (duplicateSupplier != null)
                {
                    return Conflict(new { error = "Supplier with this code or registration number already exists" });
                }

                // Update supplier details
                supplier.BusinessName = request.BusinessName;
                supplier.TradeName = request.TradeName;
                supplier.RegistrationNumber = request.RegistrationNumber;
                supplier.TaxIdentificationNumber = request.TaxIdentificationNumber;
                supplier.PharmacyLicenseNumber = request.PharmacyLicenseNumber;
                supplier.DrugSupplierLicense = request.DrugSupplierLicense;
                supplier.ContactPerson = request.ContactPerson;
                supplier.ContactPersonTitle = request.ContactPersonTitle;
                supplier.PrimaryPhoneNumber = request.PrimaryPhoneNumber;
                supplier.SecondaryPhoneNumber = request.SecondaryPhoneNumber;
                supplier.Email = request.Email;
                supplier.AlternativeEmail = request.AlternativeEmail;
                supplier.Website = request.Website;
                supplier.PhysicalAddress = request.PhysicalAddress;
                supplier.PostalAddress = request.PostalAddress;
                supplier.City = request.City;
                supplier.Province = request.Province;
                supplier.Country = request.Country;
                supplier.PostalCode = request.PostalCode;
                supplier.BusinessType = request.BusinessType;
                supplier.Industry = request.Industry;
                supplier.YearsInOperation = request.YearsInOperation;
                supplier.NumberOfEmployees = request.NumberOfEmployees;
                supplier.AnnualRevenue = request.AnnualRevenue;
                supplier.BankName = request.BankName;
                supplier.BankAccountNumber = request.BankAccountNumber;
                supplier.BankAccountName = request.BankAccountName;
                supplier.BankBranch = request.BankBranch;
                supplier.BankCode = request.BankCode;
                supplier.SwiftCode = request.SwiftCode;
                supplier.PaymentTerms = request.PaymentTerms;
                supplier.CreditLimit = request.CreditLimit ?? 0;
                supplier.CreditPeriod = request.CreditPeriod ?? 0;
                supplier.DiscountTerms = request.DiscountTerms ?? "";
                supplier.EarlyPaymentDiscount = request.EarlyPaymentDiscount ?? 0.0m;
                supplier.SupplierCategory = request.SupplierCategory;
                supplier.SupplierStatus = request.SupplierStatus;
                supplier.PriorityLevel = request.PriorityLevel;
                supplier.IsPreferred = request.IsPreferred ?? false;
                supplier.IsBlacklisted = request.IsBlacklisted ?? false;
                supplier.BlacklistReason = (request.IsBlacklisted ?? false) ? request.BlacklistReason : null;
                supplier.ZambianRegistered = request.ZambianRegistered ?? false;
                supplier.GmpCertified = request.GmpCertified ?? false;
                supplier.IsoCertified = request.IsoCertified ?? false;
                supplier.CertificationExpiryDate = request.CertificationExpiryDate;
                supplier.RegulatoryComplianceStatus = request.RegulatoryComplianceStatus;
                supplier.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Supplier {SupplierId} updated by user {UserId}", supplier.Id, userId);

                return Ok(supplier);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating supplier {SupplierId}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteSupplier(int id)
        {
            try
            {
                var tenantId = GetCurrentTenantId();
                var userId = GetCurrentUserId();

                if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var supplier = await _context.Suppliers
                    .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == tenantId && s.IsActive);

                if (supplier == null)
                {
                    return NotFound(new { error = "Supplier not found" });
                }

                // Check if supplier has active products
                var activeProducts = await _context.SupplierProducts
                    .AnyAsync(sp => sp.SupplierId == id && sp.IsActive);

                if (activeProducts)
                {
                    return BadRequest(new { error = "Cannot delete supplier with active products" });
                }

                supplier.IsActive = false;
                supplier.SupplierStatus = "Deleted";
                supplier.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Supplier {SupplierId} deleted by user {UserId}", id, userId);

                return Ok(new { message = "Supplier deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting supplier {SupplierId}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("{id}/products")]
        public async Task<ActionResult<IEnumerable<SupplierProduct>>> GetSupplierProducts(int id)
        {
            try
            {
                var tenantId = GetCurrentTenantId();
                if (string.IsNullOrEmpty(tenantId))
                {
                    return Unauthorized(new { error = "Tenant not found" });
                }

                // Verify supplier exists and belongs to tenant
                var supplier = await _context.Suppliers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == tenantId && s.IsActive);

                if (supplier == null)
                {
                    return NotFound(new { error = "Supplier not found" });
                }

                var products = await _context.SupplierProducts
                    .AsNoTracking()
                    .Where(sp => sp.SupplierId == id && sp.IsActive)
                    .Include(sp => sp.Product)
                    .Include(sp => sp.InventoryItem)
                    .OrderByDescending(sp => sp.CreatedAt)
                    .ToListAsync();

                return Ok(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving products for supplier {SupplierId}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("{id}/products")]
        public async Task<ActionResult<SupplierProduct>> AddSupplierProduct(int id, [FromBody] CreateSupplierProductRequest request)
        {
            try
            {
                var tenantId = GetCurrentTenantId();
                var userId = GetCurrentUserId();

                if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Verify supplier exists and belongs to tenant
                var supplier = await _context.Suppliers
                    .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == tenantId && s.IsActive);

                if (supplier == null)
                {
                    return NotFound(new { error = "Supplier not found" });
                }

                var supplierProduct = new SupplierProduct
                {
                    SupplierId = id,
                    ProductId = request.ProductId,
                    InventoryItemId = request.InventoryItemId,
                    SupplierProductCode = request.SupplierProductCode,
                    SupplierProductName = request.SupplierProductName,
                    Description = request.Description,
                    UnitCost = request.UnitCost,
                    Currency = request.Currency ?? "ZMW",
                    MinimumOrderQuantity = request.MinimumOrderQuantity,
                    MaximumOrderQuantity = request.MaximumOrderQuantity,
                    OrderMultiples = request.OrderMultiples,
                    MinimumOrderValue = request.MinimumOrderValue,
                    IsAvailable = request.IsAvailable ?? true,
                    LeadTimeDays = request.LeadTimeDays ?? 7,
                    QualityGrade = request.QualityGrade,
                    BatchNumber = request.BatchNumber,
                    ManufactureDate = request.ManufactureDate,
                    ExpiryDate = request.ExpiryDate,
                    StorageRequirements = request.StorageRequirements,
                    SupplierCatalogNumber = request.SupplierCatalogNumber,
                    SupplierBarcode = request.SupplierBarcode,
                    PackagingInformation = request.PackagingInformation,
                    WeightPerUnit = request.WeightPerUnit,
                    Dimensions = request.Dimensions,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.SupplierProducts.Add(supplierProduct);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Supplier product {ProductId} added to supplier {SupplierId} by user {UserId}",
                    supplierProduct.Id, id, userId);

                return CreatedAtAction(nameof(GetSupplierProducts), new { id = id }, supplierProduct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding product to supplier {SupplierId}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<Supplier>>> SearchSuppliers([FromQuery] string query)
        {
            try
            {
                var tenantId = GetCurrentTenantId();
                if (string.IsNullOrEmpty(tenantId))
                {
                    return Unauthorized(new { error = "Tenant not found" });
                }

                if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
                {
                    return BadRequest(new { error = "Search query must be at least 2 characters" });
                }

                var suppliers = await _context.Suppliers
                    .AsNoTracking()
                    .Where(s =>
                        s.TenantId == tenantId &&
                        s.IsActive &&
                        (s.BusinessName.Contains(query) ||
                         (s.TradeName != null && s.TradeName.Contains(query)) ||
                         (s.ContactPerson != null && s.ContactPerson.Contains(query)) ||
                         (s.Email != null && s.Email.Contains(query)) ||
                         (s.PrimaryPhoneNumber != null && s.PrimaryPhoneNumber.Contains(query))))
                    .OrderByDescending(s => s.CreatedAt)
                    .Take(20)
                    .ToListAsync();

                return Ok(suppliers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching suppliers with query: {Query}", query);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        private async Task<string> GenerateSupplierCode(string tenantId)
        {
            var datePrefix = DateTime.UtcNow.ToString("yyyyMMdd");
            var count = await _context.Suppliers
                .CountAsync(s => s.TenantId == tenantId && s.SupplierCode.StartsWith(datePrefix));

            return $"SUP{datePrefix}{(count + 1):D3}";
        }
    }

    // Request/Response DTOs
    public class CreateSupplierRequest
    {
        [StringLength(50)]
        public string? SupplierCode { get; set; }

        [Required]
        [StringLength(200)]
        public string BusinessName { get; set; } = string.Empty;

        [StringLength(200)]
        public string? TradeName { get; set; }

        [StringLength(100)]
        public string? RegistrationNumber { get; set; }

        [StringLength(50)]
        public string? TaxIdentificationNumber { get; set; }

        [StringLength(100)]
        public string? PharmacyLicenseNumber { get; set; }

        [StringLength(100)]
        public string? DrugSupplierLicense { get; set; }

        [StringLength(100)]
        public string? ContactPerson { get; set; }

        [StringLength(50)]
        public string? ContactPersonTitle { get; set; }

        [StringLength(20)]
        public string? PrimaryPhoneNumber { get; set; }

        [StringLength(20)]
        public string? SecondaryPhoneNumber { get; set; }

        [StringLength(100)]
        public string? Email { get; set; }

        [StringLength(100)]
        public string? AlternativeEmail { get; set; }

        [StringLength(200)]
        public string? Website { get; set; }

        [StringLength(500)]
        public string? PhysicalAddress { get; set; }

        [StringLength(500)]
        public string? PostalAddress { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(100)]
        public string? Province { get; set; }

        [StringLength(100)]
        public string? Country { get; set; }

        [StringLength(20)]
        public string? PostalCode { get; set; }

        [StringLength(50)]
        public string? BusinessType { get; set; }

        [StringLength(100)]
        public string? Industry { get; set; }

        public int? YearsInOperation { get; set; }
        public int? NumberOfEmployees { get; set; }

        [Column(TypeName = "decimal(15,2)")]
        public decimal? AnnualRevenue { get; set; }

        [StringLength(100)]
        public string? BankName { get; set; }

        [StringLength(50)]
        public string? BankAccountNumber { get; set; }

        [StringLength(100)]
        public string? BankAccountName { get; set; }

        [StringLength(100)]
        public string? BankBranch { get; set; }

        [StringLength(20)]
        public string? BankCode { get; set; }

        [StringLength(20)]
        public string? SwiftCode { get; set; }

        [StringLength(50)]
        public string? PaymentTerms { get; set; }

        [Column(TypeName = "decimal(15,2)")]
        public decimal? CreditLimit { get; set; }

        public int? CreditPeriod { get; set; }

        [StringLength(100)]
        public string? DiscountTerms { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal? EarlyPaymentDiscount { get; set; }

        [StringLength(50)]
        public string? SupplierCategory { get; set; }

        [StringLength(20)]
        public string? SupplierStatus { get; set; }

        [StringLength(20)]
        public string? PriorityLevel { get; set; }

        public bool? IsPreferred { get; set; }
        public bool? IsBlacklisted { get; set; }

        [StringLength(500)]
        public string? BlacklistReason { get; set; }

        public bool? ZambianRegistered { get; set; }
        public bool? GmpCertified { get; set; }
        public bool? IsoCertified { get; set; }
        public DateTime? CertificationExpiryDate { get; set; }

        [StringLength(20)]
        public string? RegulatoryComplianceStatus { get; set; }

        public List<CreateSupplierContactRequest>? Contacts { get; set; }
    }

    public class UpdateSupplierRequest
    {
        [Required]
        [StringLength(200)]
        public string BusinessName { get; set; } = string.Empty;

        [StringLength(200)]
        public string? TradeName { get; set; }

        [StringLength(100)]
        public string? RegistrationNumber { get; set; }

        [StringLength(50)]
        public string? TaxIdentificationNumber { get; set; }

        [StringLength(100)]
        public string? PharmacyLicenseNumber { get; set; }

        [StringLength(100)]
        public string? DrugSupplierLicense { get; set; }

        [StringLength(100)]
        public string? ContactPerson { get; set; }

        [StringLength(50)]
        public string? ContactPersonTitle { get; set; }

        [StringLength(20)]
        public string? PrimaryPhoneNumber { get; set; }

        [StringLength(20)]
        public string? SecondaryPhoneNumber { get; set; }

        [StringLength(100)]
        public string? Email { get; set; }

        [StringLength(100)]
        public string? AlternativeEmail { get; set; }

        [StringLength(200)]
        public string? Website { get; set; }

        [StringLength(500)]
        public string? PhysicalAddress { get; set; }

        [StringLength(500)]
        public string? PostalAddress { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(100)]
        public string? Province { get; set; }

        [StringLength(100)]
        public string? Country { get; set; }

        [StringLength(20)]
        public string? PostalCode { get; set; }

        [StringLength(50)]
        public string? BusinessType { get; set; }

        [StringLength(100)]
        public string? Industry { get; set; }

        public int? YearsInOperation { get; set; }
        public int? NumberOfEmployees { get; set; }

        [Column(TypeName = "decimal(15,2)")]
        public decimal? AnnualRevenue { get; set; }

        [StringLength(100)]
        public string? BankName { get; set; }

        [StringLength(50)]
        public string? BankAccountNumber { get; set; }

        [StringLength(100)]
        public string? BankAccountName { get; set; }

        [StringLength(100)]
        public string? BankBranch { get; set; }

        [StringLength(20)]
        public string? BankCode { get; set; }

        [StringLength(20)]
        public string? SwiftCode { get; set; }

        [StringLength(50)]
        public string? PaymentTerms { get; set; }

        [Column(TypeName = "decimal(15,2)")]
        public decimal? CreditLimit { get; set; }

        public int? CreditPeriod { get; set; }

        [StringLength(100)]
        public string? DiscountTerms { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal? EarlyPaymentDiscount { get; set; }

        [StringLength(50)]
        public string? SupplierCategory { get; set; }

        [StringLength(20)]
        public string? SupplierStatus { get; set; }

        [StringLength(20)]
        public string? PriorityLevel { get; set; }

        public bool? IsPreferred { get; set; }
        public bool? IsBlacklisted { get; set; }

        [StringLength(500)]
        public string? BlacklistReason { get; set; }

        public bool? ZambianRegistered { get; set; }
        public bool? GmpCertified { get; set; }
        public bool? IsoCertified { get; set; }
        public DateTime? CertificationExpiryDate { get; set; }

        [StringLength(20)]
        public string? RegulatoryComplianceStatus { get; set; }
    }

    public class CreateSupplierContactRequest
    {
        [Required]
        [StringLength(100)]
        public string ContactName { get; set; } = string.Empty;

        [StringLength(50)]
        public string? ContactTitle { get; set; }

        [StringLength(50)]
        public string? Department { get; set; }

        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        [StringLength(20)]
        public string? MobileNumber { get; set; }

        [StringLength(100)]
        public string? Email { get; set; }

        public bool IsPrimary { get; set; } = false;
        public bool IsOrderContact { get; set; } = false;
        public bool IsBillingContact { get; set; } = false;
        public bool IsTechnicalContact { get; set; } = false;

        [StringLength(500)]
        public string? Notes { get; set; }
    }

    public class CreateSupplierProductRequest
    {
        public int? ProductId { get; set; }
        public int? InventoryItemId { get; set; }

        [StringLength(100)]
        public string? SupplierProductCode { get; set; }

        [StringLength(200)]
        public string? SupplierProductName { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal UnitCost { get; set; }

        [StringLength(10)]
        public string? Currency { get; set; }

        public int MinimumOrderQuantity { get; set; } = 1;
        public int? MaximumOrderQuantity { get; set; }
        public int OrderMultiples { get; set; } = 1;

        [Column(TypeName = "decimal(10,2)")]
        public decimal MinimumOrderValue { get; set; } = 0.00m;

        public bool? IsAvailable { get; set; }
        public int? LeadTimeDays { get; set; }

        [StringLength(20)]
        public string? QualityGrade { get; set; }

        [StringLength(100)]
        public string? BatchNumber { get; set; }

        public DateTime? ManufactureDate { get; set; }
        public DateTime? ExpiryDate { get; set; }

        [StringLength(200)]
        public string? StorageRequirements { get; set; }

        [StringLength(100)]
        public string? SupplierCatalogNumber { get; set; }

        [StringLength(100)]
        public string? SupplierBarcode { get; set; }

        [StringLength(200)]
        public string? PackagingInformation { get; set; }

        [Column(TypeName = "decimal(10,3)")]
        public decimal? WeightPerUnit { get; set; }

        [StringLength(50)]
        public string? Dimensions { get; set; }
    }
}

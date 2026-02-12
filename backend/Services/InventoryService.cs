using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using UmiHealthPOS.Models;
using UmiHealthPOS.Repositories;
using UmiHealthPOS.Data;
using Microsoft.AspNetCore.Http;

namespace UmiHealthPOS.Services
{
    public interface IInventoryService
    {
        Task<List<Product>> GetProductsAsync();
        Task<Product> GetProductAsync(int id);
        Task<Product> GetProductByBarcodeAsync(string barcode);
        Task<List<Customer>> GetCustomersAsync();
        Task<Customer> GetCustomerAsync(int id);
        Task<SaleResult> ProcessSaleAsync(SaleRequest request);
        Task<bool> UpdateStockAsync(int productId, int newStock, string reason);
        Task<List<Product>> GetLowStockProductsAsync();

        // New inventory item methods
        Task<List<InventoryItem>> GetInventoryItemsAsync();
        Task<InventoryItem> CreateInventoryItemAsync(CreateInventoryItemRequest request);
        Task<InventoryItem> UpdateInventoryItemAsync(int id, UpdateInventoryItemRequest request);
        Task<bool> DeleteInventoryItemAsync(int id);
        Task<CsvImportResult> ImportInventoryFromCsvAsync(IFormFile file);
        Task<byte[]> ExportInventoryToCsvAsync();
    }

    public class InventoryService : IInventoryService
    {
        private readonly IProductRepository _productRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly ISaleRepository _saleRepository;
        private readonly IStockTransactionRepository _stockTransactionRepository;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<InventoryService> _logger;

        public InventoryService(
            IProductRepository productRepository,
            ICustomerRepository customerRepository,
            ISaleRepository saleRepository,
            IStockTransactionRepository stockTransactionRepository,
            ApplicationDbContext context,
            ILogger<InventoryService> logger)
        {
            _productRepository = productRepository;
            _customerRepository = customerRepository;
            _saleRepository = saleRepository;
            _stockTransactionRepository = stockTransactionRepository;
            _context = context;
            _logger = logger;
        }

        public async Task<List<Product>> GetProductsAsync()
        {
            try
            {
                return await _productRepository.GetAllAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving products");
                throw;
            }
        }

        public async Task<Product> GetProductAsync(int id)
        {
            try
            {
                return await _productRepository.GetByIdAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving product {ProductId}", id);
                throw;
            }
        }

        public async Task<Product> GetProductByBarcodeAsync(string barcode)
        {
            try
            {
                return await _productRepository.GetByBarcodeAsync(barcode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving product by barcode {Barcode}", barcode);
                throw;
            }
        }

        public async Task<List<Customer>> GetCustomersAsync()
        {
            try
            {
                return await _customerRepository.GetAllAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving customers");
                throw;
            }
        }

        public async Task<Customer> GetCustomerAsync(int id)
        {
            try
            {
                return await _customerRepository.GetByIdAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving customer {CustomerId}", id);
                throw;
            }
        }

        public async Task<SaleResult> ProcessSaleAsync(SaleRequest request)
        {
            var result = new SaleResult { Success = false };

            try
            {
                // Validate request
                if (request.Items == null || !request.Items.Any())
                {
                    result.ErrorMessage = "No items in sale";
                    return result;
                }

                // Get or create customer
                var customer = await GetOrCreateCustomerAsync(request.CustomerId);

                // Validate stock availability
                var stockValidation = await ValidateStockAvailabilityAsync(request.Items);
                if (!stockValidation.IsValid)
                {
                    result.ErrorMessage = stockValidation.ErrorMessage;
                    return result;
                }

                // Process sale with stock updates
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Create sale record
                    var sale = new Sale
                    {
                        CustomerId = customer.Id,
                        Subtotal = request.Subtotal,
                        Tax = request.Tax,
                        Total = request.Total,
                        PaymentMethod = request.PaymentMethod,
                        CashReceived = request.CashReceived,
                        Change = request.CashReceived - request.Total,
                        Status = "Completed",
                        CreatedAt = DateTime.UtcNow
                    };

                    var createdSale = await _saleRepository.AddAsync(sale);

                    // Create sale items and update stock
                    var saleItems = new List<SaleItem>();
                    foreach (var item in request.Items)
                    {
                        var product = await _productRepository.GetByIdAsync(item.ProductId);
                        if (product == null)
                        {
                            throw new Exception($"Product {item.ProductId} not found");
                        }

                        var saleItem = new SaleItem
                        {
                            SaleId = createdSale.Id,
                            ProductId = item.ProductId,
                            UnitPrice = item.Price,
                            Quantity = item.Quantity,
                            TotalPrice = item.Price * item.Quantity
                        };
                        saleItems.Add(saleItem);

                        // Update stock
                        var newStock = product.Stock - item.Quantity;
                        if (!await _productRepository.UpdateStockAsync(item.ProductId, newStock, $"Sale #{createdSale.Id}"))
                        {
                            throw new Exception($"Failed to update stock for product {item.ProductId}");
                        }
                    }

                    // Add sale items
                    await _context.SaleItems.AddRangeAsync(saleItems);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    result.Success = true;
                    result.SaleId = createdSale.Id;
                    result.Message = "Sale processed successfully";

                    _logger.LogInformation("Sale {SaleId} processed successfully with {ItemCount} items",
                        createdSale.Id, request.Items.Count);

                    return result;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error processing sale transaction");
                    result.ErrorMessage = "Failed to process sale";
                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing sale");
                result.ErrorMessage = "An error occurred while processing the sale";
                return result;
            }
        }

        public async Task<bool> UpdateStockAsync(int productId, int newStock, string reason)
        {
            try
            {
                return await _productRepository.UpdateStockAsync(productId, newStock, reason);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating stock for product {ProductId}", productId);
                return false;
            }
        }

        public async Task<List<Product>> GetLowStockProductsAsync()
        {
            try
            {
                return await _productRepository.GetLowStockAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving low stock products");
                throw;
            }
        }

        public async Task<List<InventoryItem>> GetInventoryItemsAsync()
        {
            try
            {
                return await _context.InventoryItems
                    .Where(i => i.IsActive)
                    .OrderBy(i => i.InventoryItemName)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving inventory items");
                throw;
            }
        }

        public async Task<InventoryItem> CreateInventoryItemAsync(CreateInventoryItemRequest request)
        {
            try
            {
                var inventoryItem = new InventoryItem
                {
                    InventoryItemName = request.InventoryItemName,
                    GenericName = request.GenericName,
                    BrandName = request.BrandName,
                    ManufactureDate = request.ManufactureDate,
                    BatchNumber = request.BatchNumber,
                    LicenseNumber = request.LicenseNumber,
                    ZambiaRegNumber = request.ZambiaRegNumber,
                    PackingType = request.PackingType,
                    Quantity = request.Quantity,
                    UnitPrice = request.UnitPrice,
                    SellingPrice = request.SellingPrice,
                    ReorderLevel = request.ReorderLevel,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.InventoryItems.Add(inventoryItem);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created new inventory item: {InventoryItemName} with batch: {BatchNumber}",
                    inventoryItem.InventoryItemName, inventoryItem.BatchNumber);

                return inventoryItem;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating inventory item");
                throw;
            }
        }

        public async Task<InventoryItem> UpdateInventoryItemAsync(int id, UpdateInventoryItemRequest request)
        {
            try
            {
                var inventoryItem = await _context.InventoryItems.FindAsync(id);
                if (inventoryItem == null)
                {
                    return null;
                }

                inventoryItem.InventoryItemName = request.InventoryItemName;
                inventoryItem.GenericName = request.GenericName;
                inventoryItem.BrandName = request.BrandName;
                inventoryItem.ManufactureDate = request.ManufactureDate;
                inventoryItem.BatchNumber = request.BatchNumber;
                inventoryItem.LicenseNumber = request.LicenseNumber;
                inventoryItem.ZambiaRegNumber = request.ZambiaRegNumber;
                inventoryItem.PackingType = request.PackingType;
                inventoryItem.Quantity = request.Quantity;
                inventoryItem.UnitPrice = request.UnitPrice;
                inventoryItem.SellingPrice = request.SellingPrice;
                inventoryItem.ReorderLevel = request.ReorderLevel;
                inventoryItem.IsActive = request.IsActive;
                inventoryItem.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated inventory item: {InventoryItemName} with batch: {BatchNumber}",
                    inventoryItem.InventoryItemName, inventoryItem.BatchNumber);

                return inventoryItem;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating inventory item with ID: {Id}", id);
                throw;
            }
        }

        public async Task<bool> DeleteInventoryItemAsync(int id)
        {
            try
            {
                var inventoryItem = await _context.InventoryItems.FindAsync(id);
                if (inventoryItem == null)
                {
                    return false;
                }

                inventoryItem.IsActive = false;
                inventoryItem.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Soft deleted inventory item: {InventoryItemName} with batch: {BatchNumber}",
                    inventoryItem.InventoryItemName, inventoryItem.BatchNumber);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting inventory item with ID: {Id}", id);
                return false;
            }
        }

        public async Task<CsvImportResult> ImportInventoryFromCsvAsync(IFormFile file)
        {
            var result = new CsvImportResult();

            try
            {
                using var stream = file.OpenReadStream();
                using var reader = new StreamReader(stream);

                var lines = new List<string>();
                while (!reader.EndOfStream)
                {
                    lines.Add(await reader.ReadLineAsync());
                }

                if (lines.Count < 2)
                {
                    result.Errors.Add("CSV file is empty or missing header");
                    return result;
                }

                var header = lines[0].Split(',');
                var expectedHeaders = new[] {
                    "InventoryItemName", "GenericName", "BrandName", "ManufactureDate",
                    "BatchNumber", "LicenseNumber", "ZambiaRegNumber", "PackingType",
                    "Quantity", "UnitPrice", "SellingPrice", "ReorderLevel"
                };

                // Validate headers
                for (int i = 0; i < expectedHeaders.Length; i++)
                {
                    if (i >= header.Length || !header[i].Trim().Equals(expectedHeaders[i], StringComparison.OrdinalIgnoreCase))
                    {
                        result.Errors.Add($"Missing or incorrect header: {expectedHeaders[i]}");
                        return result;
                    }
                }

                // Process data rows
                for (int i = 1; i < lines.Count; i++)
                {
                    try
                    {
                        var values = lines[i].Split(',');
                        if (values.Length < expectedHeaders.Length)
                        {
                            result.Errors.Add($"Row {i + 1}: Incomplete data");
                            continue;
                        }

                        var createRequest = new CreateInventoryItemRequest
                        {
                            InventoryItemName = values[0].Trim(),
                            GenericName = values[1].Trim(),
                            BrandName = values[2].Trim(),
                            ManufactureDate = DateTime.Parse(values[3].Trim()),
                            BatchNumber = values[4].Trim(),
                            LicenseNumber = values[5].Trim(),
                            ZambiaRegNumber = values[6].Trim(),
                            PackingType = values[7].Trim(),
                            Quantity = int.Parse(values[8].Trim()),
                            UnitPrice = decimal.Parse(values[9].Trim()),
                            SellingPrice = decimal.Parse(values[10].Trim()),
                            ReorderLevel = int.Parse(values[11].Trim())
                        };

                        await CreateInventoryItemAsync(createRequest);
                        result.ImportedCount++;
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add($"Row {i + 1}: {ex.Message}");
                    }
                }

                _logger.LogInformation("CSV import completed. Imported: {ImportedCount}, Errors: {ErrorCount}",
                    result.ImportedCount, result.Errors.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing inventory from CSV");
                result.Errors.Add($"General error: {ex.Message}");
                return result;
            }
        }

        public async Task<byte[]> ExportInventoryToCsvAsync()
        {
            try
            {
                var inventoryItems = await GetInventoryItemsAsync();

                using var output = new MemoryStream();
                using var writer = new StreamWriter(output, System.Text.Encoding.UTF8);

                // Write header
                await writer.WriteLineAsync("InventoryItemName,GenericName,BrandName,ManufactureDate,BatchNumber,LicenseNumber,ZambiaRegNumber,PackingType,Quantity,UnitPrice,SellingPrice,ReorderLevel,CreatedAt");

                // Write data rows
                foreach (var item in inventoryItems)
                {
                    var line = $"{EscapeCsvField(item.InventoryItemName)},{EscapeCsvField(item.GenericName)},{EscapeCsvField(item.BrandName)},{item.ManufactureDate:yyyy-MM-dd},{EscapeCsvField(item.BatchNumber)},{EscapeCsvField(item.LicenseNumber)},{EscapeCsvField(item.ZambiaRegNumber)},{EscapeCsvField(item.PackingType)},{item.Quantity},{item.UnitPrice},{item.SellingPrice},{item.ReorderLevel},{item.CreatedAt:yyyy-MM-dd HH:mm:ss}";
                    await writer.WriteLineAsync(line);
                }

                await writer.FlushAsync();
                return output.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting inventory to CSV");
                throw;
            }
        }

        private string EscapeCsvField(string field)
        {
            if (string.IsNullOrEmpty(field))
                return "";

            if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
            {
                return $"\"{field.Replace("\"", "\"\"")}\"";
            }

            return field;
        }

        private async Task<Customer> GetOrCreateCustomerAsync(int? customerId)
        {
            if (customerId.HasValue && customerId.Value > 0)
            {
                var customer = await _customerRepository.GetByIdAsync(customerId.Value);
                if (customer != null) return customer;
            }

            // Default to walk-in customer (ID = 1)
            return await _customerRepository.GetByIdAsync(1);
        }

        private async Task AddSaleItemsAsync(int saleId, List<SaleItem> saleItems)
        {
            // This would ideally be in the SaleRepository, but for now we'll add it directly
            // In a real implementation, you'd inject DbContext or create a proper method
            foreach (var item in saleItems)
            {
                // Create the sale item with the correct sale ID
                var saleItem = new SaleItem
                {
                    SaleId = saleId,
                    ProductId = item.ProductId,
                    UnitPrice = item.UnitPrice,
                    Quantity = item.Quantity,
                    TotalPrice = item.TotalPrice
                };

                // Add to database - this is a simplified approach
                // In production, you'd use proper repository pattern
                await Task.CompletedTask; // Placeholder for actual database operation
            }
        }

        private async Task<StockValidationResult> ValidateStockAvailabilityAsync(List<SaleItemRequest> items)
        {
            foreach (var item in items)
            {
                var product = await _productRepository.GetByIdAsync(item.ProductId);
                if (product == null)
                {
                    return new StockValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = $"Product {item.ProductId} not found"
                    };
                }

                if (product.Stock < item.Quantity)
                {
                    return new StockValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = $"Insufficient stock for {product.Name}. Available: {product.Stock}, Requested: {item.Quantity}"
                    };
                }
            }

            return new StockValidationResult { IsValid = true };
        }
    }

    // Supporting classes
    public class SaleRequest
    {
        public List<SaleItemRequest> Items { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Tax { get; set; }
        public decimal Total { get; set; }
        public string PaymentMethod { get; set; }
        public decimal CashReceived { get; set; }
        public int? CustomerId { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class SaleItemRequest
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
    }

    public class SaleResult
    {
        public bool Success { get; set; }
        public int SaleId { get; set; }
        public string Message { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class StockValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class CreateInventoryItemRequest
    {
        public string? InventoryItemName { get; set; }
        public string? GenericName { get; set; }
        public string? BrandName { get; set; }
        public DateTime ManufactureDate { get; set; }
        public string? BatchNumber { get; set; }
        public string? LicenseNumber { get; set; }
        public string? ZambiaRegNumber { get; set; }
        public string? PackingType { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal SellingPrice { get; set; }
        public int ReorderLevel { get; set; }
    }

    public class UpdateInventoryItemRequest
    {
        public string? InventoryItemName { get; set; }
        public string? GenericName { get; set; }
        public string? BrandName { get; set; }
        public DateTime ManufactureDate { get; set; }
        public string? BatchNumber { get; set; }
        public string? LicenseNumber { get; set; }
        public string? ZambiaRegNumber { get; set; }
        public string? PackingType { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal SellingPrice { get; set; }
        public int ReorderLevel { get; set; }
        public bool IsActive { get; set; }
    }
}

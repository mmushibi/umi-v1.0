using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using UmiHealthPOS.Models;
using UmiHealthPOS.Data;
using UmiHealthPOS.DTOs;
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
        private readonly ApplicationDbContext _context;
        private readonly ILogger<InventoryService> _logger;

        public InventoryService(
            ApplicationDbContext context,
            ILogger<InventoryService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<Product>> GetProductsAsync()
        {
            try
            {
                return await _context.Products
                    .Where(p => p.Status == "Active")
                    .OrderBy(p => p.Name)
                    .ToListAsync();
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
                return await _context.Products
                    .FirstOrDefaultAsync(p => p.Id == id && p.Status == "Active");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving product with ID: {Id}", id);
                throw;
            }
        }

        public async Task<Product> GetProductByBarcodeAsync(string barcode)
        {
            try
            {
                // Product doesn't have Barcode property, search by Name or other identifier
                return await _context.Products
                    .FirstOrDefaultAsync(p => p.Status == "Active" &&
                                           (p.Name.Contains(barcode) ||
                                            (p.BrandName != null && p.BrandName.Contains(barcode)) ||
                                            (p.GenericName != null && p.GenericName.Contains(barcode))));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving product by barcode: {Barcode}", barcode);
                throw;
            }
        }

        public async Task<List<Customer>> GetCustomersAsync()
        {
            try
            {
                return await _context.Customers
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.Name)
                    .ToListAsync();
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
                return await _context.Customers
                    .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving customer with ID: {Id}", id);
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
                        Subtotal = request.Items.Sum(item => item.UnitPrice * item.Quantity),
                        Tax = 0, // Calculate tax if needed
                        Total = request.Items.Sum(item => item.UnitPrice * item.Quantity) - request.DiscountAmount,
                        PaymentMethod = request.PaymentMethod,
                        CashReceived = request.Items.Sum(item => item.UnitPrice * item.Quantity), // This should be calculated or passed separately
                        Change = request.Items.Sum(item => item.UnitPrice * item.Quantity) - (request.Items.Sum(item => item.UnitPrice * item.Quantity) - request.DiscountAmount),
                        Status = "Completed",
                        CreatedAt = DateTime.UtcNow
                    };

                    var createdSale = await _context.Sales.AddAsync(sale);
                    await _context.SaveChangesAsync();

                    // Create sale items and update stock
                    var saleItems = new List<SaleItem>();
                    foreach (var item in request.Items)
                    {
                        var product = await _context.Products.FindAsync(item.ProductId);
                        if (product == null)
                        {
                            throw new Exception($"Product {item.ProductId} not found");
                        }

                        var saleItem = new SaleItem
                        {
                            SaleId = createdSale.Entity.Id,
                            ProductId = item.ProductId,
                            UnitPrice = item.UnitPrice,
                            Quantity = item.Quantity,
                            TotalPrice = item.UnitPrice * item.Quantity
                        };
                        saleItems.Add(saleItem);

                        // Update stock
                        var newStock = product.Stock - item.Quantity;
                        product.Stock = newStock;
                        _context.Products.Update(product);
                    }

                    // Add sale items
                    await _context.SaleItems.AddRangeAsync(saleItems);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    result.Success = true;
                    result.SaleId = createdSale.Entity.Id;
                    result.Message = "Sale processed successfully";

                    _logger.LogInformation("Sale {SaleId} processed successfully with {ItemCount} items",
                        createdSale.Entity.Id, request.Items.Count);

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
                var product = await _context.Products.FindAsync(productId);
                if (product == null)
                {
                    return false;
                }

                product.Stock = newStock;
                _context.Products.Update(product);
                await _context.SaveChangesAsync();

                return true;
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
                return await _context.Products
                    .Where(p => p.Status == "Active" && p.Stock <= p.ReorderLevel)
                    .OrderBy(p => p.Name)
                    .ToListAsync();
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
                    .OrderBy(i => i.Name)
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
                    Name = request.Name,
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
                    inventoryItem.Name, inventoryItem.BatchNumber);

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

                inventoryItem.Name = request.Name;
                inventoryItem.GenericName = request.GenericName;
                inventoryItem.BrandName = request.BrandName;
                inventoryItem.ManufactureDate = request.ManufactureDate;
                inventoryItem.BatchNumber = request.BatchNumber;
                inventoryItem.LicenseNumber = request.LicenseNumber;
                inventoryItem.ZambiaRegNumber = request.ZambiaRegNumber;
                inventoryItem.PackingType = request.PackingType;
                inventoryItem.Quantity = request.Quantity ?? inventoryItem.Quantity;
                inventoryItem.UnitPrice = request.UnitPrice ?? inventoryItem.UnitPrice;
                inventoryItem.SellingPrice = request.SellingPrice ?? inventoryItem.SellingPrice;
                inventoryItem.ReorderLevel = request.ReorderLevel ?? inventoryItem.ReorderLevel;
                inventoryItem.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated inventory item: {InventoryItemName} with batch: {BatchNumber}",
                    inventoryItem.Name, inventoryItem.BatchNumber);

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
                    inventoryItem.Name, inventoryItem.BatchNumber);

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
                    "Name", "GenericName", "BrandName", "ManufactureDate",
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
                            Name = values[0].Trim(),
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
                        result.SuccessCount++;
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add($"Row {i + 1}: {ex.Message}");
                    }
                }

                _logger.LogInformation("CSV import completed. Imported: {ImportedCount}, Errors: {ErrorCount}",
                    result.SuccessCount, result.Errors.Count);

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
                await writer.WriteLineAsync("Name,GenericName,BrandName,ManufactureDate,BatchNumber,LicenseNumber,ZambiaRegNumber,PackingType,Quantity,UnitPrice,SellingPrice,ReorderLevel,CreatedAt");

                // Write data rows
                foreach (var item in inventoryItems)
                {
                    var line = $"{EscapeCsvField(item.Name)},{EscapeCsvField(item.GenericName)},{EscapeCsvField(item.BrandName)},{item.ManufactureDate:yyyy-MM-dd},{EscapeCsvField(item.BatchNumber)},{EscapeCsvField(item.LicenseNumber)},{EscapeCsvField(item.ZambiaRegNumber)},{EscapeCsvField(item.PackingType)},{item.Quantity},{item.UnitPrice},{item.SellingPrice},{item.ReorderLevel},{item.CreatedAt:yyyy-MM-dd HH:mm:ss}";
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

        private async Task AddSaleItemsAsync(int saleId, List<SaleItem> saleItems)
        {
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

                // Add to database using proper Entity Framework operations
                _context.SaleItems.Add(saleItem);
            }

            // Save all changes at once for better performance
            await _context.SaveChangesAsync();
        }

        private async Task<Customer> GetOrCreateCustomerAsync(int? customerId)
        {
            if (customerId.HasValue)
            {
                var customer = await _context.Customers.FindAsync(customerId.Value);
                if (customer != null && customer.IsActive)
                {
                    return customer;
                }
            }

            // Create a new customer for walk-in sales
            var newCustomer = new Customer
            {
                Name = "Walk-in Customer",
                Phone = "0000000000",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Customers.Add(newCustomer);
            await _context.SaveChangesAsync();

            return newCustomer;
        }

        private async Task<StockValidationResult> ValidateStockAvailabilityAsync(List<SaleItemRequest> items)
        {
            foreach (var item in items)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
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
}

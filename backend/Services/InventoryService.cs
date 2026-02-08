using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using UmiHealthPOS.Models;
using UmiHealthPOS.Repositories;
using UmiHealthPOS.Data;

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
}

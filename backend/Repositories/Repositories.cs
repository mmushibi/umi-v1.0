using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using UmiHealthPOS.Data;
using UmiHealthPOS.Models;

namespace UmiHealthPOS.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IStockTransactionRepository _stockTransactionRepository;

        public ProductRepository(ApplicationDbContext context, IStockTransactionRepository stockTransactionRepository)
        {
            _context = context;
            _stockTransactionRepository = stockTransactionRepository;
        }

        public async Task<List<Product>> GetAllAsync()
        {
            return await _context.Products
                .Where(p => p.IsActive)
                .OrderBy(p => p.Name)
                .ToListAsync();
        }

        public async Task<Product> GetByIdAsync(int id)
        {
            return await _context.Products
                .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);
        }

        public async Task<Product> GetByBarcodeAsync(string barcode)
        {
            return await _context.Products
                .FirstOrDefaultAsync(p => p.Barcode == barcode && p.IsActive);
        }

        public async Task<Product> AddAsync(Product product)
        {
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            return product;
        }

        public async Task<Product> UpdateAsync(Product product)
        {
            product.UpdatedAt = DateTime.UtcNow;
            _context.Products.Update(product);
            await _context.SaveChangesAsync();
            return product;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var product = await GetByIdAsync(id);
            if (product == null) return false;

            product.IsActive = false;
            product.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateStockAsync(int productId, int newStock, string reason)
        {
            var product = await GetByIdAsync(productId);
            if (product == null) return false;

            var oldStock = product.Stock;
            product.Stock = newStock;
            product.UpdatedAt = DateTime.UtcNow;

            // Create stock transaction
            var transaction = new StockTransaction
            {
                ProductId = productId,
                TransactionType = "Sale",
                QuantityChange = newStock - oldStock,
                PreviousStock = oldStock,
                NewStock = newStock,
                Reason = reason,
                CreatedAt = DateTime.UtcNow
            };

            using var transactionScope = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.Products.Update(product);
                await _stockTransactionRepository.AddAsync(transaction);
                await _context.SaveChangesAsync();
                await transactionScope.CommitAsync();
                return true;
            }
            catch
            {
                await transactionScope.RollbackAsync();
                return false;
            }
        }

        public async Task<List<Product>> GetLowStockAsync()
        {
            return await _context.Products
                .Where(p => p.IsActive && p.Stock <= p.MinStock)
                .OrderBy(p => p.Stock)
                .ToListAsync();
        }
    }

    public class CustomerRepository : ICustomerRepository
    {
        private readonly ApplicationDbContext _context;

        public CustomerRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Customer>> GetAllAsync()
        {
            return await _context.Customers
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<Customer> GetByIdAsync(int id)
        {
            return await _context.Customers
                .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);
        }

        public async Task<Customer> AddAsync(Customer customer)
        {
            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();
            return customer;
        }

        public async Task<Customer> UpdateAsync(Customer customer)
        {
            customer.UpdatedAt = DateTime.UtcNow;
            _context.Customers.Update(customer);
            await _context.SaveChangesAsync();
            return customer;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var customer = await GetByIdAsync(id);
            if (customer == null) return false;

            customer.IsActive = false;
            customer.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }
    }

    public class SaleRepository : ISaleRepository
    {
        private readonly ApplicationDbContext _context;

        public SaleRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public ApplicationDbContext Context => _context;

        public async Task<Sale> AddAsync(Sale sale)
        {
            _context.Sales.Add(sale);
            await _context.SaveChangesAsync();
            return sale;
        }

        public async Task<Sale> GetByIdAsync(int id)
        {
            return await _context.Sales
                .Include(s => s.Customer)
                .Include(s => s.SaleItems)
                .ThenInclude(si => si.Product)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<List<Sale>> GetByCustomerIdAsync(int customerId)
        {
            return await _context.Sales
                .Include(s => s.Customer)
                .Include(s => s.SaleItems)
                .ThenInclude(si => si.Product)
                .Where(s => s.CustomerId == customerId)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Sale>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.Sales
                .Include(s => s.Customer)
                .Include(s => s.SaleItems)
                .ThenInclude(si => si.Product)
                .Where(s => s.CreatedAt >= startDate && s.CreatedAt <= endDate)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }
    }

    public class StockTransactionRepository : IStockTransactionRepository
    {
        private readonly ApplicationDbContext _context;

        public StockTransactionRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<StockTransaction> AddAsync(StockTransaction transaction)
        {
            _context.StockTransactions.Add(transaction);
            await _context.SaveChangesAsync();
            return transaction;
        }

        public async Task<List<StockTransaction>> GetByProductIdAsync(int productId)
        {
            return await _context.StockTransactions
                .Include(st => st.Product)
                .Where(st => st.ProductId == productId)
                .OrderByDescending(st => st.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<StockTransaction>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.StockTransactions
                .Include(st => st.Product)
                .Where(st => st.CreatedAt >= startDate && st.CreatedAt <= endDate)
                .OrderByDescending(st => st.CreatedAt)
                .ToListAsync();
        }
    }
}

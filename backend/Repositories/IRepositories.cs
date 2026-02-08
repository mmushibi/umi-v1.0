using System.Collections.Generic;
using System.Threading.Tasks;
using UmiHealthPOS.Models;

namespace UmiHealthPOS.Repositories
{
    public interface IProductRepository
    {
        Task<List<Product>> GetAllAsync();
        Task<Product> GetByIdAsync(int id);
        Task<Product> GetByBarcodeAsync(string barcode);
        Task<Product> AddAsync(Product product);
        Task<Product> UpdateAsync(Product product);
        Task<bool> DeleteAsync(int id);
        Task<bool> UpdateStockAsync(int productId, int newStock, string reason);
        Task<List<Product>> GetLowStockAsync();
    }

    public interface ICustomerRepository
    {
        Task<List<Customer>> GetAllAsync();
        Task<Customer> GetByIdAsync(int id);
        Task<Customer> AddAsync(Customer customer);
        Task<Customer> UpdateAsync(Customer customer);
        Task<bool> DeleteAsync(int id);
    }

    public interface ISaleRepository
    {
        Task<Sale> AddAsync(Sale sale);
        Task<Sale> GetByIdAsync(int id);
        Task<List<Sale>> GetByCustomerIdAsync(int customerId);
        Task<List<Sale>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
    }

    public interface IStockTransactionRepository
    {
        Task<StockTransaction> AddAsync(StockTransaction transaction);
        Task<List<StockTransaction>> GetByProductIdAsync(int productId);
        Task<List<StockTransaction>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
    }
}

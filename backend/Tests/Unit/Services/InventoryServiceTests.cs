using Microsoft.EntityFrameworkCore;
using FluentAssertions;
using UmiHealthPOS.Data;
using UmiHealthPOS.Models;
using UmiHealthPOS.Services;
using UmiHealthPOS.Repositories;
using Microsoft.Extensions.Logging;
using Moq;

namespace UmiHealthPOS.Tests.Unit.Services;

public class InventoryServiceTests
{
    private readonly ApplicationDbContext _dbContext;
    private readonly Mock<IProductRepository> _mockProductRepository;
    private readonly Mock<ICustomerRepository> _mockCustomerRepository;
    private readonly Mock<ISaleRepository> _mockSaleRepository;
    private readonly Mock<IStockTransactionRepository> _mockStockTransactionRepository;
    private readonly Mock<ILogger<InventoryService>> _mockLogger;
    private readonly InventoryService _service;

    public InventoryServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemory(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _mockProductRepository = new Mock<IProductRepository>();
        _mockCustomerRepository = new Mock<ICustomerRepository>();
        _mockSaleRepository = new Mock<ISaleRepository>();
        _mockStockTransactionRepository = new Mock<IStockTransactionRepository>();
        _mockLogger = new Mock<ILogger<InventoryService>>();

        _service = new InventoryService(
            _mockProductRepository.Object,
            _mockCustomerRepository.Object,
            _mockSaleRepository.Object,
            _mockStockTransactionRepository.Object,
            _dbContext,
            _mockLogger.Object);
    }

    [Fact]
    public async Task GetInventoryItemsAsync_ReturnsAllItems()
    {
        // Arrange
        await SeedTestData();

        // Act
        var result = await _service.GetInventoryItemsAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(i => i.InventoryItemName == "Test Item 1");
        result.Should().Contain(i => i.InventoryItemName == "Test Item 2");
    }

    [Fact]
    public async Task CreateInventoryItemAsync_CreatesItemSuccessfully()
    {
        // Arrange
        var createRequest = new CreateInventoryItemRequest
        {
            InventoryItemName = "New Item",
            GenericName = "Generic",
            BrandName = "Brand",
            ManufactureDate = DateTime.Today,
            BatchNumber = "B001",
            LicenseNumber = "L001",
            ZambiaRegNumber = "Z001",
            PackingType = "Box",
            Quantity = 10,
            UnitPrice = 5.99m,
            SellingPrice = 7.99m,
            ReorderLevel = 5
        };

        // Act
        var result = await _service.CreateInventoryItemAsync(createRequest);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.InventoryItemName.Should().Be("New Item");

        var savedItem = await _dbContext.InventoryItems.FindAsync(result.Id);
        savedItem.Should().NotBeNull();
        savedItem!.InventoryItemName.Should().Be("New Item");
    }

    [Fact]
    public async Task UpdateInventoryItemAsync_UpdatesItemSuccessfully()
    {
        // Arrange
        await SeedTestData();
        var existingItem = await _dbContext.InventoryItems.FirstAsync();

        var updateRequest = new UpdateInventoryItemRequest
        {
            InventoryItemName = "Updated Item",
            GenericName = "Updated Generic",
            BrandName = "Updated Brand",
            ManufactureDate = DateTime.Today,
            BatchNumber = "B002",
            LicenseNumber = "L002",
            ZambiaRegNumber = "Z002",
            PackingType = "Bottle",
            Quantity = 20,
            UnitPrice = 15.99m,
            SellingPrice = 19.99m,
            ReorderLevel = 10,
            IsActive = true
        };

        // Act
        var result = await _service.UpdateInventoryItemAsync(existingItem.Id, updateRequest);

        // Assert
        result.Should().NotBeNull();
        result!.InventoryItemName.Should().Be("Updated Item");
        result.UnitPrice.Should().Be(15.99m);
    }

    [Fact]
    public async Task DeleteInventoryItemAsync_SoftDeletesItemSuccessfully()
    {
        // Arrange
        await SeedTestData();
        var existingItem = await _dbContext.InventoryItems.FirstAsync();

        // Act
        var result = await _service.DeleteInventoryItemAsync(existingItem.Id);

        // Assert
        result.Should().BeTrue();

        var deletedItem = await _dbContext.InventoryItems.FindAsync(existingItem.Id);
        deletedItem.Should().NotBeNull();
        deletedItem!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task ImportInventoryFromCsvAsync_ImportsValidData()
    {
        // Arrange
        var csvContent = "InventoryItemName,GenericName,BrandName,ManufactureDate,BatchNumber,LicenseNumber,ZambiaRegNumber,PackingType,Quantity,UnitPrice,SellingPrice,ReorderLevel\n" +
                        "Test CSV Item,Generic CSV,Brand CSV,2024-01-01,CSV001,CSV001,CSV001,Box,10,5.99,7.99,5";

        var file = CreateMockCsvFile(csvContent);

        // Act
        var result = await _service.ImportInventoryFromCsvAsync(file);

        // Assert
        result.ImportedCount.Should().Be(1);
        result.Errors.Should().BeEmpty();

        var importedItem = await _dbContext.InventoryItems
            .FirstOrDefaultAsync(i => i.InventoryItemName == "Test CSV Item");
        importedItem.Should().NotBeNull();
        importedItem!.BatchNumber.Should().Be("CSV001");
    }

    [Fact]
    public async Task ExportInventoryToCsvAsync_ReturnsValidCsv()
    {
        // Arrange
        await SeedTestData();

        // Act
        var result = await _service.ExportInventoryToCsvAsync();

        // Assert
        result.Should().NotBeNull();
        result.Length.Should().BeGreaterThan(0);

        var csvContent = System.Text.Encoding.UTF8.GetString(result);
        csvContent.Should().Contain("InventoryItemName,GenericName,BrandName");
        csvContent.Should().Contain("Test Item 1");
        csvContent.Should().Contain("Test Item 2");
    }

    private async Task SeedTestData()
    {
        var items = new List<InventoryItem>
        {
            new()
            {
                Id = 1,
                InventoryItemName = "Test Item 1",
                GenericName = "Generic 1",
                BrandName = "Brand 1",
                ManufactureDate = DateTime.Today,
                BatchNumber = "B001",
                LicenseNumber = "L001",
                ZambiaRegNumber = "Z001",
                PackingType = "Box",
                Quantity = 10,
                UnitPrice = 5.99m,
                SellingPrice = 7.99m,
                ReorderLevel = 5,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = 2,
                InventoryItemName = "Test Item 2",
                GenericName = "Generic 2",
                BrandName = "Brand 2",
                ManufactureDate = DateTime.Today,
                BatchNumber = "B002",
                LicenseNumber = "L002",
                ZambiaRegNumber = "Z002",
                PackingType = "Bottle",
                Quantity = 20,
                UnitPrice = 10.99m,
                SellingPrice = 15.99m,
                ReorderLevel = 10,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        await _dbContext.InventoryItems.AddRangeAsync(items);
        await _dbContext.SaveChangesAsync();
    }

    private IFormFile CreateMockCsvFile(string content)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(content);
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "data", "test.csv")
        {
            Headers = new HeaderDictionary(),
            ContentType = "text/csv"
        };
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using UmiHealthPOS.Controllers.Api;
using UmiHealthPOS.Data;
using UmiHealthPOS.Models;
using UmiHealthPOS.Services;
using UmiHealthPOS.Repositories;

namespace UmiHealthPOS.Tests.Unit.Controllers;

public class TenantAdminControllerTests
{
    private readonly Mock<ILogger<TenantAdminController>> _mockLogger;
    private readonly Mock<IInventoryService> _mockInventoryService;
    private readonly ApplicationDbContext _dbContext;
    private readonly TenantAdminController _controller;

    public TenantAdminControllerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemory(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _mockLogger = new Mock<ILogger<TenantAdminController>>();
        _mockInventoryService = new Mock<IInventoryService>();

        _controller = new TenantAdminController(_mockLogger.Object, _dbContext, _mockInventoryService.Object);
    }

    [Fact]
    public async Task GetInventoryItems_ReturnsOkResult_WithItems()
    {
        // Arrange
        var items = new List<InventoryItem>
        {
            new() { Id = 1, InventoryItemName = "Test Item", Quantity = 10, UnitPrice = 5.99m }
        };

        _mockInventoryService.Setup(x => x.GetInventoryItemsAsync()).ReturnsAsync(items);

        // Act
        var result = await _controller.GetInventoryItems();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult.Value.Should().BeEquivalentTo(items);
    }

    [Fact]
    public async Task CreateInventoryItem_ReturnsCreatedAtAction_WhenValid()
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
            Quantity = 5,
            UnitPrice = 10.99m,
            SellingPrice = 15.99m,
            ReorderLevel = 5
        };

        var newItem = new InventoryItem
        {
            Id = 1,
            InventoryItemName = "New Item",
            Quantity = 5,
            UnitPrice = 10.99m
        };

        _mockInventoryService.Setup(x => x.CreateInventoryItemAsync(It.IsAny<CreateInventoryItemRequest>()))
            .ReturnsAsync(newItem);

        // Act
        var result = await _controller.CreateInventoryItem(createRequest);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = result as CreatedAtActionResult;
        createdResult.Value.Should().BeEquivalentTo(newItem);
    }

    [Fact]
    public async Task UpdateInventoryItem_ReturnsNoContent_WhenSuccessful()
    {
        // Arrange
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
            Quantity = 15,
            UnitPrice = 12.99m,
            SellingPrice = 18.99m,
            ReorderLevel = 8,
            IsActive = true
        };

        var existingItem = new InventoryItem
        {
            Id = 1,
            InventoryItemName = "Existing Item",
            Quantity = 10,
            UnitPrice = 8.99m
        };

        _mockInventoryService.Setup(x => x.UpdateInventoryItemAsync(It.IsAny<int>(), It.IsAny<UpdateInventoryItemRequest>()))
            .ReturnsAsync(existingItem);

        // Act
        var result = await _controller.UpdateInventoryItem(1, updateRequest);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteInventoryItem_ReturnsNoContent_WhenSuccessful()
    {
        // Arrange
        _mockInventoryService.Setup(x => x.DeleteInventoryItemAsync(It.IsAny<int>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteInventoryItem(1);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task ImportInventoryFromCsv_ReturnsOkResult_WithImportResult()
    {
        // Arrange
        var importResult = new CsvImportResult
        {
            ImportedCount = 10,
            Errors = new List<string>()
        };

        _mockInventoryService.Setup(x => x.ImportInventoryFromCsvAsync(It.IsAny<Microsoft.AspNetCore.Http.IFormFile>()))
            .ReturnsAsync(importResult);

        // Act
        var result = await _controller.ImportInventoryFromCsv(null);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult.Value.Should().BeEquivalentTo(importResult);
    }

    [Fact]
    public async Task ExportInventoryToCsv_ReturnsFileResult()
    {
        // Arrange
        var csvData = new byte[] { 1, 2, 3, 4, 5 };

        _mockInventoryService.Setup(x => x.ExportInventoryToCsvAsync())
            .ReturnsAsync(csvData);

        // Act
        var result = await _controller.ExportInventoryToCsv();

        // Assert
        result.Should().BeOfType<FileContentResult>();
        var fileResult = result as FileContentResult;
        fileResult.FileContents.Should().BeEquivalentTo(csvData);
        fileResult.ContentType.Should().Be("text/csv");
        fileResult.FileDownloadName.Should().Be("inventory-export.csv");
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using FluentAssertions;
using UmiHealthPOS.Controllers.Api;
using UmiHealthPOS.Data;
using UmiHealthPOS.Models;
using UmiHealthPOS.Services;
using UmiHealthPOS.Repositories;
using Microsoft.AspNetCore.Http;

namespace UmiHealthPOS.Tests.Integration.Controllers;

[Trait("Category", "Integration")]
public class TenantAdminControllerIntegrationTests : IClassFixture<TestApplicationFactory>
{
    private readonly TestApplicationFactory _factory;
    private readonly HttpClient _client;

    public TenantAdminControllerIntegrationTests(TestApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetInventoryItems_ReturnsSuccessAndCorrectContentType()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var response = await _client.GetAsync("/api/tenantadmin/inventory/items");

        // Assert
        response.EnsureSuccessStatusCode();
        response.Content.Headers.ContentType.ToString().Should().Be("application/json; charset=utf-8");
    }

    [Fact]
    public async Task CreateInventoryItem_ValidInput_ReturnsCreatedResponse()
    {
        // Arrange
        var createRequest = new CreateInventoryItemRequest
        {
            InventoryItemName = "Integration Test Item",
            GenericName = "Test Generic",
            BrandName = "Test Brand",
            ManufactureDate = DateTime.Today,
            BatchNumber = "INT001",
            LicenseNumber = "INT001",
            ZambiaRegNumber = "INT001",
            PackingType = "Box",
            Quantity = 15,
            UnitPrice = 25.99m,
            SellingPrice = 35.99m,
            ReorderLevel = 8
        };

        var content = JsonContent.Create(createRequest);

        // Act
        var response = await _client.PostAsync("/api/tenantadmin/inventory/items", content);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);

        var createdItem = await response.Content.ReadFromJsonAsync<InventoryItem>();
        createdItem.Should().NotBeNull();
        createdItem!.InventoryItemName.Should().Be("Integration Test Item");
        createdItem.Quantity.Should().Be(15);
    }

    [Fact]
    public async Task UpdateInventoryItem_ValidInput_ReturnsNoContent()
    {
        // Arrange
        var itemId = await SeedTestItemAsync();

        var updateRequest = new UpdateInventoryItemRequest
        {
            InventoryItemName = "Updated Integration Item",
            GenericName = "Updated Generic",
            BrandName = "Updated Brand",
            ManufactureDate = DateTime.Today,
            BatchNumber = "UPD001",
            LicenseNumber = "UPD001",
            ZambiaRegNumber = "UPD001",
            PackingType = "Bottle",
            Quantity = 25,
            UnitPrice = 45.99m,
            SellingPrice = 65.99m,
            ReorderLevel = 12,
            IsActive = true
        };

        var content = JsonContent.Create(updateRequest);

        // Act
        var response = await _client.PutAsync($"/api/tenantadmin/inventory/items/{itemId}", content);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteInventoryItem_ExistingItem_ReturnsNoContent()
    {
        // Arrange
        var itemId = await SeedTestItemAsync();

        // Act
        var response = await _client.DeleteAsync($"/api/tenantadmin/inventory/items/{itemId}");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ImportInventoryFromCsv_ValidFile_ReturnsSuccess()
    {
        // Arrange
        var csvContent = "InventoryItemName,GenericName,BrandName,ManufactureDate,BatchNumber,LicenseNumber,ZambiaRegNumber,PackingType,Quantity,UnitPrice,SellingPrice,ReorderLevel\n" +
                        "Import Test 1,Generic 1,Brand 1,2024-01-01,IMP001,IMP001,IMP001,Box,10,5.99,7.99,5\n" +
                        "Import Test 2,Generic 2,Brand 2,2024-01-01,IMP002,IMP002,IMP002,Bottle,20,10.99,15.99,8";

        var formContent = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes(csvContent));
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/csv");
        formContent.Add(fileContent, "file", "test-import.csv");

        // Act
        var response = await _client.PostAsync("/api/tenantadmin/inventory/import-csv", formContent);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<CsvImportResult>();
        result.Should().NotBeNull();
        result!.ImportedCount.Should().Be(2);
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ExportInventoryToCsv_ReturnsValidCsvFile()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var response = await _client.GetAsync("/api/tenantadmin/inventory/export-csv");

        // Assert
        response.EnsureSuccessStatusCode();
        response.Content.Headers.ContentType.MediaType.Should().Be("text/csv");

        var csvContent = await response.Content.ReadAsStringAsync();
        csvContent.Should().Contain("InventoryItemName,GenericName,BrandName");
        csvContent.Should().Contain("Test Item 1");
    }

    private async Task SeedTestDataAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var items = new List<InventoryItem>
        {
            new()
            {
                InventoryItemName = "Test Item 1",
                GenericName = "Generic 1",
                BrandName = "Brand 1",
                ManufactureDate = DateTime.Today,
                BatchNumber = "TEST001",
                LicenseNumber = "TEST001",
                ZambiaRegNumber = "TEST001",
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
                InventoryItemName = "Test Item 2",
                GenericName = "Generic 2",
                BrandName = "Brand 2",
                ManufactureDate = DateTime.Today,
                BatchNumber = "TEST002",
                LicenseNumber = "TEST002",
                ZambiaRegNumber = "TEST002",
                PackingType = "Bottle",
                Quantity = 20,
                UnitPrice = 10.99m,
                SellingPrice = 15.99m,
                ReorderLevel = 8,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        await context.InventoryItems.AddRangeAsync(items);
        await context.SaveChangesAsync();
    }

    private async Task<int> SeedTestItemAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var item = new InventoryItem
        {
            InventoryItemName = "Test Item for Update/Delete",
            GenericName = "Test Generic",
            BrandName = "Test Brand",
            ManufactureDate = DateTime.Today,
            BatchNumber = "TEST003",
            LicenseNumber = "TEST003",
            ZambiaRegNumber = "TEST003",
            PackingType = "Box",
            Quantity = 15,
            UnitPrice = 12.99m,
            SellingPrice = 18.99m,
            ReorderLevel = 6,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await context.InventoryItems.AddAsync(item);
        await context.SaveChangesAsync();

        return item.Id;
    }
}

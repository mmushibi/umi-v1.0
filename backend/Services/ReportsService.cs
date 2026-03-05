using Microsoft.EntityFrameworkCore;
using UmiHealthPOS.Data;
using UmiHealthPOS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.IO;
using System.Text;

namespace UmiHealthPOS.Services
{
    public class ReportsService : IReportsService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ReportsService> _logger;

        public ReportsService(ApplicationDbContext context, ILogger<ReportsService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<Branch>> GetUserBranchesAsync(string userId, string userRole)
        {
            try
            {
                var query = _context.UserBranches
                    .Where(ub => ub.UserId == userId && ub.IsActive && ub.Branch.IsActive);

                if (userRole == "SuperAdmin")
                {
                    // Super admins can see all branches
                    return await _context.Branches
                        .Where(b => b.IsActive)
                        .OrderBy(b => b.Name)
                        .ToListAsync();
                }

                return await query
                    .Select(ub => ub.Branch)
                    .OrderBy(b => b.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user branches for user: {UserId}", userId);
                return new List<Branch>();
            }
        }

        public async Task<List<Models.SalesReportDto>> GetSalesReportsAsync(string userId, string userRole, DateTime? startDate, DateTime? endDate, int? branchId = null)
        {
            try
            {
                var query = _context.Sales.AsQueryable();

                if (startDate.HasValue)
                    query = query.Where(s => s.CreatedAt >= startDate.Value);

                if (endDate.HasValue)
                    query = query.Where(s => s.CreatedAt <= endDate.Value);

                if (branchId.HasValue)
                    query = query.Where(s => s.BranchId == branchId.Value);

                var sales = await query
                    .GroupBy(s => new
                    {
                        Year = s.CreatedAt.Year,
                        Month = s.CreatedAt.Month
                    })
                    .Select(g => new Models.SalesReportDto
                    {
                        Period = $"{g.Key.Year}-{g.Key.Month:D2}",
                        StartDate = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("yyyy-MM-dd"),
                        EndDate = new DateTime(g.Key.Year, g.Key.Month, DateTime.DaysInMonth(g.Key.Year, g.Key.Month)).ToString("yyyy-MM-dd"),
                        TotalRevenue = g.Sum(s => s.Total),
                        TotalTransactions = g.Count(),
                        AverageTransaction = g.Average(s => s.Total),
                        MonthlyGrowth = 0 // Would need previous month data for calculation
                    })
                    .OrderByDescending(r => r.Period)
                    .ToListAsync();

                return sales;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sales reports");
                return new List<Models.SalesReportDto>();
            }
        }

        public async Task<List<InventoryReportDto>> GetInventoryReportsAsync(string userId, string userRole, int? branchId = null)
        {
            try
            {
                var query = _context.InventoryItems.AsQueryable();

                if (branchId.HasValue)
                    query = query.Where(i => i.BranchId == branchId.Value);

                var inventory = await query
                    .Select(i => new InventoryReportDto
                    {
                        Id = i.Id,
                        ProductName = i.Name,
                        CurrentStock = i.Quantity,
                        ReorderLevel = i.ReorderLevel,
                        UnitPrice = i.UnitPrice,
                        TotalValue = i.Quantity * i.UnitPrice,
                        Status = i.Quantity <= i.ReorderLevel ? "Low Stock" : "In Stock",
                        BranchName = i.Branch != null ? i.Branch.Name : "Unknown"
                    })
                    .ToListAsync();

                return inventory;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting inventory reports");
                return new List<InventoryReportDto>();
            }
        }

        public async Task<List<FinancialReportDto>> GetFinancialReportsAsync(string userId, string userRole, DateTime? startDate, DateTime? endDate, int? branchId = null)
        {
            try
            {
                var query = _context.Sales.AsQueryable();

                if (startDate.HasValue)
                    query = query.Where(s => s.CreatedAt >= startDate.Value);

                if (endDate.HasValue)
                    query = query.Where(s => s.CreatedAt <= endDate.Value);

                if (branchId.HasValue)
                    query = query.Where(s => s.BranchId == branchId.Value);

                var financial = await query
                    .Join(_context.Branches, s => s.BranchId, b => b.Id, (s, b) => new { s, b })
                    .GroupBy(x => new { x.s.CreatedAt.Date, Branch = x.b.Name })
                    .Select(g => new FinancialReportDto
                    {
                        Id = g.FirstOrDefault().s.Id,
                        Date = g.Key.Date,
                        Description = $"Daily Sales - {g.Key.Date:yyyy-MM-dd}",
                        Revenue = g.Sum(x => x.s.Total),
                        Expenses = 0, // Would need expense tracking
                        NetProfit = g.Sum(x => x.s.Total),
                        BranchName = g.Key.Branch
                    })
                    .ToListAsync();

                return financial;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting financial reports");
                return new List<FinancialReportDto>();
            }
        }

        public async Task<byte[]> ExportReportAsync(string reportType, string format, object parameters)
        {
            try
            {
                switch (reportType.ToLower())
                {
                    case "sales":
                        return await ExportSalesReportAsync(format, parameters);
                    case "inventory":
                        return await ExportInventoryReportAsync(format, parameters);
                    case "financial":
                        return await ExportFinancialReportAsync(format, parameters);
                    case "patients":
                        return await ExportPatientsReportAsync(format, parameters);
                    case "prescriptions":
                        return await ExportPrescriptionsReportAsync(format, parameters);
                    default:
                        throw new ArgumentException($"Unsupported report type: {reportType}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting report {ReportType} in format {Format}", reportType, format);
                throw;
            }
        }

        private async Task<byte[]> ExportSalesReportAsync(string format, object parameters)
        {
            try
            {
                var startDate = parameters?.GetType().GetProperty("StartDate")?.GetValue(parameters) as DateTime?;
                var endDate = parameters?.GetType().GetProperty("EndDate")?.GetValue(parameters) as DateTime?;

                var sales = await _context.Sales
                    .Include(s => s.Customer)
                    .Include(s => s.SaleItems)
                    .ThenInclude(si => si.Product)
                    .Where(s => (!startDate.HasValue || s.CreatedAt >= startDate.Value) &&
                               (!endDate.HasValue || s.CreatedAt <= endDate.Value))
                    .OrderByDescending(s => s.CreatedAt)
                    .ToListAsync();

                return format.ToLower() switch
                {
                    "csv" => GenerateSalesCsv(sales),
                    "excel" => GenerateSalesExcel(sales),
                    "pdf" => GenerateSalesPdf(sales),
                    _ => throw new ArgumentException($"Unsupported format: {format}")
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting sales report");
                throw;
            }
        }

        private async Task<byte[]> ExportInventoryReportAsync(string format, object parameters)
        {
            try
            {
                var inventory = await _context.InventoryItems
                    .Include(i => i.Category)
                    .OrderBy(i => i.Name)
                    .ToListAsync();

                return format.ToLower() switch
                {
                    "csv" => GenerateInventoryCsv(inventory),
                    "excel" => GenerateInventoryExcel(inventory),
                    "pdf" => GenerateInventoryPdf(inventory),
                    _ => throw new ArgumentException($"Unsupported format: {format}")
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting inventory report");
                throw;
            }
        }

        private async Task<byte[]> ExportFinancialReportAsync(string format, object parameters)
        {
            try
            {
                var startDate = parameters?.GetType().GetProperty("StartDate")?.GetValue(parameters) as DateTime?;
                var endDate = parameters?.GetType().GetProperty("EndDate")?.GetValue(parameters) as DateTime?;

                var financials = await GetFinancialReportsAsync("system", "SuperAdmin", startDate, endDate);

                return format.ToLower() switch
                {
                    "csv" => GenerateFinancialCsv(financials),
                    "excel" => GenerateFinancialExcel(financials),
                    "pdf" => GenerateFinancialPdf(financials),
                    _ => throw new ArgumentException($"Unsupported format: {format}")
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting financial report");
                throw;
            }
        }

        private async Task<byte[]> ExportPatientsReportAsync(string format, object parameters)
        {
            try
            {
                var patients = await _context.Patients
                    .OrderBy(p => p.LastName)
                    .ThenBy(p => p.FirstName)
                    .ToListAsync();

                return format.ToLower() switch
                {
                    "csv" => GeneratePatientsCsv(patients),
                    "excel" => GeneratePatientsExcel(patients),
                    "pdf" => GeneratePatientsPdf(patients),
                    _ => throw new ArgumentException($"Unsupported format: {format}")
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting patients report");
                throw;
            }
        }

        private async Task<byte[]> ExportPrescriptionsReportAsync(string format, object parameters)
        {
            try
            {
                var startDate = parameters?.GetType().GetProperty("StartDate")?.GetValue(parameters) as DateTime?;
                var endDate = parameters?.GetType().GetProperty("EndDate")?.GetValue(parameters) as DateTime?;

                var prescriptions = await _context.Prescriptions
                    .Include(p => p.Patient)
                    .Include(p => p.Doctor)
                    .Where(p => (!startDate.HasValue || p.CreatedAt >= startDate.Value) &&
                               (!endDate.HasValue || p.CreatedAt <= endDate.Value))
                    .OrderByDescending(p => p.CreatedAt)
                    .ToListAsync();

                return format.ToLower() switch
                {
                    "csv" => GeneratePrescriptionsCsv(prescriptions),
                    "excel" => GeneratePrescriptionsExcel(prescriptions),
                    "pdf" => GeneratePrescriptionsPdf(prescriptions),
                    _ => throw new ArgumentException($"Unsupported format: {format}")
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting prescriptions report");
                throw;
            }
        }

        private byte[] GenerateSalesCsv(List<Sale> sales)
        {
            var csv = new System.Text.StringBuilder();
            csv.AppendLine("Receipt Number,Customer Name,Date,Total,Status,Payment Method,Cashier");

            foreach (var sale in sales)
            {
                csv.AppendLine($"{sale.ReceiptNumber}," +
                           $"{sale.Customer?.Name ?? "Walk-in"}," +
                           $"{sale.CreatedAt:yyyy-MM-dd HH:mm}," +
                           $"{sale.Total:F2}," +
                           $"{sale.Status}," +
                           $"{sale.PaymentMethod}," +
                           $"{sale.CreatedBy ?? "System"}");
            }

            return System.Text.Encoding.UTF8.GetBytes(csv.ToString());
        }

        private byte[] GenerateInventoryCsv(List<InventoryItem> inventory)
        {
            var csv = new System.Text.StringBuilder();
            csv.AppendLine("Item Name,Category,Quantity,Unit Price,Selling Price,Reorder Level,Status");

            foreach (var item in inventory)
            {
                csv.AppendLine($"{item.Name}," +
                           $"{item.Category ?? "Uncategorized"}," +
                           $"{item.Quantity}," +
                           $"{item.UnitPrice:F2}," +
                           $"{item.SellingPrice:F2}," +
                           $"{item.ReorderLevel}," +
                           $"{(item.Quantity <= item.ReorderLevel ? "Low Stock" : "In Stock")}");
            }

            return System.Text.Encoding.UTF8.GetBytes(csv.ToString());
        }

        private byte[] GenerateFinancialCsv(List<FinancialReportDto> financials)
        {
            var csv = new System.Text.StringBuilder();
            csv.AppendLine("Period,Revenue,Expenses,Profit,Transactions,Average Transaction");

            foreach (var financial in financials)
            {
                csv.AppendLine($"{financial.Date:yyyy-MM-dd}," +
                           $"{financial.Revenue:F2}," +
                           $"{financial.Expenses:F2}," +
                           $"{financial.NetProfit:F2}," +
                           $"0," + // TotalTransactions not available in DTO
                           $"0"); // AverageTransactionValue not available in DTO
            }

            return System.Text.Encoding.UTF8.GetBytes(csv.ToString());
        }

        private byte[] GeneratePatientsCsv(List<Patient> patients)
        {
            var csv = new System.Text.StringBuilder();
            csv.AppendLine("Patient ID,Name,Email,Phone,Date of Birth,Gender,Address");

            foreach (var patient in patients)
            {
                csv.AppendLine($"{patient.Id}," +
                           $"{patient.FirstName} {patient.LastName}," +
                           $"{patient.Email}," +
                           $"{patient.PhoneNumber}," +
                           $"{patient.DateOfBirth:yyyy-MM-dd}," +
                           $"{patient.Gender}," +
                           $"{patient.Address}");
            }

            return System.Text.Encoding.UTF8.GetBytes(csv.ToString());
        }

        private byte[] GeneratePrescriptionsCsv(List<Prescription> prescriptions)
        {
            var csv = new System.Text.StringBuilder();
            csv.AppendLine("RX Number,Patient Name,Doctor Name,Medication,Dosage,Instructions,Date,Status");

            foreach (var prescription in prescriptions)
            {
                csv.AppendLine($"{prescription.RxNumber}," +
                           $"{prescription.Patient?.FirstName} {prescription.Patient?.LastName}," +
                           $"{prescription.DoctorName}," +
                           $"{prescription.Medication}," +
                           $"{prescription.Dosage}," +
                           $"{prescription.Instructions}," +
                           $"{prescription.CreatedAt:yyyy-MM-dd HH:mm}," +
                           $"{prescription.Status}");
            }

            return System.Text.Encoding.UTF8.GetBytes(csv.ToString());
        }

        // Excel Generation Methods
        private byte[] GenerateSalesExcel(List<Sale> sales)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Sales Report");
            
            // Headers
            worksheet.Cell(1, 1).Value = "Sale ID";
            worksheet.Cell(1, 2).Value = "Customer";
            worksheet.Cell(1, 3).Value = "Date";
            worksheet.Cell(1, 4).Value = "Total Amount";
            worksheet.Cell(1, 5).Value = "Payment Method";
            worksheet.Cell(1, 6).Value = "Status";
            
            // Header styling
            var headerRange = worksheet.Range(1, 1, 1, 6);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;
            
            // Data
            int row = 2;
            foreach (var sale in sales)
            {
                worksheet.Cell(row, 1).Value = sale.Id;
                worksheet.Cell(row, 2).Value = sale.Customer?.Name ?? "Walk-in";
                worksheet.Cell(row, 3).Value = sale.CreatedAt.ToString("yyyy-MM-dd HH:mm");
                worksheet.Cell(row, 4).Value = sale.TotalAmount;
                worksheet.Cell(row, 5).Value = sale.PaymentMethod;
                worksheet.Cell(row, 6).Value = sale.Status;
                row++;
            }
            
            // Auto-fit columns
            worksheet.Columns().AdjustToContents();
            
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        private byte[] GenerateInventoryExcel(List<InventoryItem> inventory)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Inventory Report");
            
            // Headers
            worksheet.Cell(1, 1).Value = "Product Name";
            worksheet.Cell(1, 2).Value = "Category";
            worksheet.Cell(1, 3).Value = "Quantity";
            worksheet.Cell(1, 4).Value = "Unit Price";
            worksheet.Cell(1, 5).Value = "Selling Price";
            worksheet.Cell(1, 6).Value = "Reorder Level";
            worksheet.Cell(1, 7).Value = "Status";
            
            // Header styling
            var headerRange = worksheet.Range(1, 1, 1, 7);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGreen;
            
            // Data
            int row = 2;
            foreach (var item in inventory)
            {
                worksheet.Cell(row, 1).Value = item.Name;
                worksheet.Cell(row, 2).Value = item.Category ?? "Uncategorized";
                worksheet.Cell(row, 3).Value = item.Quantity;
                worksheet.Cell(row, 4).Value = item.UnitPrice;
                worksheet.Cell(row, 5).Value = item.SellingPrice;
                worksheet.Cell(row, 6).Value = item.ReorderLevel;
                worksheet.Cell(row, 7).Value = item.Quantity <= item.ReorderLevel ? "Low Stock" : "In Stock";
                row++;
            }
            
            worksheet.Columns().AdjustToContents();
            
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        private byte[] GenerateFinancialExcel(List<FinancialReportDto> financials)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Financial Report");
            
            worksheet.Cell(1, 1).Value = "Financial Report";
            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Cell(1, 1).Style.Font.FontSize = 16;
            
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        private byte[] GeneratePatientsExcel(List<Patient> patients)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Patients Report");
            
            // Headers
            worksheet.Cell(1, 1).Value = "Patient ID";
            worksheet.Cell(1, 2).Value = "Name";
            worksheet.Cell(1, 3).Value = "Email";
            worksheet.Cell(1, 4).Value = "Phone";
            worksheet.Cell(1, 5).Value = "Date of Birth";
            worksheet.Cell(1, 6).Value = "Registration Date";
            
            // Header styling
            var headerRange = worksheet.Range(1, 1, 1, 6);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightCoral;
            
            // Data
            int row = 2;
            foreach (var patient in patients)
            {
                worksheet.Cell(row, 1).Value = patient.Id;
                worksheet.Cell(row, 2).Value = $"{patient.FirstName} {patient.LastName}";
                worksheet.Cell(row, 3).Value = patient.Email ?? "N/A";
                worksheet.Cell(row, 4).Value = patient.PhoneNumber ?? "N/A";
                worksheet.Cell(row, 5).Value = patient.DateOfBirth?.ToString("yyyy-MM-dd") ?? "N/A";
                worksheet.Cell(row, 6).Value = patient.CreatedAt.ToString("yyyy-MM-dd");
                row++;
            }
            
            worksheet.Columns().AdjustToContents();
            
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        private byte[] GeneratePrescriptionsExcel(List<Prescription> prescriptions)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Prescriptions Report");
            
            // Headers
            worksheet.Cell(1, 1).Value = "Prescription ID";
            worksheet.Cell(1, 2).Value = "Patient";
            worksheet.Cell(1, 3).Value = "Doctor";
            worksheet.Cell(1, 4).Value = "Medication";
            worksheet.Cell(1, 5).Value = "Dosage";
            worksheet.Cell(1, 6).Value = "Instructions";
            worksheet.Cell(1, 7).Value = "Date";
            worksheet.Cell(1, 8).Value = "Status";
            
            // Header styling
            var headerRange = worksheet.Range(1, 1, 1, 8);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightYellow;
            
            // Data
            int row = 2;
            foreach (var prescription in prescriptions)
            {
                worksheet.Cell(row, 1).Value = prescription.Id;
                worksheet.Cell(row, 2).Value = prescription.Patient?.FirstName + " " + prescription.Patient?.LastName;
                worksheet.Cell(row, 3).Value = prescription.Doctor?.Name ?? "N/A";
                worksheet.Cell(row, 4).Value = prescription.Medication;
                worksheet.Cell(row, 5).Value = prescription.Dosage;
                worksheet.Cell(row, 6).Value = prescription.Instructions;
                worksheet.Cell(row, 7).Value = prescription.CreatedAt.ToString("yyyy-MM-dd HH:mm");
                worksheet.Cell(row, 8).Value = prescription.Status;
                row++;
            }
            
            worksheet.Columns().AdjustToContents();
            
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        // PDF Generation Methods
        private byte[] GenerateSalesPdf(List<Sale> sales)
        {
            QuestPDF.Settings.License = LicenseType.Community;
            
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Header().Text("Sales Report").FontSize(20).Bold().FontColor(Colors.Blue.Medium);
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                    });
                    
                    page.Content().Column(column =>
                    {
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(50);
                                columns.RelativeColumn();
                                columns.ConstantColumn(80);
                                columns.ConstantColumn(60);
                                columns.ConstantColumn(80);
                                columns.ConstantColumn(60);
                            });
                            
                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("ID").Bold();
                                header.Cell().Element(CellStyle).Text("Customer").Bold();
                                header.Cell().Element(CellStyle).Text("Date").Bold();
                                header.Cell().Element(CellStyle).Text("Amount").Bold();
                                header.Cell().Element(CellStyle).Text("Payment").Bold();
                                header.Cell().Element(CellStyle).Text("Status").Bold();
                            });
                            
                            foreach (var sale in sales.Take(50)) // Limit to 50 for PDF
                            {
                                table.Cell().Element(CellStyle).Text(sale.Id.ToString());
                                table.Cell().Element(CellStyle).Text(sale.Customer?.Name ?? "Walk-in");
                                table.Cell().Element(CellStyle).Text(sale.CreatedAt.ToString("yyyy-MM-dd"));
                                table.Cell().Element(CellStyle).Text($"K{sale.TotalAmount:F2}");
                                table.Cell().Element(CellStyle).Text(sale.PaymentMethod);
                                table.Cell().Element(CellStyle).Text(sale.Status);
                            }
                        });
                    });
                });
            });
            
            using var stream = new MemoryStream();
            document.GeneratePdf(stream);
            return stream.ToArray();
        }

        private byte[] GenerateInventoryPdf(List<InventoryItem> inventory)
        {
            QuestPDF.Settings.License = LicenseType.Community;
            
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Header().Text("Inventory Report").FontSize(20).Bold().FontColor(Colors.Green.Medium);
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                    });
                    
                    page.Content().Column(column =>
                    {
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(2);
                                columns.ConstantColumn(50);
                                columns.ConstantColumn(60);
                                columns.ConstantColumn(60);
                                columns.ConstantColumn(60);
                                columns.ConstantColumn(70);
                            });
                            
                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("Product").Bold();
                                header.Cell().Element(CellStyle).Text("Category").Bold();
                                header.Cell().Element(CellStyle).Text("Qty").Bold();
                                header.Cell().Element(CellStyle).Text("Unit Price").Bold();
                                header.Cell().Element(CellStyle).Text("Selling Price").Bold();
                                header.Cell().Element(CellStyle).Text("Reorder").Bold();
                                header.Cell().Element(CellStyle).Text("Status").Bold();
                            });
                            
                            foreach (var item in inventory.Take(50))
                            {
                                table.Cell().Element(CellStyle).Text(item.Name);
                                table.Cell().Element(CellStyle).Text(item.Category ?? "Uncategorized");
                                table.Cell().Element(CellStyle).Text(item.Quantity.ToString());
                                table.Cell().Element(CellStyle).Text($"K{item.UnitPrice:F2}");
                                table.Cell().Element(CellStyle).Text($"K{item.SellingPrice:F2}");
                                table.Cell().Element(CellStyle).Text(item.ReorderLevel.ToString());
                                table.Cell().Element(CellStyle).Text(item.Quantity <= item.ReorderLevel ? "Low Stock" : "In Stock");
                            }
                        });
                    });
                });
            });
            
            using var stream = new MemoryStream();
            document.GeneratePdf(stream);
            return stream.ToArray();
        }

        private byte[] GenerateFinancialPdf(List<FinancialReportDto> financials)
        {
            QuestPDF.Settings.License = LicenseType.Community;
            
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Header().Text("Financial Report").FontSize(20).Bold().FontColor(Colors.Purple.Medium);
                    page.Content().Padding(30).Column(column =>
                    {
                        column.Item().Text("Financial Report Summary").FontSize(16).Bold();
                        column.Item().Text($"Generated on: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    });
                });
            });
            
            using var stream = new MemoryStream();
            document.GeneratePdf(stream);
            return stream.ToArray();
        }

        private byte[] GeneratePatientsPdf(List<Patient> patients)
        {
            QuestPDF.Settings.License = LicenseType.Community;
            
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Header().Text("Patients Report").FontSize(20).Bold().FontColor(Colors.Red.Medium);
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                    });
                    
                    page.Content().Column(column =>
                    {
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(60);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                                columns.ConstantColumn(80);
                                columns.ConstantColumn(80);
                            });
                            
                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("Patient ID").Bold();
                                header.Cell().Element(CellStyle).Text("Name").Bold();
                                header.Cell().Element(CellStyle).Text("Email").Bold();
                                header.Cell().Element(CellStyle).Text("Phone").Bold();
                                header.Cell().Element(CellStyle).Text("DOB").Bold();
                                header.Cell().Element(CellStyle).Text("Registered").Bold();
                            });
                            
                            foreach (var patient in patients.Take(50))
                            {
                                table.Cell().Element(CellStyle).Text(patient.Id.ToString());
                                table.Cell().Element(CellStyle).Text($"{patient.FirstName} {patient.LastName}");
                                table.Cell().Element(CellStyle).Text(patient.Email ?? "N/A");
                                table.Cell().Element(CellStyle).Text(patient.PhoneNumber ?? "N/A");
                                table.Cell().Element(CellStyle).Text(patient.DateOfBirth?.ToString("yyyy-MM-dd") ?? "N/A");
                                table.Cell().Element(CellStyle).Text(patient.CreatedAt.ToString("yyyy-MM-dd"));
                            }
                        });
                    });
                });
            });
            
            using var stream = new MemoryStream();
            document.GeneratePdf(stream);
            return stream.ToArray();
        }

        private byte[] GeneratePrescriptionsPdf(List<Prescription> prescriptions)
        {
            QuestPDF.Settings.License = LicenseType.Community;
            
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Header().Text("Prescriptions Report").FontSize(20).Bold().FontColor(Colors.Orange.Medium);
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                    });
                    
                    page.Content().Column(column =>
                    {
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(80);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                                columns.ConstantColumn(60);
                                columns.RelativeColumn(3);
                                columns.ConstantColumn(80);
                                columns.ConstantColumn(60);
                            });
                            
                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("Rx ID").Bold();
                                header.Cell().Element(CellStyle).Text("Patient").Bold();
                                header.Cell().Element(CellStyle).Text("Doctor").Bold();
                                header.Cell().Element(CellStyle).Text("Medication").Bold();
                                header.Cell().Element(CellStyle).Text("Dosage").Bold();
                                header.Cell().Element(CellStyle).Text("Instructions").Bold();
                                header.Cell().Element(CellStyle).Text("Date").Bold();
                                header.Cell().Element(CellStyle).Text("Status").Bold();
                            });
                            
                            foreach (var prescription in prescriptions.Take(50))
                            {
                                table.Cell().Element(CellStyle).Text(prescription.Id.ToString());
                                table.Cell().Element(CellStyle).Text($"{prescription.Patient?.FirstName} {prescription.Patient?.LastName}");
                                table.Cell().Element(CellStyle).Text(prescription.Doctor?.Name ?? "N/A");
                                table.Cell().Element(CellStyle).Text(prescription.Medication);
                                table.Cell().Element(CellStyle).Text(prescription.Dosage);
                                table.Cell().Element(CellStyle).Text(prescription.Instructions);
                                table.Cell().Element(CellStyle).Text(prescription.CreatedAt.ToString("yyyy-MM-dd"));
                                table.Cell().Element(CellStyle).Text(prescription.Status);
                            }
                        });
                    });
                });
            });
            
            using var stream = new MemoryStream();
            document.GeneratePdf(stream);
            return stream.ToArray();
        }

        static IContainer CellStyle(IContainer container)
        {
            return container
                .Border(1)
                .BorderColor(Colors.Grey.Lighten2)
                .Padding(5)
                .AlignCenter()
                .AlignMiddle();
        }

        public async Task<List<BranchPerformanceDto>> GetBranchPerformanceAsync(string userId, string userRole, DateTime? startDate, DateTime? endDate)
        {
            try
            {
                var query = _context.Sales.AsQueryable();

                if (startDate.HasValue)
                    query = query.Where(s => s.CreatedAt >= startDate.Value);

                if (endDate.HasValue)
                    query = query.Where(s => s.CreatedAt <= endDate.Value);

                var performance = await query
                    .Join(_context.Branches, s => s.BranchId, b => b.Id, (s, b) => new { s, b })
                    .GroupBy(x => x.b)
                    .Select(g => new BranchPerformanceDto
                    {
                        Id = g.Key.Id,
                        BranchName = g.Key.Name,
                        TotalRevenue = g.Sum(x => x.s.Total),
                        TotalSales = g.Count(),
                        AverageTransactionValue = g.Average(x => x.s.Total),
                        UniqueCustomers = g.Select(x => x.s.CustomerId).Distinct().Count(),
                        GrowthPercentage = 0 // Would need historical data
                    })
                    .ToListAsync();

                return performance;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting branch performance");
                return new List<BranchPerformanceDto>();
            }
        }
    }
}

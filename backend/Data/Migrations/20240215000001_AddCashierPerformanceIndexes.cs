using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UmiHealthPOS.Data.Migrations
{
    public partial class AddCashierPerformanceIndexes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create optimized indexes for cashier dashboard queries
            migrationBuilder.Sql(
                "CREATE INDEX IF NOT EXISTS \"IX_Sales_TenantId_CreatedAt\" \"Sales\" (\"TenantId\", \"CreatedAt\" DESC);");

            migrationBuilder.Sql(
                "CREATE INDEX IF NOT EXISTS \"IX_Sales_TenantId_CreatedAt_Total\" \"Sales\" (\"TenantId\", \"CreatedAt\", \"Total\");");

            migrationBuilder.Sql(
                "CREATE INDEX IF NOT EXISTS \"IX_Sales_TenantId_CustomerId_CreatedAt\" \"Sales\" (\"TenantId\", \"CustomerId\", \"CreatedAt\" DESC);");

            migrationBuilder.Sql(
                "CREATE INDEX IF NOT EXISTS \"IX_Sales_ReceiptNumber_TenantId\" \"Sales\" (\"ReceiptNumber\", \"TenantId\");");

            // Create partial indexes for better performance on filtered queries
            migrationBuilder.Sql(
                "CREATE INDEX IF NOT EXISTS \"IX_Sales_Today_TenantId\" \"Sales\" (\"TenantId\", \"CreatedAt\") WHERE \"CreatedAt\" >= CURRENT_DATE;");

            // Optimize SaleItems for sales detail queries
            migrationBuilder.Sql(
                "CREATE INDEX IF NOT EXISTS \"IX_SaleItems_SaleId_ProductId\" \"SaleItems\" (\"SaleId\", \"ProductId\");");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "DROP INDEX IF EXISTS \"IX_Sales_TenantId_CreatedAt\";");

            migrationBuilder.Sql(
                "DROP INDEX IF EXISTS \"IX_Sales_TenantId_CreatedAt_Total\";");

            migrationBuilder.Sql(
                "DROP INDEX IF EXISTS \"IX_Sales_TenantId_CustomerId_CreatedAt\";");

            migrationBuilder.Sql(
                "DROP INDEX IF EXISTS \"IX_Sales_ReceiptNumber_TenantId\";");

            migrationBuilder.Sql(
                "DROP INDEX IF EXISTS \"IX_Sales_Today_TenantId\";");

            migrationBuilder.Sql(
                "DROP INDEX IF EXISTS \"IX_SaleItems_SaleId_ProductId\";");
        }
    }
}

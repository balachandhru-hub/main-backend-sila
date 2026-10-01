using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Buyer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SetErpOperationPoCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "process",
                schema: "buyersystem",
                table: "erp_integration_configuration",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "PO_CREATE",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldDefaultValue: "WISHLIST");

            migrationBuilder.Sql(
                "UPDATE [buyersystem].[erp_integration_configuration] SET [process] = N'PO_CREATE' WHERE [process] = N'WISHLIST' OR [process] = N''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE [buyersystem].[erp_integration_configuration] SET [process] = N'WISHLIST' WHERE [process] = N'PO_CREATE'");

            migrationBuilder.AlterColumn<string>(
                name: "process",
                schema: "buyersystem",
                table: "erp_integration_configuration",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "WISHLIST",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldDefaultValue: "PO_CREATE");
        }
    }
}

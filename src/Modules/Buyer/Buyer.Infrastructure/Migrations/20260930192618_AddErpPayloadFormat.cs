using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Buyer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddErpPayloadFormat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "payload_format",
                schema: "buyersystem",
                table: "erp_integration_configuration",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "supplier_organization_id",
                schema: "buyersystem",
                table: "erp_integration_configuration",
                type: "uniqueidentifier",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "payload_format",
                schema: "buyersystem",
                table: "erp_integration_configuration");

            migrationBuilder.DropColumn(
                name: "supplier_organization_id",
                schema: "buyersystem",
                table: "erp_integration_configuration");
        }
    }
}

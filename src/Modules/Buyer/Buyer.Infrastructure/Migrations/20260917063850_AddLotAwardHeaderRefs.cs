using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Buyer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLotAwardHeaderRefs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "quotation_version",
                schema: "buyersystem",
                table: "rfqaward",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "supplier_id",
                schema: "buyersystem",
                table: "rfqaward",
                type: "uniqueidentifier",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "quotation_version",
                schema: "buyersystem",
                table: "rfqaward");

            migrationBuilder.DropColumn(
                name: "supplier_id",
                schema: "buyersystem",
                table: "rfqaward");
        }
    }
}

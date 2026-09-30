using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Buyer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TypetoMaterApprovalFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "currency",
                schema: "buyersystem",
                table: "master_approval_flow",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "total_amount",
                schema: "buyersystem",
                table: "master_approval_flow",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "type",
                schema: "buyersystem",
                table: "master_approval_flow",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "currency",
                schema: "buyersystem",
                table: "master_approval_flow");

            migrationBuilder.DropColumn(
                name: "total_amount",
                schema: "buyersystem",
                table: "master_approval_flow");

            migrationBuilder.DropColumn(
                name: "type",
                schema: "buyersystem",
                table: "master_approval_flow");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Buyer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSegmentAndFamilyToRFQ : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "family_id",
                schema: "buyersystem",
                table: "rfq",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "family_title",
                schema: "buyersystem",
                table: "rfq",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "segment_id",
                schema: "buyersystem",
                table: "rfq",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "segment_title",
                schema: "buyersystem",
                table: "rfq",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "family_id",
                schema: "buyersystem",
                table: "rfq");

            migrationBuilder.DropColumn(
                name: "family_title",
                schema: "buyersystem",
                table: "rfq");

            migrationBuilder.DropColumn(
                name: "segment_id",
                schema: "buyersystem",
                table: "rfq");

            migrationBuilder.DropColumn(
                name: "segment_title",
                schema: "buyersystem",
                table: "rfq");
        }
    }
}

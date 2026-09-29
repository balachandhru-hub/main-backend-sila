using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Buyer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSegmentFromContractAttachment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "segment_id",
                schema: "buyersystem",
                table: "contract_attachment");

            migrationBuilder.DropColumn(
                name: "segment_title",
                schema: "buyersystem",
                table: "contract_attachment");

            migrationBuilder.DropColumn(
                name: "title",
                schema: "buyersystem",
                table: "contract_attachment");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "segment_id",
                schema: "buyersystem",
                table: "contract_attachment",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "segment_title",
                schema: "buyersystem",
                table: "contract_attachment",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "title",
                schema: "buyersystem",
                table: "contract_attachment",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}

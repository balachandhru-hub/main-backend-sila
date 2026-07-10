using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Buyer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStatusingBuyer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "comment",
                schema: "buyersystem",
                table: "buyer_business_profile",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status",
                schema: "buyersystem",
                table: "buyer_business_profile",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "comment",
                schema: "buyersystem",
                table: "buyer_business_profile");

            migrationBuilder.DropColumn(
                name: "status",
                schema: "buyersystem",
                table: "buyer_business_profile");
        }
    }
}

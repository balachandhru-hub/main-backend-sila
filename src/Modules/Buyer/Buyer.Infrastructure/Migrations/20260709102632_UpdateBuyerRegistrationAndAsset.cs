using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Buyer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateBuyerRegistrationAndAsset : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "email_verified",
                schema: "buyersystem",
                table: "buyer_business_profile");

            migrationBuilder.AddColumn<string>(
                name: "registration_name",
                schema: "buyersystem",
                table: "buyer_registration",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "registration_type",
                schema: "buyersystem",
                table: "buyer_registration",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "asset",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    entity_type = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    asset_type = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    file_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    entity_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    file_type = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    asset_name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asset", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_asset_is_active",
                schema: "buyersystem",
                table: "asset",
                column: "is_active");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "asset",
                schema: "buyersystem");

            migrationBuilder.DropColumn(
                name: "registration_name",
                schema: "buyersystem",
                table: "buyer_registration");

            migrationBuilder.DropColumn(
                name: "registration_type",
                schema: "buyersystem",
                table: "buyer_registration");

            migrationBuilder.AddColumn<bool>(
                name: "email_verified",
                schema: "buyersystem",
                table: "buyer_business_profile",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}

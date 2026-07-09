using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Buyer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "buyersystem");

            migrationBuilder.CreateTable(
                name: "buyer_bank_account",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    account_holder_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    bank_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    branch_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    account_number = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ifsccode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    swiftcode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    currency = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    is_primary = table.Column<bool>(type: "bit", nullable: false),
                    is_verified = table.Column<bool>(type: "bit", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_buyer_bank_account", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "buyer_business_profile",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    organization_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    organization_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    phone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    email_verified = table.Column<bool>(type: "bit", nullable: false),
                    country = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    address_line1 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    address_line2 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    city = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    state = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    pin_code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    industry = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    business_type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    employee_count = table.Column<int>(type: "int", nullable: true),
                    annual_turnover = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    currency = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    year_established = table.Column<int>(type: "int", nullable: true),
                    website = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_buyer_business_profile", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "buyer_category",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    segment = table.Column<long>(type: "bigint", nullable: false),
                    segment_title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    family = table.Column<long>(type: "bigint", nullable: true),
                    family_title = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    @class = table.Column<long>(name: "class", type: "bigint", nullable: true),
                    class_title = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    commodity = table.Column<long>(type: "bigint", nullable: true),
                    commodity_title = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_buyer_category", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "buyer_delivery_location",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    location_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    address_line1 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    address_line2 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    city = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    state = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    country = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    pin_code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    contact_person = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    contact_phone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    is_default = table.Column<bool>(type: "bit", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_buyer_delivery_location", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "buyer_registration",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    registration_number = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    asset_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    is_verified = table.Column<bool>(type: "bit", nullable: false),
                    verified_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    expiry_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_buyer_registration", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_buyer_bank_account_is_active",
                schema: "buyersystem",
                table: "buyer_bank_account",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_buyer_business_profile_is_active",
                schema: "buyersystem",
                table: "buyer_business_profile",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_buyer_category_is_active",
                schema: "buyersystem",
                table: "buyer_category",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_buyer_delivery_location_is_active",
                schema: "buyersystem",
                table: "buyer_delivery_location",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_buyer_registration_is_active",
                schema: "buyersystem",
                table: "buyer_registration",
                column: "is_active");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "buyer_bank_account",
                schema: "buyersystem");

            migrationBuilder.DropTable(
                name: "buyer_business_profile",
                schema: "buyersystem");

            migrationBuilder.DropTable(
                name: "buyer_category",
                schema: "buyersystem");

            migrationBuilder.DropTable(
                name: "buyer_delivery_location",
                schema: "buyersystem");

            migrationBuilder.DropTable(
                name: "buyer_registration",
                schema: "buyersystem");
        }
    }
}

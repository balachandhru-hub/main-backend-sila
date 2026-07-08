using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Supplier.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "supplier");

            migrationBuilder.CreateTable(
                name: "supplier_bank_accounts",
                schema: "supplier",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    account_holder_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    bank_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    branch_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    account_number = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ifsccode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    swiftcode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    iban = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("pk_supplier_bank_accounts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "supplier_business_profiles",
                schema: "supplier",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    organization_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    organization_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    organization_type = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("pk_supplier_business_profiles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "supplier_dispatch_locations",
                schema: "supplier",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    location_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    address_line1 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    address_line2 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    city = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    state = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    country = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    pin_code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    contact_person = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    contact_email = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("pk_supplier_dispatch_locations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "supplier_registrations",
                schema: "supplier",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    registration_type = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    registration_number = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    registration_name = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("pk_supplier_registrations", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_supplier_bank_accounts_is_active",
                schema: "supplier",
                table: "supplier_bank_accounts",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_supplier_business_profiles_is_active",
                schema: "supplier",
                table: "supplier_business_profiles",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_supplier_dispatch_locations_is_active",
                schema: "supplier",
                table: "supplier_dispatch_locations",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_supplier_registrations_is_active",
                schema: "supplier",
                table: "supplier_registrations",
                column: "is_active");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "supplier_bank_accounts",
                schema: "supplier");

            migrationBuilder.DropTable(
                name: "supplier_business_profiles",
                schema: "supplier");

            migrationBuilder.DropTable(
                name: "supplier_dispatch_locations",
                schema: "supplier");

            migrationBuilder.DropTable(
                name: "supplier_registrations",
                schema: "supplier");
        }
    }
}

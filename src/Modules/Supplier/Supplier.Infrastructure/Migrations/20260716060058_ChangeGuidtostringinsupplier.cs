using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Supplier.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChangeGuidtostringinsupplier : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "pk_supplier_registrations",
                schema: "supplier",
                table: "supplier_registrations");

            migrationBuilder.DropPrimaryKey(
                name: "pk_supplier_dispatch_locations",
                schema: "supplier",
                table: "supplier_dispatch_locations");

            migrationBuilder.DropPrimaryKey(
                name: "pk_supplier_business_profiles",
                schema: "supplier",
                table: "supplier_business_profiles");

            migrationBuilder.DropPrimaryKey(
                name: "pk_supplier_bank_accounts",
                schema: "supplier",
                table: "supplier_bank_accounts");

            migrationBuilder.RenameTable(
                name: "supplier_registrations",
                schema: "supplier",
                newName: "supplier_registration",
                newSchema: "supplier");

            migrationBuilder.RenameTable(
                name: "supplier_dispatch_locations",
                schema: "supplier",
                newName: "supplier_dispatch_location",
                newSchema: "supplier");

            migrationBuilder.RenameTable(
                name: "supplier_business_profiles",
                schema: "supplier",
                newName: "supplier_business_profile",
                newSchema: "supplier");

            migrationBuilder.RenameTable(
                name: "supplier_bank_accounts",
                schema: "supplier",
                newName: "supplier_bank_account",
                newSchema: "supplier");

            migrationBuilder.RenameIndex(
                name: "ix_supplier_registrations_is_active",
                schema: "supplier",
                table: "supplier_registration",
                newName: "ix_supplier_registration_is_active");

            migrationBuilder.RenameIndex(
                name: "ix_supplier_dispatch_locations_is_active",
                schema: "supplier",
                table: "supplier_dispatch_location",
                newName: "ix_supplier_dispatch_location_is_active");

            migrationBuilder.RenameIndex(
                name: "ix_supplier_business_profiles_is_active",
                schema: "supplier",
                table: "supplier_business_profile",
                newName: "ix_supplier_business_profile_is_active");

            migrationBuilder.RenameIndex(
                name: "ix_supplier_bank_accounts_is_active",
                schema: "supplier",
                table: "supplier_bank_account",
                newName: "ix_supplier_bank_account_is_active");

            migrationBuilder.AlterColumn<string>(
                name: "registration_type",
                schema: "supplier",
                table: "supplier_registration",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddPrimaryKey(
                name: "pk_supplier_registration",
                schema: "supplier",
                table: "supplier_registration",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_supplier_dispatch_location",
                schema: "supplier",
                table: "supplier_dispatch_location",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_supplier_business_profile",
                schema: "supplier",
                table: "supplier_business_profile",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_supplier_bank_account",
                schema: "supplier",
                table: "supplier_bank_account",
                column: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "pk_supplier_registration",
                schema: "supplier",
                table: "supplier_registration");

            migrationBuilder.DropPrimaryKey(
                name: "pk_supplier_dispatch_location",
                schema: "supplier",
                table: "supplier_dispatch_location");

            migrationBuilder.DropPrimaryKey(
                name: "pk_supplier_business_profile",
                schema: "supplier",
                table: "supplier_business_profile");

            migrationBuilder.DropPrimaryKey(
                name: "pk_supplier_bank_account",
                schema: "supplier",
                table: "supplier_bank_account");

            migrationBuilder.RenameTable(
                name: "supplier_registration",
                schema: "supplier",
                newName: "supplier_registrations",
                newSchema: "supplier");

            migrationBuilder.RenameTable(
                name: "supplier_dispatch_location",
                schema: "supplier",
                newName: "supplier_dispatch_locations",
                newSchema: "supplier");

            migrationBuilder.RenameTable(
                name: "supplier_business_profile",
                schema: "supplier",
                newName: "supplier_business_profiles",
                newSchema: "supplier");

            migrationBuilder.RenameTable(
                name: "supplier_bank_account",
                schema: "supplier",
                newName: "supplier_bank_accounts",
                newSchema: "supplier");

            migrationBuilder.RenameIndex(
                name: "ix_supplier_registration_is_active",
                schema: "supplier",
                table: "supplier_registrations",
                newName: "ix_supplier_registrations_is_active");

            migrationBuilder.RenameIndex(
                name: "ix_supplier_dispatch_location_is_active",
                schema: "supplier",
                table: "supplier_dispatch_locations",
                newName: "ix_supplier_dispatch_locations_is_active");

            migrationBuilder.RenameIndex(
                name: "ix_supplier_business_profile_is_active",
                schema: "supplier",
                table: "supplier_business_profiles",
                newName: "ix_supplier_business_profiles_is_active");

            migrationBuilder.RenameIndex(
                name: "ix_supplier_bank_account_is_active",
                schema: "supplier",
                table: "supplier_bank_accounts",
                newName: "ix_supplier_bank_accounts_is_active");

            migrationBuilder.AlterColumn<Guid>(
                name: "registration_type",
                schema: "supplier",
                table: "supplier_registrations",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddPrimaryKey(
                name: "pk_supplier_registrations",
                schema: "supplier",
                table: "supplier_registrations",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_supplier_dispatch_locations",
                schema: "supplier",
                table: "supplier_dispatch_locations",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_supplier_business_profiles",
                schema: "supplier",
                table: "supplier_business_profiles",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_supplier_bank_accounts",
                schema: "supplier",
                table: "supplier_bank_accounts",
                column: "id");
        }
    }
}

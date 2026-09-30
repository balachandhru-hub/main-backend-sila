using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Supplier.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SupplierEmailOtpVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "supplier_email_verification",
                schema: "supplier",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    otp_hash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    buyer_rfqid = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    expires_on = table.Column<DateTime>(type: "datetime2", nullable: false),
                    attempt_count = table.Column<int>(type: "int", nullable: false),
                    is_verified = table.Column<bool>(type: "bit", nullable: false),
                    ip_address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    temporary_verification_token = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    temporary_verification_token_expires_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_supplier_email_verification", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_supplier_email_verification_is_active",
                schema: "supplier",
                table: "supplier_email_verification",
                column: "is_active");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "supplier_email_verification",
                schema: "supplier");
        }
    }
}

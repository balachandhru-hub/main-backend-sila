using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Buyer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRFQSupplierMapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "rfqnumber",
                schema: "buyersystem",
                table: "supplier_verification_request",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "rfqsupplier_mapping",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    rfqid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    rfqnumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rfqsupplier_mapping", x => x.id);
                    table.ForeignKey(
                        name: "fk_rfqsupplier_mapping_rfq_rfqid",
                        column: x => x.rfqid,
                        principalSchema: "buyersystem",
                        principalTable: "rfq",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_supplier_verification_request_rfqid",
                schema: "buyersystem",
                table: "supplier_verification_request",
                column: "rfqid");

            migrationBuilder.CreateIndex(
                name: "ix_rfqsupplier_mapping_is_active",
                schema: "buyersystem",
                table: "rfqsupplier_mapping",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_rfqsupplier_mapping_rfqid",
                schema: "buyersystem",
                table: "rfqsupplier_mapping",
                column: "rfqid");

            migrationBuilder.AddForeignKey(
                name: "fk_supplier_verification_request_rfq_rfqid",
                schema: "buyersystem",
                table: "supplier_verification_request",
                column: "rfqid",
                principalSchema: "buyersystem",
                principalTable: "rfq",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_supplier_verification_request_rfq_rfqid",
                schema: "buyersystem",
                table: "supplier_verification_request");

            migrationBuilder.DropTable(
                name: "rfqsupplier_mapping",
                schema: "buyersystem");

            migrationBuilder.DropIndex(
                name: "ix_supplier_verification_request_rfqid",
                schema: "buyersystem",
                table: "supplier_verification_request");

            migrationBuilder.DropColumn(
                name: "rfqnumber",
                schema: "buyersystem",
                table: "supplier_verification_request");
        }
    }
}

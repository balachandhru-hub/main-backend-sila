using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Buyer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class externalsupplier : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "external_supplier",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    supplier_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    phone_number = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    address = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_external_supplier", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "rfqexternal_supplier",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    rfqid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    external_supplier_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rfqexternal_supplier", x => x.id);
                    table.ForeignKey(
                        name: "fk_rfqexternal_supplier_external_supplier_external_supplier_id",
                        column: x => x.external_supplier_id,
                        principalSchema: "buyersystem",
                        principalTable: "external_supplier",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_rfqexternal_supplier_rfq_rfqid",
                        column: x => x.rfqid,
                        principalSchema: "buyersystem",
                        principalTable: "rfq",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_external_supplier_is_active",
                schema: "buyersystem",
                table: "external_supplier",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_rfqexternal_supplier_external_supplier_id",
                schema: "buyersystem",
                table: "rfqexternal_supplier",
                column: "external_supplier_id");

            migrationBuilder.CreateIndex(
                name: "ix_rfqexternal_supplier_is_active",
                schema: "buyersystem",
                table: "rfqexternal_supplier",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_rfqexternal_supplier_rfqid",
                schema: "buyersystem",
                table: "rfqexternal_supplier",
                column: "rfqid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "rfqexternal_supplier",
                schema: "buyersystem");

            migrationBuilder.DropTable(
                name: "external_supplier",
                schema: "buyersystem");
        }
    }
}

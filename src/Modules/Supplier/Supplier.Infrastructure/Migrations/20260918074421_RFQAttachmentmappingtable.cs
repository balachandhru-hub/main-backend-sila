using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Supplier.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RFQAttachmentmappingtable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "terms_and_condition",
                schema: "supplier",
                table: "supplier_rfq",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "rfqattachment_mapping",
                schema: "supplier",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_rfqid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    supplier_rfqid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    asset_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rfqattachment_mapping", x => x.id);
                    table.ForeignKey(
                        name: "fk_rfqattachment_mapping_supplier_rfq_supplier_rfqid",
                        column: x => x.supplier_rfqid,
                        principalSchema: "supplier",
                        principalTable: "supplier_rfq",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_rfqattachment_mapping_is_active",
                schema: "supplier",
                table: "rfqattachment_mapping",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_rfqattachment_mapping_supplier_rfqid",
                schema: "supplier",
                table: "rfqattachment_mapping",
                column: "supplier_rfqid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "rfqattachment_mapping",
                schema: "supplier");

            migrationBuilder.DropColumn(
                name: "terms_and_condition",
                schema: "supplier",
                table: "supplier_rfq");
        }
    }
}

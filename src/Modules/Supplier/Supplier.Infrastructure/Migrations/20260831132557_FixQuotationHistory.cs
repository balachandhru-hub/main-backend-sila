using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Supplier.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixQuotationHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_supplier_quotation_history_supplier_quotation_supplier_quotation_id",
                schema: "supplier",
                table: "supplier_quotation_history");

            migrationBuilder.DropForeignKey(
                name: "fk_supplier_quotation_history_supplier_rfq_supplier_rfqid",
                schema: "supplier",
                table: "supplier_quotation_history");

            migrationBuilder.DropForeignKey(
                name: "fk_supplier_quotation_item_history_supplier_quotation_item_supplier_quotation_item_id",
                schema: "supplier",
                table: "supplier_quotation_item_history");

            migrationBuilder.DropForeignKey(
                name: "fk_supplier_quotation_item_history_supplier_quotation_supplier_quotation_id",
                schema: "supplier",
                table: "supplier_quotation_item_history");

            migrationBuilder.DropForeignKey(
                name: "fk_supplier_quotation_item_history_supplier_rfqitem_supplier_rfqitem_id",
                schema: "supplier",
                table: "supplier_quotation_item_history");

            migrationBuilder.DropIndex(
                name: "ix_supplier_quotation_item_history_supplier_quotation_item_id",
                schema: "supplier",
                table: "supplier_quotation_item_history");

            migrationBuilder.DropIndex(
                name: "ix_supplier_quotation_item_history_supplier_rfqitem_id",
                schema: "supplier",
                table: "supplier_quotation_item_history");

            migrationBuilder.DropIndex(
                name: "ix_supplier_quotation_history_supplier_rfqid",
                schema: "supplier",
                table: "supplier_quotation_history");

            migrationBuilder.AddForeignKey(
                name: "fk_supplier_quotation_history_supplier_quotation_supplier_quotation_id",
                schema: "supplier",
                table: "supplier_quotation_history",
                column: "supplier_quotation_id",
                principalSchema: "supplier",
                principalTable: "supplier_quotation",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_supplier_quotation_item_history_supplier_quotation_supplier_quotation_id",
                schema: "supplier",
                table: "supplier_quotation_item_history",
                column: "supplier_quotation_id",
                principalSchema: "supplier",
                principalTable: "supplier_quotation",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_supplier_quotation_history_supplier_quotation_supplier_quotation_id",
                schema: "supplier",
                table: "supplier_quotation_history");

            migrationBuilder.DropForeignKey(
                name: "fk_supplier_quotation_item_history_supplier_quotation_supplier_quotation_id",
                schema: "supplier",
                table: "supplier_quotation_item_history");

            migrationBuilder.CreateIndex(
                name: "ix_supplier_quotation_item_history_supplier_quotation_item_id",
                schema: "supplier",
                table: "supplier_quotation_item_history",
                column: "supplier_quotation_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_supplier_quotation_item_history_supplier_rfqitem_id",
                schema: "supplier",
                table: "supplier_quotation_item_history",
                column: "supplier_rfqitem_id");

            migrationBuilder.CreateIndex(
                name: "ix_supplier_quotation_history_supplier_rfqid",
                schema: "supplier",
                table: "supplier_quotation_history",
                column: "supplier_rfqid");

            migrationBuilder.AddForeignKey(
                name: "fk_supplier_quotation_history_supplier_quotation_supplier_quotation_id",
                schema: "supplier",
                table: "supplier_quotation_history",
                column: "supplier_quotation_id",
                principalSchema: "supplier",
                principalTable: "supplier_quotation",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_supplier_quotation_history_supplier_rfq_supplier_rfqid",
                schema: "supplier",
                table: "supplier_quotation_history",
                column: "supplier_rfqid",
                principalSchema: "supplier",
                principalTable: "supplier_rfq",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_supplier_quotation_item_history_supplier_quotation_item_supplier_quotation_item_id",
                schema: "supplier",
                table: "supplier_quotation_item_history",
                column: "supplier_quotation_item_id",
                principalSchema: "supplier",
                principalTable: "supplier_quotation_item",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_supplier_quotation_item_history_supplier_quotation_supplier_quotation_id",
                schema: "supplier",
                table: "supplier_quotation_item_history",
                column: "supplier_quotation_id",
                principalSchema: "supplier",
                principalTable: "supplier_quotation",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_supplier_quotation_item_history_supplier_rfqitem_supplier_rfqitem_id",
                schema: "supplier",
                table: "supplier_quotation_item_history",
                column: "supplier_rfqitem_id",
                principalSchema: "supplier",
                principalTable: "supplier_rfqitem",
                principalColumn: "id");
        }
    }
}

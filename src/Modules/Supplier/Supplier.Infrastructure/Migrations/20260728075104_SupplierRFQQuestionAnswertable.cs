using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Supplier.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SupplierRFQQuestionAnswertable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "supplier_rfqanswer_option",
                schema: "supplier",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    supplier_rfqquestion_answer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    rfqquestion_option_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_supplier_rfqanswer_option", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "supplier_rfqquestion_answer",
                schema: "supplier",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    supplier_rfqid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    rfqquestion_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_rfqid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    rfqnumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    answer = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    asset_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    question_option_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    answered_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_supplier_rfqquestion_answer", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_supplier_rfqanswer_option_is_active",
                schema: "supplier",
                table: "supplier_rfqanswer_option",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_supplier_rfqquestion_answer_is_active",
                schema: "supplier",
                table: "supplier_rfqquestion_answer",
                column: "is_active");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "supplier_rfqanswer_option",
                schema: "supplier");

            migrationBuilder.DropTable(
                name: "supplier_rfqquestion_answer",
                schema: "supplier");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Supplier.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSupplierAnswer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "supplier_verification_answer",
                schema: "supplier",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    supplier_verification_request_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    verification_template_question_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    template_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    answer = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    asset_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    verification_template_question_option_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    answered_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_supplier_verification_answer", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "supplier_verification_answer_option",
                schema: "supplier",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    supplier_verification_answer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    verification_template_question_option_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_supplier_verification_answer_option", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_supplier_verification_answer_is_active",
                schema: "supplier",
                table: "supplier_verification_answer",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_supplier_verification_answer_option_is_active",
                schema: "supplier",
                table: "supplier_verification_answer_option",
                column: "is_active");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "supplier_verification_answer",
                schema: "supplier");

            migrationBuilder.DropTable(
                name: "supplier_verification_answer_option",
                schema: "supplier");
        }
    }
}

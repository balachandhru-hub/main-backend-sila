using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Supplier.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SupplierCategorytable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "supplier_category",
                schema: "supplier",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    segment = table.Column<long>(type: "bigint", nullable: false),
                    segment_title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    family = table.Column<long>(type: "bigint", nullable: true),
                    family_title = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    @class = table.Column<long>(name: "class", type: "bigint", nullable: true),
                    class_title = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    commodity = table.Column<long>(type: "bigint", nullable: true),
                    commodity_title = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_supplier_category", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_supplier_category_is_active",
                schema: "supplier",
                table: "supplier_category",
                column: "is_active");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "supplier_category",
                schema: "supplier");
        }
    }
}

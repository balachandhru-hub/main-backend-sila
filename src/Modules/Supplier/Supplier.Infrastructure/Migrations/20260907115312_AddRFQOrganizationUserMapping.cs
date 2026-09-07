using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Supplier.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRFQOrganizationUserMapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "rfqorganization_user_mapping",
                schema: "supplier",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_rfqid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    rfqnumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    supplier_rfqid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    organization_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rfqorganization_user_mapping", x => x.id);
                    table.ForeignKey(
                        name: "fk_rfqorganization_user_mapping_supplier_rfq_supplier_rfqid",
                        column: x => x.supplier_rfqid,
                        principalSchema: "supplier",
                        principalTable: "supplier_rfq",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_rfqorganization_user_mapping_is_active",
                schema: "supplier",
                table: "rfqorganization_user_mapping",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_rfqorganization_user_mapping_supplier_rfqid",
                schema: "supplier",
                table: "rfqorganization_user_mapping",
                column: "supplier_rfqid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "rfqorganization_user_mapping",
                schema: "supplier");
        }
    }
}

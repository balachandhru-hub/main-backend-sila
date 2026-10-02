using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SilaMe.Api.Data;

#nullable disable

namespace SilaMe.Api.Migrations;

[DbContext(typeof(SilaMeDbContext))]
[Migration("20260916190000_SupplierMasterAndErpGrn")]
public partial class SupplierMasterAndErpGrn : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "EntityCode", table: "suppliers", type: "character varying(100)", nullable: false, defaultValue: "DEFAULT");
        migrationBuilder.AddColumn<string>(name: "LegalName", table: "suppliers", type: "character varying(250)", nullable: true);
        migrationBuilder.AddColumn<string>(name: "Address", table: "suppliers", type: "character varying(1000)", nullable: true);
        migrationBuilder.AddColumn<string>(name: "Country", table: "suppliers", type: "character varying(100)", nullable: true);
        migrationBuilder.AddColumn<string>(name: "CompanyCode", table: "suppliers", type: "character varying(100)", nullable: true);
        migrationBuilder.AddColumn<string>(name: "PurchasingOrganization", table: "suppliers", type: "character varying(100)", nullable: true);
        migrationBuilder.AddColumn<string>(name: "Currency", table: "suppliers", type: "character varying(10)", nullable: true);
        migrationBuilder.AddColumn<string>(name: "PaymentTerms", table: "suppliers", type: "character varying(100)", nullable: true);
        migrationBuilder.AddColumn<bool>(name: "IsBlocked", table: "suppliers", nullable: false, defaultValue: false);
        migrationBuilder.AddColumn<bool>(name: "IsDeleted", table: "suppliers", nullable: false, defaultValue: false);
        migrationBuilder.DropIndex(name: "IX_suppliers_OrganizationId_SupplierCode", table: "suppliers");
        migrationBuilder.CreateIndex(name: "IX_suppliers_OrganizationId_EntityCode_SupplierCode", table: "suppliers", columns: new[] { "OrganizationId", "EntityCode", "SupplierCode" }, unique: true);

        migrationBuilder.CreateTable(
            name: "supplier_aliases",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                Alias = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                NormalizedAlias = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                SourceSystem = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_supplier_aliases", x => x.Id);
                table.ForeignKey("FK_supplier_aliases_organizations_OrganizationId", x => x.OrganizationId, "organizations", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_supplier_aliases_suppliers_SupplierId", x => x.SupplierId, "suppliers", "Id", onDelete: ReferentialAction.Cascade);
            });
        migrationBuilder.CreateIndex(name: "IX_supplier_aliases_OrganizationId_NormalizedAlias", table: "supplier_aliases", columns: new[] { "OrganizationId", "NormalizedAlias" }, unique: true);
        migrationBuilder.CreateIndex(name: "IX_supplier_aliases_SupplierId", table: "supplier_aliases", column: "SupplierId");

        foreach (var column in new[] { "BusinessStatus", "ErpPostingStatus", "ErpMaterialDocument", "ErpDocumentYear" })
            migrationBuilder.AddColumn<string>(name: column, table: "goods_receipts", type: "character varying(100)", nullable: true);
        migrationBuilder.AddColumn<string>(name: "ErpResponseJson", table: "goods_receipts", type: "text", nullable: true);
        migrationBuilder.AddColumn<DateTime>(name: "ErpPostedAt", table: "goods_receipts", type: "timestamp with time zone", nullable: true);
        migrationBuilder.AddColumn<int>(name: "ErpAttemptCount", table: "goods_receipts", nullable: false, defaultValue: 0);
        migrationBuilder.AddColumn<DateTime>(name: "LastErpAttemptAt", table: "goods_receipts", type: "timestamp with time zone", nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder) => throw new NotSupportedException("This migration is additive and is not rolled back automatically.");
}
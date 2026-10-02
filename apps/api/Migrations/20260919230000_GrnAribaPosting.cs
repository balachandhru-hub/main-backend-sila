using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SilaMe.Api.Data;

#nullable disable

namespace SilaMe.Api.Migrations;

[DbContext(typeof(SilaMeDbContext))]
[Migration("20260919230000_GrnAribaPosting")]
public partial class GrnAribaPosting : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "SourceLastChangedAtRaw",
            table: "purchase_orders",
            type: "character varying(100)",
            maxLength: 100,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "AsnReference",
            table: "goods_receipts",
            type: "character varying(150)",
            maxLength: 150,
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "IntegrationRouteId",
            table: "goods_receipts",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "IntegrationConfigurationId",
            table: "goods_receipts",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ExternalSystem",
            table: "goods_receipts",
            type: "character varying(40)",
            maxLength: 40,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "SourceLastChangedAtRaw", table: "purchase_orders");
        migrationBuilder.DropColumn(name: "AsnReference", table: "goods_receipts");
        migrationBuilder.DropColumn(name: "IntegrationRouteId", table: "goods_receipts");
        migrationBuilder.DropColumn(name: "IntegrationConfigurationId", table: "goods_receipts");
        migrationBuilder.DropColumn(name: "ExternalSystem", table: "goods_receipts");
    }
}

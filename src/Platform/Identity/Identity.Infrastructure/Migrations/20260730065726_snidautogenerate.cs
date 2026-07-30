using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Identity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class snidautogenerate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "OrganizationSNSequence",
                schema: "identitysystem",
                startValue: 2L);

            migrationBuilder.AlterColumn<string>(
                name: "snid",
                schema: "identitysystem",
                table: "organizations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValueSql: "'SN' + RIGHT('00000000000' + CAST(NEXT VALUE FOR [identitysystem].[OrganizationSNSequence] AS VARCHAR(11)), 11)",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropSequence(
                name: "OrganizationSNSequence",
                schema: "identitysystem");

            migrationBuilder.AlterColumn<string>(
                name: "snid",
                schema: "identitysystem",
                table: "organizations",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldDefaultValueSql: "'SN' + RIGHT('00000000000' + CAST(NEXT VALUE FOR [identitysystem].[OrganizationSNSequence] AS VARCHAR(11)), 11)");
        }
    }
}

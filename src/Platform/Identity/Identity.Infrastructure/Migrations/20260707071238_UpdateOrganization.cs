using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Identity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateOrganization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
                migrationBuilder.DropColumn(
                name: "status",
                schema: "identitysystem",
                table: "organizations");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
             migrationBuilder.AddColumn<int>(
                name: "status",
                schema: "identitysystem",
                table: "organizations",
                type: "int",
                nullable: false,
                defaultValue: 0);

        }
    }
}

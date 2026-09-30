using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MasterData.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailCredentialsToApiConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "client_id",
                schema: "masterdata",
                table: "api_configs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "client_secret",
                schema: "masterdata",
                table: "api_configs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "tenant_id",
                schema: "masterdata",
                table: "api_configs",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "client_id",
                schema: "masterdata",
                table: "api_configs");

            migrationBuilder.DropColumn(
                name: "client_secret",
                schema: "masterdata",
                table: "api_configs");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                schema: "masterdata",
                table: "api_configs");
        }
    }
}

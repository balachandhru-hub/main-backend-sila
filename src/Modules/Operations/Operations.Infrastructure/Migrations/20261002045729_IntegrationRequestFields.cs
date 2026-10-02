using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Operations.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class IntegrationRequestFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "api_key_header",
                schema: "operations",
                table: "api_integration_configuration",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "headers_json",
                schema: "operations",
                table: "api_integration_configuration",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "http_method",
                schema: "operations",
                table: "api_integration_configuration",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "GET");

            migrationBuilder.AddColumn<string>(
                name: "payload_format",
                schema: "operations",
                table: "api_integration_configuration",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "JSON");

            migrationBuilder.AddColumn<string>(
                name: "protected_api_key",
                schema: "operations",
                table: "api_integration_configuration",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "request_body",
                schema: "operations",
                table: "api_integration_configuration",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "system_name",
                schema: "operations",
                table: "api_integration_configuration",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "api_key_header",
                schema: "operations",
                table: "api_integration_configuration");

            migrationBuilder.DropColumn(
                name: "headers_json",
                schema: "operations",
                table: "api_integration_configuration");

            migrationBuilder.DropColumn(
                name: "http_method",
                schema: "operations",
                table: "api_integration_configuration");

            migrationBuilder.DropColumn(
                name: "payload_format",
                schema: "operations",
                table: "api_integration_configuration");

            migrationBuilder.DropColumn(
                name: "protected_api_key",
                schema: "operations",
                table: "api_integration_configuration");

            migrationBuilder.DropColumn(
                name: "request_body",
                schema: "operations",
                table: "api_integration_configuration");

            migrationBuilder.DropColumn(
                name: "system_name",
                schema: "operations",
                table: "api_integration_configuration");
        }
    }
}

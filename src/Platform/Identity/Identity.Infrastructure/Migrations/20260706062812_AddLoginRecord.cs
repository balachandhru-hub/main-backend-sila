using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Identity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLoginRecord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "access_token",
                schema: "identitysystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    access_token_value = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    expired_time = table.Column<DateTime>(type: "datetime2", nullable: false),
                    refresh_token = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_access_token", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "login_record",
                schema: "identitysystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    attribute_id = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    organization_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    login_time = table.Column<DateTime>(type: "datetime2", nullable: false),
                    entity_type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ip_address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    user_name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_login_record", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_access_token_is_active",
                schema: "identitysystem",
                table: "access_token",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_login_record_is_active",
                schema: "identitysystem",
                table: "login_record",
                column: "is_active");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "access_token",
                schema: "identitysystem");

            migrationBuilder.DropTable(
                name: "login_record",
                schema: "identitysystem");
        }
    }
}

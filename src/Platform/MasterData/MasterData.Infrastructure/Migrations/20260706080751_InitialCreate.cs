using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MasterData.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "masterdata");

            migrationBuilder.CreateTable(
                name: "api_configs",
                schema: "masterdata",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    base_url = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    username = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    password = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_api_configs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "email_cclists",
                schema: "masterdata",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    entity_type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_email_cclists", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "email_contents",
                schema: "masterdata",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    key = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    subject = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_email_contents", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "email_failed_details",
                schema: "masterdata",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    entity_type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    entity_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    email_type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    trigger_count = table.Column<int>(type: "int", nullable: false),
                    subject = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    body = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_email_failed_details", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "email_sent_details",
                schema: "masterdata",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    entity_type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    entity_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    email_type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_email_sent_details", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "unspsc_categories",
                schema: "masterdata",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    version = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    key = table.Column<int>(type: "int", nullable: false),
                    segment = table.Column<long>(type: "bigint", nullable: false),
                    segment_title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    segment_definition = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    family = table.Column<long>(type: "bigint", nullable: true),
                    family_title = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    family_definition = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    @class = table.Column<long>(name: "class", type: "bigint", nullable: true),
                    class_title = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    class_definition = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    commodity = table.Column<long>(type: "bigint", nullable: true),
                    commodity_title = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    commodity_definition = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    synonym = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    acronym = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_unspsc_categories", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_api_configs_is_active",
                schema: "masterdata",
                table: "api_configs",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_email_cclists_is_active",
                schema: "masterdata",
                table: "email_cclists",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_email_contents_is_active",
                schema: "masterdata",
                table: "email_contents",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_email_failed_details_is_active",
                schema: "masterdata",
                table: "email_failed_details",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_email_sent_details_is_active",
                schema: "masterdata",
                table: "email_sent_details",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_unspsc_categories_is_active_segment_family_class_commodity",
                schema: "masterdata",
                table: "unspsc_categories",
                columns: new[] { "is_active", "segment", "family", "class", "commodity" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "api_configs",
                schema: "masterdata");

            migrationBuilder.DropTable(
                name: "email_cclists",
                schema: "masterdata");

            migrationBuilder.DropTable(
                name: "email_contents",
                schema: "masterdata");

            migrationBuilder.DropTable(
                name: "email_failed_details",
                schema: "masterdata");

            migrationBuilder.DropTable(
                name: "email_sent_details",
                schema: "masterdata");

            migrationBuilder.DropTable(
                name: "unspsc_categories",
                schema: "masterdata");
        }
    }
}

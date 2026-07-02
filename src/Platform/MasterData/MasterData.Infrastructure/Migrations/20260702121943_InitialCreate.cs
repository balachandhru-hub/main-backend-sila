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
            migrationBuilder.CreateTable(
                name: "unspsc_categories",
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
                name: "ix_unspsc_categories_is_active_segment_family_class_commodity",
                table: "unspsc_categories",
                columns: new[] { "is_active", "segment", "family", "class", "commodity" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "unspsc_categories");
        }
    }
}

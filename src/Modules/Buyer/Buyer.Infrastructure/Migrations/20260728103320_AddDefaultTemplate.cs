using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Buyer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDefaultTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "default_verification_template",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    template_code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    template_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_default_verification_template", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "default_verification_template_question",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    default_verification_template_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    question = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    question_key = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    question_type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    display_order = table.Column<int>(type: "int", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_default_verification_template_question", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_default_verification_template_is_active",
                schema: "buyersystem",
                table: "default_verification_template",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_default_verification_template_question_is_active",
                schema: "buyersystem",
                table: "default_verification_template_question",
                column: "is_active");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "default_verification_template",
                schema: "buyersystem");

            migrationBuilder.DropTable(
                name: "default_verification_template_question",
                schema: "buyersystem");
        }
    }
}

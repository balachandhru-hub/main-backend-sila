using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Buyer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class message : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "message_thread",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    rfqid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    rfqnumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    last_message_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_message_thread", x => x.id);
                    table.ForeignKey(
                        name: "fk_message_thread_rfq_rfqid",
                        column: x => x.rfqid,
                        principalSchema: "buyersystem",
                        principalTable: "rfq",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "message",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    thread_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    sender_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    sender_organization_type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    sender_organization_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    body = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    is_read_by_buyer = table.Column<bool>(type: "bit", nullable: false),
                    is_read_by_supplier = table.Column<bool>(type: "bit", nullable: false),
                    read_by_buyer_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    read_by_supplier_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_message", x => x.id);
                    table.ForeignKey(
                        name: "fk_message_message_thread_thread_id",
                        column: x => x.thread_id,
                        principalSchema: "buyersystem",
                        principalTable: "message_thread",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "message_attachment",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    message_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    file_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    content_type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    file_size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    storage_path = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_message_attachment", x => x.id);
                    table.ForeignKey(
                        name: "fk_message_attachment_message_message_id",
                        column: x => x.message_id,
                        principalSchema: "buyersystem",
                        principalTable: "message",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_message_is_active",
                schema: "buyersystem",
                table: "message",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_message_thread_id",
                schema: "buyersystem",
                table: "message",
                column: "thread_id");

            migrationBuilder.CreateIndex(
                name: "ix_message_attachment_is_active",
                schema: "buyersystem",
                table: "message_attachment",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_message_attachment_message_id",
                schema: "buyersystem",
                table: "message_attachment",
                column: "message_id");

            migrationBuilder.CreateIndex(
                name: "ix_message_thread_is_active",
                schema: "buyersystem",
                table: "message_thread",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_message_thread_rfqid",
                schema: "buyersystem",
                table: "message_thread",
                column: "rfqid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "message_attachment",
                schema: "buyersystem");

            migrationBuilder.DropTable(
                name: "message",
                schema: "buyersystem");

            migrationBuilder.DropTable(
                name: "message_thread",
                schema: "buyersystem");
        }
    }
}

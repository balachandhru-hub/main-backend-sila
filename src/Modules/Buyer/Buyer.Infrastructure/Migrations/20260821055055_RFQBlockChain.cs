using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Buyer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RFQBlockChain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "rfqblockchain_record",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    rfqid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    entity_type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    event_type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    data_hash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    blockchain_transaction_id = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    block_number = table.Column<long>(type: "bigint", nullable: true),
                    blockchain_network = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rfqblockchain_record", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_rfqblockchain_record_is_active",
                schema: "buyersystem",
                table: "rfqblockchain_record",
                column: "is_active");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "rfqblockchain_record",
                schema: "buyersystem");
        }
    }
}

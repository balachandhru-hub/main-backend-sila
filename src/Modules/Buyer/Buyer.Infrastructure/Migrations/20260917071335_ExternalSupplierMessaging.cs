using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Buyer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ExternalSupplierMessaging : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "supplier_id",
                schema: "buyersystem",
                table: "message_thread",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<Guid>(
                name: "external_supplier_id",
                schema: "buyersystem",
                table: "message_thread",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "sender_user_id",
                schema: "buyersystem",
                table: "message",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.CreateIndex(
                name: "ix_message_thread_external_supplier_id",
                schema: "buyersystem",
                table: "message_thread",
                column: "external_supplier_id");

            migrationBuilder.AddForeignKey(
                name: "fk_message_thread_external_supplier_external_supplier_id",
                schema: "buyersystem",
                table: "message_thread",
                column: "external_supplier_id",
                principalSchema: "buyersystem",
                principalTable: "external_supplier",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_message_thread_external_supplier_external_supplier_id",
                schema: "buyersystem",
                table: "message_thread");

            migrationBuilder.DropIndex(
                name: "ix_message_thread_external_supplier_id",
                schema: "buyersystem",
                table: "message_thread");

            migrationBuilder.DropColumn(
                name: "external_supplier_id",
                schema: "buyersystem",
                table: "message_thread");

            migrationBuilder.AlterColumn<Guid>(
                name: "supplier_id",
                schema: "buyersystem",
                table: "message_thread",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "sender_user_id",
                schema: "buyersystem",
                table: "message",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);
        }
    }
}

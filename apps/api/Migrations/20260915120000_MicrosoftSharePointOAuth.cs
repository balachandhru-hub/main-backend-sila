using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SilaMe.Api.Data;

#nullable disable

namespace SilaMe.Api.Migrations;

[DbContext(typeof(SilaMeDbContext))]
[Migration("20260915120000_MicrosoftSharePointOAuth")]
public partial class MicrosoftSharePointOAuth : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "CredentialReference",
            table: "document_storage_connections",
            type: "text",
            nullable: true,
            oldClrType: typeof(string),
            oldType: "character varying(500)",
            oldMaxLength: 500);
        migrationBuilder.AddColumn<string>(
            name: "DriveName",
            table: "document_storage_connections",
            type: "character varying(200)",
            maxLength: 200,
            nullable: true);
        migrationBuilder.AddColumn<string>(
            name: "FolderPath",
            table: "document_storage_connections",
            type: "character varying(1000)",
            maxLength: 1000,
            nullable: true);
        migrationBuilder.AddColumn<DateTime>(
            name: "ConnectedAt",
            table: "document_storage_connections",
            type: "timestamp with time zone",
            nullable: true);
        migrationBuilder.AddColumn<DateTime>(
            name: "LastTestedAt",
            table: "document_storage_connections",
            type: "timestamp with time zone",
            nullable: true);
        migrationBuilder.AddColumn<string>(
            name: "LastTestStatus",
            table: "document_storage_connections",
            type: "character varying(100)",
            maxLength: 100,
            nullable: true);

        migrationBuilder.CreateTable(
            name: "microsoft_authorization_states",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                StateHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                ReturnUrl = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                DraftJson = table.Column<string>(type: "text", nullable: false),
                ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_microsoft_authorization_states", x => x.Id);
                table.ForeignKey(
                    name: "FK_microsoft_authorization_states_organizations_OrganizationId",
                    column: x => x.OrganizationId,
                    principalTable: "organizations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_microsoft_authorization_states_users_UserId",
                    column: x => x.UserId,
                    principalTable: "users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_microsoft_authorization_states_StateHash",
            table: "microsoft_authorization_states",
            column: "StateHash",
            unique: true);
        migrationBuilder.CreateIndex(
            name: "IX_microsoft_authorization_states_UserId_ExpiresAt",
            table: "microsoft_authorization_states",
            columns: new[] { "UserId", "ExpiresAt" });
        migrationBuilder.CreateIndex(
            name: "IX_microsoft_authorization_states_OrganizationId",
            table: "microsoft_authorization_states",
            column: "OrganizationId");
        migrationBuilder.CreateIndex(
            name: "IX_microsoft_authorization_states_UserId",
            table: "microsoft_authorization_states",
            column: "UserId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "microsoft_authorization_states");
        migrationBuilder.DropColumn(name: "DriveName", table: "document_storage_connections");
        migrationBuilder.DropColumn(name: "FolderPath", table: "document_storage_connections");
        migrationBuilder.DropColumn(name: "ConnectedAt", table: "document_storage_connections");
        migrationBuilder.DropColumn(name: "LastTestedAt", table: "document_storage_connections");
        migrationBuilder.DropColumn(name: "LastTestStatus", table: "document_storage_connections");
    }
}
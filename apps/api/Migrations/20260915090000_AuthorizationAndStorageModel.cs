using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SilaMe.Api.Data;

#nullable disable

namespace SilaMe.Api.Migrations;

[DbContext(typeof(SilaMeDbContext))]
[Migration("20260915090000_AuthorizationAndStorageModel")]
public partial class AuthorizationAndStorageModel : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "FirstName",
            table: "users",
            type: "character varying(100)",
            maxLength: 100,
            nullable: true);
        migrationBuilder.AddColumn<string>(
            name: "LastName",
            table: "users",
            type: "character varying(100)",
            maxLength: 100,
            nullable: true);
        migrationBuilder.AddColumn<string>(
            name: "MobileNumber",
            table: "users",
            type: "character varying(40)",
            maxLength: 40,
            nullable: true);
        migrationBuilder.AddColumn<bool>(
            name: "MustChangePassword",
            table: "users",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<string>(
            name: "ApplicationScope",
            table: "roles",
            type: "character varying(16)",
            maxLength: 16,
            nullable: false,
            defaultValue: "BOTH");

        migrationBuilder.AddColumn<string>(
            name: "Module",
            table: "permissions",
            type: "character varying(100)",
            maxLength: 100,
            nullable: false,
            defaultValue: "Administration");
        migrationBuilder.AddColumn<string>(
            name: "SubModule",
            table: "permissions",
            type: "character varying(100)",
            maxLength: 100,
            nullable: true);
        migrationBuilder.AddColumn<string>(
            name: "ApplicationScope",
            table: "permissions",
            type: "character varying(16)",
            maxLength: 16,
            nullable: false,
            defaultValue: "BOTH");
        migrationBuilder.AddColumn<string>(
            name: "RiskLevel",
            table: "permissions",
            type: "character varying(16)",
            maxLength: 16,
            nullable: false,
            defaultValue: "LOW");
        migrationBuilder.AddColumn<bool>(
            name: "IsSystemAuthorization",
            table: "permissions",
            type: "boolean",
            nullable: false,
            defaultValue: true);
        migrationBuilder.AddColumn<bool>(
            name: "IsActive",
            table: "permissions",
            type: "boolean",
            nullable: false,
            defaultValue: true);

        migrationBuilder.AddColumn<string>(
            name: "OldStateJson",
            table: "audit_events",
            type: "text",
            nullable: true);
        migrationBuilder.AddColumn<string>(
            name: "NewStateJson",
            table: "audit_events",
            type: "text",
            nullable: true);
        migrationBuilder.AddColumn<Guid>(
            name: "ChangedByUserId",
            table: "audit_events",
            type: "uuid",
            nullable: true);
        migrationBuilder.AddColumn<string>(
            name: "Reason",
            table: "audit_events",
            type: "character varying(500)",
            maxLength: 500,
            nullable: true);
        migrationBuilder.AddColumn<string>(
            name: "Result",
            table: "audit_events",
            type: "character varying(30)",
            maxLength: 30,
            nullable: true);

        migrationBuilder.CreateTable(
            name: "user_authorization_overrides",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                AuthorizationId = table.Column<Guid>(type: "uuid", nullable: false),
                OverrideType = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                EffectiveFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                EffectiveTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_user_authorization_overrides", x => x.Id);
                table.ForeignKey("FK_user_authorization_overrides_permissions_AuthorizationId", x => x.AuthorizationId, "permissions", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_user_authorization_overrides_users_CreatedByUserId", x => x.CreatedByUserId, "users", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_user_authorization_overrides_users_UserId", x => x.UserId, "users", "Id", onDelete: ReferentialAction.Cascade);
            });
        migrationBuilder.CreateIndex(
            name: "IX_user_authorization_overrides_UserId_AuthorizationId",
            table: "user_authorization_overrides",
            columns: new[] { "UserId", "AuthorizationId" },
            unique: true);
        migrationBuilder.CreateIndex(name: "IX_user_authorization_overrides_AuthorizationId", table: "user_authorization_overrides", column: "AuthorizationId");
        migrationBuilder.CreateIndex(name: "IX_user_authorization_overrides_CreatedByUserId", table: "user_authorization_overrides", column: "CreatedByUserId");

        migrationBuilder.CreateTable(
            name: "document_storage_connections",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                Provider = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                ConnectionStatus = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                TenantIdentifier = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                SiteIdentifier = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                DriveIdentifier = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                FolderIdentifier = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                DisplayUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                DisplayName = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                CredentialReference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                ValidatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                ValidatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_document_storage_connections", x => x.Id);
                table.ForeignKey("FK_document_storage_connections_organizations_OrganizationId", x => x.OrganizationId, "organizations", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_document_storage_connections_users_CreatedByUserId", x => x.CreatedByUserId, "users", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_document_storage_connections_users_ValidatedByUserId", x => x.ValidatedByUserId, "users", "Id", onDelete: ReferentialAction.Restrict);
            });
        migrationBuilder.CreateIndex(name: "IX_document_storage_connections_OrganizationId_Provider_Name", table: "document_storage_connections", columns: new[] { "OrganizationId", "Provider", "Name" }, unique: true);
        migrationBuilder.CreateIndex(name: "IX_document_storage_connections_CreatedByUserId", table: "document_storage_connections", column: "CreatedByUserId");
        migrationBuilder.CreateIndex(name: "IX_document_storage_connections_ValidatedByUserId", table: "document_storage_connections", column: "ValidatedByUserId");

        migrationBuilder.CreateTable(
            name: "user_document_storage_assignments",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                Provider = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                StorageConnectionId = table.Column<Guid>(type: "uuid", nullable: true),
                SiteIdentifier = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                DriveIdentifier = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                FolderIdentifier = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                DestinationUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                ExternalTransferEnabled = table.Column<bool>(type: "boolean", nullable: false),
                Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                ValidatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_user_document_storage_assignments", x => x.Id);
                table.ForeignKey("FK_user_document_storage_assignments_document_storage_connections_StorageConnectionId", x => x.StorageConnectionId, "document_storage_connections", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_user_document_storage_assignments_users_CreatedByUserId", x => x.CreatedByUserId, "users", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_user_document_storage_assignments_users_UserId", x => x.UserId, "users", "Id", onDelete: ReferentialAction.Cascade);
            });
        migrationBuilder.CreateIndex(name: "IX_user_document_storage_assignments_UserId", table: "user_document_storage_assignments", column: "UserId", unique: true);
        migrationBuilder.CreateIndex(name: "IX_user_document_storage_assignments_StorageConnectionId", table: "user_document_storage_assignments", column: "StorageConnectionId");
        migrationBuilder.CreateIndex(name: "IX_user_document_storage_assignments_CreatedByUserId", table: "user_document_storage_assignments", column: "CreatedByUserId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "user_document_storage_assignments");
        migrationBuilder.DropTable(name: "user_authorization_overrides");
        migrationBuilder.DropTable(name: "document_storage_connections");
        migrationBuilder.DropColumn(name: "FirstName", table: "users");
        migrationBuilder.DropColumn(name: "LastName", table: "users");
        migrationBuilder.DropColumn(name: "MobileNumber", table: "users");
        migrationBuilder.DropColumn(name: "MustChangePassword", table: "users");
        migrationBuilder.DropColumn(name: "ApplicationScope", table: "roles");
        migrationBuilder.DropColumn(name: "Module", table: "permissions");
        migrationBuilder.DropColumn(name: "SubModule", table: "permissions");
        migrationBuilder.DropColumn(name: "ApplicationScope", table: "permissions");
        migrationBuilder.DropColumn(name: "RiskLevel", table: "permissions");
        migrationBuilder.DropColumn(name: "IsSystemAuthorization", table: "permissions");
        migrationBuilder.DropColumn(name: "IsActive", table: "permissions");
        migrationBuilder.DropColumn(name: "OldStateJson", table: "audit_events");
        migrationBuilder.DropColumn(name: "NewStateJson", table: "audit_events");
        migrationBuilder.DropColumn(name: "ChangedByUserId", table: "audit_events");
        migrationBuilder.DropColumn(name: "Reason", table: "audit_events");
        migrationBuilder.DropColumn(name: "Result", table: "audit_events");
    }
}
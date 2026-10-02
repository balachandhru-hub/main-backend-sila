using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SilaMe.Api.Data;

#nullable disable

namespace SilaMe.Api.Migrations;

[DbContext(typeof(SilaMeDbContext))]
[Migration("20260916100000_DocumentTransferRouting")]
public partial class DocumentTransferRouting : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "DocumentStorageDestinationId",
            table: "user_document_storage_assignments",
            type: "uuid",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "document_storage_destinations",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                PropertyId = table.Column<Guid>(type: "uuid", nullable: true),
                OperatingUnitId = table.Column<Guid>(type: "uuid", nullable: true),
                LocationId = table.Column<Guid>(type: "uuid", nullable: true),
                StorageConnectionId = table.Column<Guid>(type: "uuid", nullable: false),
                Provider = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                DocumentType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                SiteIdentifier = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                DriveIdentifier = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                FolderIdentifier = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                FolderPath = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                DisplayUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                ExternalTransferEnabled = table.Column<bool>(type: "boolean", nullable: false),
                Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                ValidatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_document_storage_destinations", x => x.Id);
                table.ForeignKey(
                    name: "FK_document_storage_destinations_organizations_OrganizationId",
                    column: x => x.OrganizationId,
                    principalTable: "organizations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_document_storage_destinations_organization_units_OperatingUnitId",
                    column: x => x.OperatingUnitId,
                    principalTable: "organization_units",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_document_storage_destinations_document_storage_connections_StorageConnectionId",
                    column: x => x.StorageConnectionId,
                    principalTable: "document_storage_connections",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_document_storage_destinations_users_CreatedByUserId",
                    column: x => x.CreatedByUserId,
                    principalTable: "users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "document_transfer_jobs",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                DestinationId = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                PropertyId = table.Column<Guid>(type: "uuid", nullable: true),
                OperatingUnitId = table.Column<Guid>(type: "uuid", nullable: true),
                DocumentType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                Provider = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                ResolutionSource = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                AttemptCount = table.Column<int>(type: "integer", nullable: false),
                NextAttemptAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                LastAttemptAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                ExternalFileId = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                ExternalWebUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                ExternalFileName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                LastErrorCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                LastErrorMessageSafe = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_document_transfer_jobs", x => x.Id);
                table.ForeignKey(
                    name: "FK_document_transfer_jobs_documents_DocumentId",
                    column: x => x.DocumentId,
                    principalTable: "documents",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_document_transfer_jobs_document_storage_destinations_DestinationId",
                    column: x => x.DestinationId,
                    principalTable: "document_storage_destinations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_document_transfer_jobs_users_UserId",
                    column: x => x.UserId,
                    principalTable: "users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_document_transfer_jobs_organizations_OrganizationId",
                    column: x => x.OrganizationId,
                    principalTable: "organizations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex("IX_user_document_storage_assignments_DocumentStorageDestinationId", "user_document_storage_assignments", "DocumentStorageDestinationId");
        migrationBuilder.CreateIndex("IX_document_storage_destinations_OrganizationId_StorageConnectionId_DocumentType_OperatingUnitId", "document_storage_destinations", new[] { "OrganizationId", "StorageConnectionId", "DocumentType", "OperatingUnitId" }, unique: true);
        migrationBuilder.CreateIndex("IX_document_storage_destinations_OrganizationId_DocumentType_Status_ExternalTransferEnabled", "document_storage_destinations", new[] { "OrganizationId", "DocumentType", "Status", "ExternalTransferEnabled" });
        migrationBuilder.CreateIndex("IX_document_storage_destinations_OperatingUnitId", "document_storage_destinations", "OperatingUnitId");
        migrationBuilder.CreateIndex("IX_document_storage_destinations_StorageConnectionId", "document_storage_destinations", "StorageConnectionId");
        migrationBuilder.CreateIndex("IX_document_storage_destinations_CreatedByUserId", "document_storage_destinations", "CreatedByUserId");
        migrationBuilder.CreateIndex("IX_document_transfer_jobs_DocumentId_DestinationId", "document_transfer_jobs", new[] { "DocumentId", "DestinationId" }, unique: true);
        migrationBuilder.CreateIndex("IX_document_transfer_jobs_Status_NextAttemptAt", "document_transfer_jobs", new[] { "Status", "NextAttemptAt" });
        migrationBuilder.CreateIndex("IX_document_transfer_jobs_DestinationId", "document_transfer_jobs", "DestinationId");
        migrationBuilder.CreateIndex("IX_document_transfer_jobs_UserId", "document_transfer_jobs", "UserId");
        migrationBuilder.CreateIndex("IX_document_transfer_jobs_OrganizationId", "document_transfer_jobs", "OrganizationId");
        migrationBuilder.AddForeignKey(
            name: "FK_user_document_storage_assignments_document_storage_destinations_DocumentStorageDestinationId",
            table: "user_document_storage_assignments",
            column: "DocumentStorageDestinationId",
            principalTable: "document_storage_destinations",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey("FK_user_document_storage_assignments_document_storage_destinations_DocumentStorageDestinationId", "user_document_storage_assignments");
        migrationBuilder.DropTable("document_transfer_jobs");
        migrationBuilder.DropTable("document_storage_destinations");
        migrationBuilder.DropIndex("IX_user_document_storage_assignments_DocumentStorageDestinationId", "user_document_storage_assignments");
        migrationBuilder.DropColumn("DocumentStorageDestinationId", "user_document_storage_assignments");
    }
}
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using SilaMe.Api.Data;

#nullable disable

namespace SilaMe.Api.Migrations;

[DbContext(typeof(SilaMeDbContext))]
[Migration("20260916180000_ApiIntegrationFramework")]
public partial class ApiIntegrationFramework : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "EntityCode", table: "purchase_orders", type: "character varying(100)",
            nullable: false, defaultValue: "DEFAULT");
        migrationBuilder.AddColumn<Guid>(name: "SourceConfigurationId", table: "purchase_orders", nullable: true);
        migrationBuilder.AddColumn<DateTime>(name: "SourceLastChangedAt", table: "purchase_orders", nullable: true);
        migrationBuilder.AddColumn<DateTime>(name: "LastSyncedAt", table: "purchase_orders", nullable: true);
        migrationBuilder.AddColumn<string>(name: "SourceHash", table: "purchase_orders", type: "character varying(128)", nullable: true);
        migrationBuilder.AddColumn<string>(name: "ExternalId", table: "purchase_order_items", type: "character varying(250)", nullable: true);
        migrationBuilder.AddColumn<DateTime>(name: "SourceLastChangedAt", table: "purchase_order_items", nullable: true);
        migrationBuilder.AddColumn<DateTime>(name: "LastSyncedAt", table: "purchase_order_items", nullable: true);
        migrationBuilder.AddColumn<string>(name: "SourceHash", table: "purchase_order_items", type: "character varying(128)", nullable: true);
        migrationBuilder.DropIndex(name: "IX_purchase_orders_OrganizationId_PoNumber", table: "purchase_orders");
        migrationBuilder.CreateIndex(name: "IX_purchase_orders_OrganizationId_EntityCode_PoNumber", table: "purchase_orders", columns: new[] { "OrganizationId", "EntityCode", "PoNumber" }, unique: true);
        migrationBuilder.CreateIndex(name: "IX_purchase_orders_OrganizationId_SourceConfigurationId_ExternalId", table: "purchase_orders", columns: new[] { "OrganizationId", "SourceConfigurationId", "ExternalId" }, unique: true, filter: "\"SourceConfigurationId\" IS NOT NULL AND \"ExternalId\" IS NOT NULL");

        migrationBuilder.CreateTable("api_integration_configurations",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                OrganizationId = table.Column<Guid>(nullable: false),
                OrganizationUnitId = table.Column<Guid>(nullable: true),
                EntityCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                ProcessType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                Protocol = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                BaseUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                ResourcePath = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                AuthenticationType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                Username = table.Column<string>(nullable: true),
                ProtectedPassword = table.Column<string>(type: "text", nullable: true),
                ProtectedClientId = table.Column<string>(nullable: true),
                ProtectedClientSecret = table.Column<string>(type: "text", nullable: true),
                ProtectedBearerToken = table.Column<string>(type: "text", nullable: true),
                TokenEndpoint = table.Column<string>(nullable: true),
                TokenScope = table.Column<string>(nullable: true),
                TokenHeadersJson = table.Column<string>(type: "text", nullable: true),
                TokenBodyJson = table.Column<string>(type: "text", nullable: true),
                TimeoutSeconds = table.Column<int>(nullable: false),
                RetryCount = table.Column<int>(nullable: false),
                PageSize = table.Column<int>(nullable: true),
                WatermarkField = table.Column<string>(nullable: true),
                LastWatermark = table.Column<DateTime>(nullable: true),
                LastAttemptAt = table.Column<DateTime>(nullable: true),
                LastSuccessfulRunAt = table.Column<DateTime>(nullable: true),
                NextRunAt = table.Column<DateTime>(nullable: true),
                IsRunning = table.Column<bool>(nullable: false),
                RunningSince = table.Column<DateTime>(nullable: true),
                LastErrorSafe = table.Column<string>(nullable: true),
                ScheduleCron = table.Column<string>(nullable: true),
                Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                TestedAt = table.Column<DateTime>(nullable: true),
                CreatedAt = table.Column<DateTime>(nullable: false),
                UpdatedAt = table.Column<DateTime>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_api_integration_configurations", x => x.Id);
                table.ForeignKey("FK_api_integration_configurations_organization_units_OrganizationUnitId", x => x.OrganizationUnitId, "organization_units", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_api_integration_configurations_organizations_OrganizationId", x => x.OrganizationId, "organizations", "Id", onDelete: ReferentialAction.Cascade);
            });
        migrationBuilder.CreateIndex("IX_api_integration_configurations_OrganizationId_EntityCode_ProcessType", "api_integration_configurations", new[] { "OrganizationId", "EntityCode", "ProcessType" }, unique: true);
        migrationBuilder.CreateIndex("IX_api_integration_configurations_OrganizationId_Status_NextRunAt", "api_integration_configurations", new[] { "OrganizationId", "Status", "NextRunAt" });

        migrationBuilder.CreateTable("integration_schema_snapshots",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false), ConfigurationId = table.Column<Guid>(nullable: false),
                MetadataUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                SchemaJson = table.Column<string>(type: "text", nullable: false), DiscoveredAt = table.Column<DateTime>(nullable: false)
            }, constraints: table => { table.PrimaryKey("PK_integration_schema_snapshots", x => x.Id); table.ForeignKey("FK_integration_schema_snapshots_api_integration_configurations_ConfigurationId", x => x.ConfigurationId, "api_integration_configurations", "Id", onDelete: ReferentialAction.Cascade); });
        migrationBuilder.CreateIndex("IX_integration_schema_snapshots_ConfigurationId_DiscoveredAt", "integration_schema_snapshots", new[] { "ConfigurationId", "DiscoveredAt" });

        migrationBuilder.CreateTable("api_field_mappings",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false), ConfigurationId = table.Column<Guid>(nullable: false),
                SourceField = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                TargetField = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                Transformation = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                NullPolicy = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                DefaultValue = table.Column<string>(nullable: true), IsValidated = table.Column<bool>(nullable: false), UpdatedAt = table.Column<DateTime>(nullable: false)
            }, constraints: table => { table.PrimaryKey("PK_api_field_mappings", x => x.Id); table.ForeignKey("FK_api_field_mappings_api_integration_configurations_ConfigurationId", x => x.ConfigurationId, "api_integration_configurations", "Id", onDelete: ReferentialAction.Cascade); });
        migrationBuilder.CreateIndex("IX_api_field_mappings_ConfigurationId_SourceField_TargetField", "api_field_mappings", new[] { "ConfigurationId", "SourceField", "TargetField" }, unique: true);

        migrationBuilder.CreateTable("api_integration_executions",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false), ConfigurationId = table.Column<Guid>(nullable: false),
                Trigger = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                StartedAt = table.Column<DateTime>(nullable: false), CompletedAt = table.Column<DateTime>(nullable: true),
                RecordsRead = table.Column<int>(nullable: false), RecordsCreated = table.Column<int>(nullable: false), RecordsUpdated = table.Column<int>(nullable: false), RecordsFailed = table.Column<int>(nullable: false),
                WatermarkBefore = table.Column<DateTime>(nullable: true), WatermarkAfter = table.Column<DateTime>(nullable: true),
                ErrorCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                ErrorMessageSafe = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true), DetailJson = table.Column<string>(type: "text", nullable: true)
            }, constraints: table => { table.PrimaryKey("PK_api_integration_executions", x => x.Id); table.ForeignKey("FK_api_integration_executions_api_integration_configurations_ConfigurationId", x => x.ConfigurationId, "api_integration_configurations", "Id", onDelete: ReferentialAction.Cascade); });
        migrationBuilder.CreateIndex("IX_api_integration_executions_ConfigurationId_StartedAt", "api_integration_executions", new[] { "ConfigurationId", "StartedAt" });
    }

    protected override void Down(MigrationBuilder migrationBuilder) { throw new NotSupportedException("The integration framework migration is additive and is not rolled back automatically."); }
}
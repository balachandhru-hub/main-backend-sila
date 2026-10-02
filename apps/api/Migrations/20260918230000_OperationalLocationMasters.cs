using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SilaMe.Api.Data;

#nullable disable

namespace SilaMe.Api.Migrations;

[DbContext(typeof(SilaMeDbContext))]
[Migration("20260918230000_OperationalLocationMasters")]
public partial class OperationalLocationMasters : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE IF NOT EXISTS company_code_masters (
                "Id" uuid NOT NULL,
                "OrganizationId" uuid NOT NULL,
                "CompanyCode" character varying(40) NOT NULL,
                "CompanyName" character varying(250) NOT NULL,
                "Country" character varying(100),
                "Currency" character varying(16),
                "Address" character varying(500),
                "City" character varying(150),
                "TaxRegistrationNumber" character varying(80),
                "SourceSystem" character varying(80),
                "ExternalId" character varying(80),
                "Status" character varying(16) NOT NULL,
                "IsDeleted" boolean NOT NULL DEFAULT FALSE,
                "CreatedAt" timestamp with time zone NOT NULL,
                "UpdatedAt" timestamp with time zone NOT NULL,
                CONSTRAINT "PK_company_code_masters" PRIMARY KEY ("Id"),
                CONSTRAINT "FK_company_code_masters_organizations_OrganizationId" FOREIGN KEY ("OrganizationId") REFERENCES organizations ("Id") ON DELETE CASCADE
            );
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_company_code_masters_OrganizationId_CompanyCode" ON company_code_masters ("OrganizationId", "CompanyCode");

            CREATE TABLE IF NOT EXISTS property_masters (
                "Id" uuid NOT NULL,
                "OrganizationId" uuid NOT NULL,
                "PropertyCode" character varying(40) NOT NULL,
                "PropertyName" character varying(250) NOT NULL,
                "Country" character varying(100),
                "CompanyCode" character varying(40),
                "Status" character varying(16) NOT NULL,
                "IsDeleted" boolean NOT NULL DEFAULT FALSE,
                "CreatedAt" timestamp with time zone NOT NULL,
                "UpdatedAt" timestamp with time zone NOT NULL,
                CONSTRAINT "PK_property_masters" PRIMARY KEY ("Id"),
                CONSTRAINT "FK_property_masters_organizations_OrganizationId" FOREIGN KEY ("OrganizationId") REFERENCES organizations ("Id") ON DELETE CASCADE
            );
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_property_masters_OrganizationId_PropertyCode" ON property_masters ("OrganizationId", "PropertyCode");

            CREATE TABLE IF NOT EXISTS plant_masters (
                "Id" uuid NOT NULL,
                "OrganizationId" uuid NOT NULL,
                "PlantCode" character varying(40) NOT NULL,
                "PlantName" character varying(250) NOT NULL,
                "CompanyCode" character varying(40),
                "PlantType" character varying(40),
                "PropertyCode" character varying(40),
                "Address" character varying(500),
                "City" character varying(150),
                "Country" character varying(100),
                "SourceSystem" character varying(80),
                "ExternalId" character varying(80),
                "Status" character varying(16) NOT NULL,
                "IsDeleted" boolean NOT NULL DEFAULT FALSE,
                "CreatedAt" timestamp with time zone NOT NULL,
                "UpdatedAt" timestamp with time zone NOT NULL,
                CONSTRAINT "PK_plant_masters" PRIMARY KEY ("Id"),
                CONSTRAINT "FK_plant_masters_organizations_OrganizationId" FOREIGN KEY ("OrganizationId") REFERENCES organizations ("Id") ON DELETE CASCADE
            );
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_plant_masters_OrganizationId_PlantCode" ON plant_masters ("OrganizationId", "PlantCode");

            CREATE TABLE IF NOT EXISTS storage_location_masters (
                "Id" uuid NOT NULL,
                "OrganizationId" uuid NOT NULL,
                "StorageLocationCode" character varying(40) NOT NULL,
                "StorageLocationName" character varying(250) NOT NULL,
                "PlantCode" character varying(40) NOT NULL,
                "CompanyCode" character varying(40),
                "LocationType" character varying(40),
                "Description" character varying(500),
                "SourceSystem" character varying(80),
                "ExternalId" character varying(80),
                "Status" character varying(16) NOT NULL,
                "IsDeleted" boolean NOT NULL DEFAULT FALSE,
                "CreatedAt" timestamp with time zone NOT NULL,
                "UpdatedAt" timestamp with time zone NOT NULL,
                CONSTRAINT "PK_storage_location_masters" PRIMARY KEY ("Id"),
                CONSTRAINT "FK_storage_location_masters_organizations_OrganizationId" FOREIGN KEY ("OrganizationId") REFERENCES organizations ("Id") ON DELETE CASCADE
            );
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_storage_location_masters_OrganizationId_PlantCode_StorageLocationCode" ON storage_location_masters ("OrganizationId", "PlantCode", "StorageLocationCode");
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP TABLE IF EXISTS storage_location_masters;
            DROP TABLE IF EXISTS plant_masters;
            DROP TABLE IF EXISTS property_masters;
            DROP TABLE IF EXISTS company_code_masters;
            """);
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Buyer.Infrastructure.Migrations
{
   
    public partial class ExcelMaterialMastertable : Migration
    {
        
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            
            migrationBuilder.Sql(@"
IF EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = 'fk_approval_flow_predefined_material_mapping_predefined_material_predefined_material_id'
      AND parent_object_id = OBJECT_ID('buyersystem.approval_flow_predefined_material_mapping')
)
ALTER TABLE [buyersystem].[approval_flow_predefined_material_mapping]
DROP CONSTRAINT [fk_approval_flow_predefined_material_mapping_predefined_material_predefined_material_id];
");

            migrationBuilder.Sql(@"
IF EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'ix_approval_flow_predefined_material_mapping_predefined_material_id'
      AND object_id = OBJECT_ID('buyersystem.approval_flow_predefined_material_mapping')
)
DROP INDEX [ix_approval_flow_predefined_material_mapping_predefined_material_id]
ON [buyersystem].[approval_flow_predefined_material_mapping];
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('buyersystem.approval_flow_predefined_material_mapping')
      AND name = 'upload_type'
)
ALTER TABLE [buyersystem].[approval_flow_predefined_material_mapping]
ADD [upload_type] nvarchar(max) NOT NULL DEFAULT '';
");

            migrationBuilder.Sql(@"
IF OBJECT_ID('buyersystem.excel_material_master') IS NULL
BEGIN
CREATE TABLE [buyersystem].[excel_material_master] (
    [id] uniqueidentifier NOT NULL,
    [asset_id] uniqueidentifier NOT NULL,
    [buyer_id] uniqueidentifier NOT NULL,
    [status] nvarchar(max) NOT NULL,
    [title] nvarchar(max) NOT NULL,
    [date_created] datetime2 NOT NULL,
    [date_updated] datetime2 NOT NULL,
    [created_by] uniqueidentifier NOT NULL,
    [updated_by] uniqueidentifier NOT NULL,
    [is_active] bit NOT NULL,
    CONSTRAINT [pk_excel_material_master] PRIMARY KEY ([id]),
    CONSTRAINT [fk_excel_material_master_asset_asset_id] FOREIGN KEY ([asset_id]) REFERENCES [buyersystem].[asset] ([id]) ON DELETE CASCADE,
    CONSTRAINT [fk_excel_material_master_buyer_business_profile_buyer_id] FOREIGN KEY ([buyer_id]) REFERENCES [buyersystem].[buyer_business_profile] ([id]) ON DELETE CASCADE
);
CREATE INDEX [ix_excel_material_master_asset_id] ON [buyersystem].[excel_material_master] ([asset_id]);
CREATE INDEX [ix_excel_material_master_buyer_id] ON [buyersystem].[excel_material_master] ([buyer_id]);
CREATE INDEX [ix_excel_material_master_is_active] ON [buyersystem].[excel_material_master] ([is_active]);
END
");
        }

      
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "excel_material_master",
                schema: "buyersystem");

            migrationBuilder.DropColumn(
                name: "upload_type",
                schema: "buyersystem",
                table: "approval_flow_predefined_material_mapping");

            migrationBuilder.CreateIndex(
                name: "ix_approval_flow_predefined_material_mapping_predefined_material_id",
                schema: "buyersystem",
                table: "approval_flow_predefined_material_mapping",
                column: "predefined_material_id");

            migrationBuilder.AddForeignKey(
                name: "fk_approval_flow_predefined_material_mapping_predefined_material_predefined_material_id",
                schema: "buyersystem",
                table: "approval_flow_predefined_material_mapping",
                column: "predefined_material_id",
                principalSchema: "buyersystem",
                principalTable: "predefined_material",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MasterData.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UnspscCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Key = table.Column<int>(type: "int", nullable: false),
                    Segment = table.Column<long>(type: "bigint", nullable: false),
                    SegmentTitle = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SegmentDefinition = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Family = table.Column<long>(type: "bigint", nullable: true),
                    FamilyTitle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FamilyDefinition = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Class = table.Column<long>(type: "bigint", nullable: true),
                    ClassTitle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClassDefinition = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Commodity = table.Column<long>(type: "bigint", nullable: true),
                    CommodityTitle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CommodityDefinition = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Synonym = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Acronym = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateUpdated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnspscCategories", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UnspscCategories");
        }
    }
}

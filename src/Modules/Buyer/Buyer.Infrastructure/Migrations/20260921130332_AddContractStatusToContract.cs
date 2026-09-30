using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Buyer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddContractStatusToContract : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "contract_status",
                schema: "buyersystem",
                table: "contract",
                type: "nvarchar(max)",
                nullable: true);

            // Contracts that already exist were created, so mark them.
            migrationBuilder.Sql(
                "UPDATE [buyersystem].[contract] SET [contract_status] = 'CONTRACT_CREATED'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "contract_status",
                schema: "buyersystem",
                table: "contract");
        }
    }
}

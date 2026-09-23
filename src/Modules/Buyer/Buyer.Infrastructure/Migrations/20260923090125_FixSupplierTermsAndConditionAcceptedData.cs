using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Buyer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixSupplierTermsAndConditionAcceptedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Follow-up for the earlier bool->string migration: on any
            // database where that migration already ran before its
            // corrective Sql() calls were added, the column was left with
            // the literal ALTER COLUMN output '1'/'0' instead of the real
            // status words. Only touches rows still at '1'/'0', so this is
            // safe to run again on a database that's already correct.
            migrationBuilder.Sql(
                "UPDATE [buyersystem].[rfqsupplier_mapping] SET [supplier_terms_and_condition_accepted] = 'ACCEPTED' WHERE [supplier_terms_and_condition_accepted] = '1';");
            migrationBuilder.Sql(
                "UPDATE [buyersystem].[rfqsupplier_mapping] SET [supplier_terms_and_condition_accepted] = 'PENDING' WHERE [supplier_terms_and_condition_accepted] = '0';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE [buyersystem].[rfqsupplier_mapping] SET [supplier_terms_and_condition_accepted] = '1' WHERE [supplier_terms_and_condition_accepted] = 'ACCEPTED';");
            migrationBuilder.Sql(
                "UPDATE [buyersystem].[rfqsupplier_mapping] SET [supplier_terms_and_condition_accepted] = '0' WHERE [supplier_terms_and_condition_accepted] <> 'ACCEPTED';");
        }
    }
}

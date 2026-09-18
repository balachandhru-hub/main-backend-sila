using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Identity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveContractApprovalFlowFeatures : Migration
    {
        private static readonly Guid SystemUserId = Guid.Parse("DF78056A-1097-430C-B29A-0CC42E3ECE7B");

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Role-feature mappings for the removed ContractApprovalFlowController permissions.
            migrationBuilder.DeleteData(
                schema: "identitysystem",
                table: "role_feature_mapping",
                keyColumn: "id",
                keyValues: new object[]
                {
                    new Guid("0160425f-dd26-4593-b565-0c9a9027b0f1"),
                    new Guid("c5fecc48-37c2-4535-89a6-2ebdaf1b2cf1"),
                    new Guid("fd96d1b3-6c34-4254-8733-2dc81120ab01"),
                    new Guid("f67448cf-5575-4b54-876b-69fc661590e9"),
                    new Guid("dae61072-79d9-4df7-9c2c-f9aeb8f5613d"),
                    new Guid("721c0ccf-fb64-49f6-8c27-b3eff7b36549"),
                    new Guid("8a477213-034b-4d8a-bf75-7fc82729eb24"),
                    new Guid("5e977fc7-afb5-4504-b7e5-dd1ddb50f176")
                });

            // The removed ContractApprovalFlowController permissions themselves.
            migrationBuilder.DeleteData(
                schema: "identitysystem",
                table: "feature",
                keyColumn: "id",
                keyValues: new object[]
                {
                    new Guid("45cf0b3a-f92f-4df0-8923-932c54ca6b4c"),
                    new Guid("b10d852f-f728-4335-a57c-c31f711dc48a"),
                    new Guid("695462b6-8874-49a1-82d3-4367fef3f158"),
                    new Guid("d1be883d-0ee9-4656-891d-7a4a5d025681")
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "identitysystem",
                table: "feature",
                columns: new[] { "id", "key", "created_by", "updated_by", "date_created", "date_updated", "is_active" },
                values: new object[,]
                {
                    { new Guid("45cf0b3a-f92f-4df0-8923-932c54ca6b4c"), "CREATE_CONTRACT_APPROVAL_FLOW", SystemUserId, SystemUserId, DateTime.UtcNow, DateTime.UtcNow, true },
                    { new Guid("b10d852f-f728-4335-a57c-c31f711dc48a"), "GET_CONTRACT_APPROVAL_FLOW", SystemUserId, SystemUserId, DateTime.UtcNow, DateTime.UtcNow, true },
                    { new Guid("695462b6-8874-49a1-82d3-4367fef3f158"), "GET_CONTRACT_APPROVAL_FLOW_USER_MAPPING", SystemUserId, SystemUserId, DateTime.UtcNow, DateTime.UtcNow, true },
                    { new Guid("d1be883d-0ee9-4656-891d-7a4a5d025681"), "UPDATE_CONTRACT_APPROVAL_FLOW", SystemUserId, SystemUserId, DateTime.UtcNow, DateTime.UtcNow, true }
                });

            migrationBuilder.InsertData(
                schema: "identitysystem",
                table: "role_feature_mapping",
                columns: new[] { "id", "feature_id", "role_id", "created_by", "updated_by", "date_created", "date_updated", "is_active" },
                values: new object[,]
                {
                    { new Guid("0160425f-dd26-4593-b565-0c9a9027b0f1"), new Guid("45cf0b3a-f92f-4df0-8923-932c54ca6b4c"), new Guid("c95f5a1b-4aec-4647-9328-895a58193ec4"), SystemUserId, SystemUserId, DateTime.UtcNow, DateTime.UtcNow, true },
                    { new Guid("c5fecc48-37c2-4535-89a6-2ebdaf1b2cf1"), new Guid("45cf0b3a-f92f-4df0-8923-932c54ca6b4c"), new Guid("5a72f81e-a2c5-4f4a-bd55-6376c3c9ed73"), SystemUserId, SystemUserId, DateTime.UtcNow, DateTime.UtcNow, true },
                    { new Guid("fd96d1b3-6c34-4254-8733-2dc81120ab01"), new Guid("b10d852f-f728-4335-a57c-c31f711dc48a"), new Guid("c95f5a1b-4aec-4647-9328-895a58193ec4"), SystemUserId, SystemUserId, DateTime.UtcNow, DateTime.UtcNow, true },
                    { new Guid("f67448cf-5575-4b54-876b-69fc661590e9"), new Guid("b10d852f-f728-4335-a57c-c31f711dc48a"), new Guid("5a72f81e-a2c5-4f4a-bd55-6376c3c9ed73"), SystemUserId, SystemUserId, DateTime.UtcNow, DateTime.UtcNow, true },
                    { new Guid("dae61072-79d9-4df7-9c2c-f9aeb8f5613d"), new Guid("695462b6-8874-49a1-82d3-4367fef3f158"), new Guid("c95f5a1b-4aec-4647-9328-895a58193ec4"), SystemUserId, SystemUserId, DateTime.UtcNow, DateTime.UtcNow, true },
                    { new Guid("721c0ccf-fb64-49f6-8c27-b3eff7b36549"), new Guid("695462b6-8874-49a1-82d3-4367fef3f158"), new Guid("5a72f81e-a2c5-4f4a-bd55-6376c3c9ed73"), SystemUserId, SystemUserId, DateTime.UtcNow, DateTime.UtcNow, true },
                    { new Guid("8a477213-034b-4d8a-bf75-7fc82729eb24"), new Guid("d1be883d-0ee9-4656-891d-7a4a5d025681"), new Guid("c95f5a1b-4aec-4647-9328-895a58193ec4"), SystemUserId, SystemUserId, DateTime.UtcNow, DateTime.UtcNow, true },
                    { new Guid("5e977fc7-afb5-4504-b7e5-dd1ddb50f176"), new Guid("d1be883d-0ee9-4656-891d-7a4a5d025681"), new Guid("5a72f81e-a2c5-4f4a-bd55-6376c3c9ed73"), SystemUserId, SystemUserId, DateTime.UtcNow, DateTime.UtcNow, true }
                });
        }
    }
}

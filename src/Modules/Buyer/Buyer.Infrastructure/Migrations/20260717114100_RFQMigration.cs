using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Buyer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RFQMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "item_buyer_master",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    material_code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    material_group = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_item_buyer_master", x => x.id);
                    table.ForeignKey(
                        name: "fk_item_buyer_master_buyer_business_profile_buyer_id",
                        column: x => x.buyer_id,
                        principalSchema: "buyersystem",
                        principalTable: "buyer_business_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "rfq",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    rfqnumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    buyer_organization_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    delivery_location = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    start_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    end_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    delivery_target_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    cost_center = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    department = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    add_lot_option = table.Column<bool>(type: "bit", nullable: false),
                    budget = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    region = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    discount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    tax_charge = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    delivery_charge = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rfq", x => x.id);
                    table.ForeignKey(
                        name: "fk_rfq_buyer_business_profile_buyer_id",
                        column: x => x.buyer_id,
                        principalSchema: "buyersystem",
                        principalTable: "buyer_business_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "rfqanswer_option",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    rfqquestion_answer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    rfqquestion_option_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rfqanswer_option", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "rfqquestion_answer",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    rfqquestion_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    rfqid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    rfqnumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    supplier_organization_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    answer = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    asset_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    question_option_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    answered_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rfqquestion_answer", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "rfqquestion_option",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    rfqquestion_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    option_text = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    display_order = table.Column<int>(type: "int", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rfqquestion_option", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "supplier_verification_request",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    rfqid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_organization_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    supplier_organization_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    rfqverification_template_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    remarks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    due_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    verified_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    verified_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_supplier_verification_request", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "verification_answer",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    supplier_verification_request_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    verification_template_question_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    template_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    answer = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    asset_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    verification_template_question_option_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    answered_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_verification_answer", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "verification_answer_option",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    verification_answer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    verification_template_question_option_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_verification_answer_option", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "verification_template",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    template_code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    template_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_verification_template", x => x.id);
                    table.ForeignKey(
                        name: "fk_verification_template_buyer_business_profile_buyer_id",
                        column: x => x.buyer_id,
                        principalSchema: "buyersystem",
                        principalTable: "buyer_business_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "verification_template_question",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    verification_template_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    question = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    question_type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    is_required = table.Column<bool>(type: "bit", nullable: false),
                    display_order = table.Column<int>(type: "int", nullable: false),
                    placeholder = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_verification_template_question", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "verification_template_question_option",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    verification_template_question_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    option_text = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    display_order = table.Column<int>(type: "int", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_verification_template_question_option", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "rfqattachment_mapping",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    rfqid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    asset_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rfqattachment_mapping", x => x.id);
                    table.ForeignKey(
                        name: "fk_rfqattachment_mapping_rfq_rfqid",
                        column: x => x.rfqid,
                        principalSchema: "buyersystem",
                        principalTable: "rfq",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "rfqitem",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    rfqid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    uom = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    material_code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    material_group = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rfqitem", x => x.id);
                    table.ForeignKey(
                        name: "fk_rfqitem_rfq_rfqid",
                        column: x => x.rfqid,
                        principalSchema: "buyersystem",
                        principalTable: "rfq",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "rfqquestion",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    rfqid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    rfqnumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    question = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    question_type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    is_required = table.Column<bool>(type: "bit", nullable: false),
                    display_order = table.Column<int>(type: "int", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rfqquestion", x => x.id);
                    table.ForeignKey(
                        name: "fk_rfqquestion_rfq_rfqid",
                        column: x => x.rfqid,
                        principalSchema: "buyersystem",
                        principalTable: "rfq",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "rfqitem_attachment_mapping",
                schema: "buyersystem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    rfqitem_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    asset_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rfqitem_attachment_mapping", x => x.id);
                    table.ForeignKey(
                        name: "fk_rfqitem_attachment_mapping_rfqitem_rfqitem_id",
                        column: x => x.rfqitem_id,
                        principalSchema: "buyersystem",
                        principalTable: "rfqitem",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_item_buyer_master_buyer_id",
                schema: "buyersystem",
                table: "item_buyer_master",
                column: "buyer_id");

            migrationBuilder.CreateIndex(
                name: "ix_item_buyer_master_is_active",
                schema: "buyersystem",
                table: "item_buyer_master",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_rfq_buyer_id",
                schema: "buyersystem",
                table: "rfq",
                column: "buyer_id");

            migrationBuilder.CreateIndex(
                name: "ix_rfq_is_active",
                schema: "buyersystem",
                table: "rfq",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_rfqanswer_option_is_active",
                schema: "buyersystem",
                table: "rfqanswer_option",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_rfqattachment_mapping_is_active",
                schema: "buyersystem",
                table: "rfqattachment_mapping",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_rfqattachment_mapping_rfqid",
                schema: "buyersystem",
                table: "rfqattachment_mapping",
                column: "rfqid");

            migrationBuilder.CreateIndex(
                name: "ix_rfqitem_is_active",
                schema: "buyersystem",
                table: "rfqitem",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_rfqitem_rfqid",
                schema: "buyersystem",
                table: "rfqitem",
                column: "rfqid");

            migrationBuilder.CreateIndex(
                name: "ix_rfqitem_attachment_mapping_is_active",
                schema: "buyersystem",
                table: "rfqitem_attachment_mapping",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_rfqitem_attachment_mapping_rfqitem_id",
                schema: "buyersystem",
                table: "rfqitem_attachment_mapping",
                column: "rfqitem_id");

            migrationBuilder.CreateIndex(
                name: "ix_rfqquestion_is_active",
                schema: "buyersystem",
                table: "rfqquestion",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_rfqquestion_rfqid",
                schema: "buyersystem",
                table: "rfqquestion",
                column: "rfqid");

            migrationBuilder.CreateIndex(
                name: "ix_rfqquestion_answer_is_active",
                schema: "buyersystem",
                table: "rfqquestion_answer",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_rfqquestion_option_is_active",
                schema: "buyersystem",
                table: "rfqquestion_option",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_supplier_verification_request_is_active",
                schema: "buyersystem",
                table: "supplier_verification_request",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_verification_answer_is_active",
                schema: "buyersystem",
                table: "verification_answer",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_verification_answer_option_is_active",
                schema: "buyersystem",
                table: "verification_answer_option",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_verification_template_buyer_id",
                schema: "buyersystem",
                table: "verification_template",
                column: "buyer_id");

            migrationBuilder.CreateIndex(
                name: "ix_verification_template_is_active",
                schema: "buyersystem",
                table: "verification_template",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_verification_template_question_is_active",
                schema: "buyersystem",
                table: "verification_template_question",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_verification_template_question_option_is_active",
                schema: "buyersystem",
                table: "verification_template_question_option",
                column: "is_active");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "item_buyer_master",
                schema: "buyersystem");

            migrationBuilder.DropTable(
                name: "rfqanswer_option",
                schema: "buyersystem");

            migrationBuilder.DropTable(
                name: "rfqattachment_mapping",
                schema: "buyersystem");

            migrationBuilder.DropTable(
                name: "rfqitem_attachment_mapping",
                schema: "buyersystem");

            migrationBuilder.DropTable(
                name: "rfqquestion",
                schema: "buyersystem");

            migrationBuilder.DropTable(
                name: "rfqquestion_answer",
                schema: "buyersystem");

            migrationBuilder.DropTable(
                name: "rfqquestion_option",
                schema: "buyersystem");

            migrationBuilder.DropTable(
                name: "supplier_verification_request",
                schema: "buyersystem");

            migrationBuilder.DropTable(
                name: "verification_answer",
                schema: "buyersystem");

            migrationBuilder.DropTable(
                name: "verification_answer_option",
                schema: "buyersystem");

            migrationBuilder.DropTable(
                name: "verification_template",
                schema: "buyersystem");

            migrationBuilder.DropTable(
                name: "verification_template_question",
                schema: "buyersystem");

            migrationBuilder.DropTable(
                name: "verification_template_question_option",
                schema: "buyersystem");

            migrationBuilder.DropTable(
                name: "rfqitem",
                schema: "buyersystem");

            migrationBuilder.DropTable(
                name: "rfq",
                schema: "buyersystem");
        }
    }
}

using SilaMe.Api.DTOs;
using SilaMe.Api.Models;

namespace SilaMe.Api.Services;

public static class ApprovalWorkflowFields
{
    public static readonly IReadOnlyList<ApprovalTypeCatalogItem> Types =
    [
        new("RECIPE", "Recipe", "CREATE and CHANGE recipe submissions resolve this configuration at submit time.",
        [
            new("RECIPE_TOTAL_COST", "Recipe total cost", "amount"),
            new("COST_PER_PORTION", "Cost per portion", "amount"),
            new("SERVING_QUANTITY", "Serving quantity", "quantity"),
            new("INGREDIENT_COUNT", "Ingredient count", "count"),
            new("PREPARATION_MINUTES", "Preparation minutes", "minutes"),
        ]),
        new("MATERIAL_MASTER", "Material Master", "CREATE and CHANGE material submissions resolve this configuration at submit time.",
        [
            new("UNIT_COST", "Unit cost", "amount"),
            new("STANDARD_PRICE", "Standard price", "amount"),
            new("MOVING_AVERAGE_PRICE", "Moving average price", "amount"),
        ]),
        new("SUPPLIER_MASTER", "Supplier Master", "Stored here for all approval types. Supplier runtime is not changed by this save.",
        [
            new("CREDIT_LIMIT", "Credit limit", "amount"),
        ]),
        new("INTERNAL_TRANSFER", "Internal Transfer", "Stored here for all approval types. Transfer posting is not changed by this save.",
        [
            new("TRANSFER_VALUE", "Transfer value", "amount"),
            new("LINE_COUNT", "Line count", "count"),
        ]),
        new("GRN", "GRN", "Stored here for all approval types. GRN finalization and posting are not changed by this save.",
        [
            new("GRN_TOTAL", "GRN total", "amount"),
            new("LINE_COUNT", "Line count", "count"),
        ]),
        new("INVOICE", "Invoice", "Stored here for all approval types. Invoice OCR and review are not changed by this save.",
        [
            new("GROSS_AMOUNT", "Gross amount", "amount"),
            new("NET_AMOUNT", "Net amount", "amount"),
            new("AMOUNT_DUE", "Amount due", "amount"),
        ]),
    ];

    public static bool IsKnownField(ApprovalWorkflowType type, string fieldKey) =>
        Types.First(item => item.Key == type.ToString()).Fields.Any(item => item.Key.Equals(fieldKey, StringComparison.OrdinalIgnoreCase));

    public static ApprovalWorkflowType? MapRecipeEvent(RecipeApprovalEvent eventKind) => eventKind switch
    {
        RecipeApprovalEvent.CREATE_RECIPE or RecipeApprovalEvent.CHANGE_RECIPE => ApprovalWorkflowType.RECIPE,
        RecipeApprovalEvent.CREATE_MATERIAL or RecipeApprovalEvent.CHANGE_MATERIAL => ApprovalWorkflowType.MATERIAL_MASTER,
        _ => null,
    };

    public static ApprovalWorkflowAction MapRecipeAction(RecipeApprovalEvent eventKind) =>
        eventKind is RecipeApprovalEvent.CREATE_RECIPE or RecipeApprovalEvent.CREATE_MATERIAL
            ? ApprovalWorkflowAction.CREATE
            : ApprovalWorkflowAction.CHANGE;

    public static RecipeApprovalEvent? MapLegacyEvent(ApprovalWorkflowType type, ApprovalWorkflowAction action) =>
        (type, action) switch
        {
            (ApprovalWorkflowType.RECIPE, ApprovalWorkflowAction.CREATE) => RecipeApprovalEvent.CREATE_RECIPE,
            (ApprovalWorkflowType.RECIPE, ApprovalWorkflowAction.CHANGE) => RecipeApprovalEvent.CHANGE_RECIPE,
            (ApprovalWorkflowType.MATERIAL_MASTER, ApprovalWorkflowAction.CREATE) => RecipeApprovalEvent.CREATE_MATERIAL,
            (ApprovalWorkflowType.MATERIAL_MASTER, ApprovalWorkflowAction.CHANGE) => RecipeApprovalEvent.CHANGE_MATERIAL,
            _ => null,
        };
}

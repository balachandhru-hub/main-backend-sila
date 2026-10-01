using Operations.Domain.Dtos;

namespace Operations.Application.Services.Integration
{
    /// <summary>
    /// Target fields an ERP payload can be mapped to.
    /// </summary>
    public static class IntegrationTargetFieldRegistry
    {
        public static readonly List<IntegrationTargetFieldResponseDto> Fields = new List<IntegrationTargetFieldResponseDto>
        {
            Field("PurchaseOrder.ExternalId", "Purchase order", "string", true, "NONE", "TRIM"),
            Field("PurchaseOrder.PoNumber", "Purchase order", "string", true, "NONE", "TRIM", "UPPER"),
            Field("PurchaseOrder.SupplierCode", "Purchase order", "string", true, "NONE", "TRIM", "UPPER"),
            Field("PurchaseOrder.SupplierName", "Purchase order", "string", true, "NONE", "TRIM"),
            Field("PurchaseOrder.Currency", "Purchase order", "string", true, "NONE", "UPPER"),
            Field("PurchaseOrder.CompanyCode", "Purchase order", "string", true, "NONE", "TRIM", "UPPER"),
            Field("PurchaseOrder.PurchaseOrderType", "Purchase order", "string", true, "NONE", "TRIM", "UPPER"),
            Field("PurchaseOrder.TotalNetAmount", "Purchase order", "decimal", true, "NONE"),
            Field("PurchaseOrder.TotalTaxAmount", "Purchase order", "decimal", true, "NONE"),
            Field("PurchaseOrder.TotalAmount", "Purchase order", "decimal", true, "NONE"),
            Field("PurchaseOrder.PoDate", "Purchase order", "date", true, "NONE"),
            Field("PurchaseOrder.DeliveryDate", "Purchase order", "date", true, "NONE"),
            Field("PurchaseOrder.SourceLastChangedAt", "Purchase order", "datetime", false, "NONE"),
            Field("PurchaseOrderItem.ExternalId", "PO item", "string", false, "NONE", "TRIM"),
            Field("PurchaseOrderItem.LineNumber", "PO item", "integer", true, "NONE"),
            Field("PurchaseOrderItem.MaterialCode", "PO item", "string", true, "NONE", "TRIM", "UPPER"),
            Field("PurchaseOrderItem.Description", "PO item", "string", true, "NONE", "TRIM"),
            Field("PurchaseOrderItem.OrderedQuantity", "PO item", "decimal", true, "NONE"),
            Field("PurchaseOrderItem.Uom", "PO item", "string", true, "NONE", "TRIM", "UPPER"),
            Field("PurchaseOrderItem.UnitPrice", "PO item", "decimal", false, "NONE"),
            Field("PurchaseOrderItem.SourceLastChangedAt", "PO item", "datetime", false, "NONE"),
            Field("Supplier.ExternalId", "Supplier", "string", false, "NONE", "TRIM"),
            Field("Supplier.SupplierCode", "Supplier", "string", true, "NONE", "TRIM", "UPPER"),
            Field("Supplier.Name", "Supplier", "string", true, "NONE", "TRIM"),
            Field("Supplier.LegalName", "Supplier", "string", false, "NONE", "TRIM"),
            Field("Supplier.TaxNumber", "Supplier", "string", false, "NONE", "TRIM"),
            Field("Supplier.Email", "Supplier", "string", false, "NONE", "TRIM", "LOWER"),
            Field("Supplier.Phone", "Supplier", "string", false, "NONE", "TRIM"),
            Field("Supplier.Country", "Supplier", "string", false, "NONE", "TRIM"),
            Field("Supplier.Currency", "Supplier", "string", true, "NONE", "UPPER"),
            Field("Supplier.SourceLastChangedAt", "Supplier", "datetime", false, "NONE"),
        };

        public static bool Contains(string target)
        {
            return Fields.Any(field => field.TargetField.Equals(target, StringComparison.OrdinalIgnoreCase));
        }

        private static IntegrationTargetFieldResponseDto Field(string target, string area, string dataType, bool required, params string[] transformations)
        {
            return new IntegrationTargetFieldResponseDto
            {
                TargetField = target,
                Area = area,
                DataType = dataType,
                Required = required,
                AllowedTransformations = transformations.ToList()
            };
        }
    }
}

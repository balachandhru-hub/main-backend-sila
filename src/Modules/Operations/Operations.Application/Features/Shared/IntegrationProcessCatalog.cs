using Operations.Domain.Enums;

namespace Operations.Application.Features.Shared
{
    /// <summary>
    /// What each API type is: which side configures it, whether the application calls it (push) or
    /// reads from it (pull), and which mapped fields it cannot work without. Every API type of the
    /// buyer and of the supplier is configured through the same integration configuration.
    /// </summary>
    internal static class IntegrationProcessCatalog
    {
        public const string SIDE_BUYER = "Buyer";
        public const string SIDE_SUPPLIER = "Supplier";

        internal sealed class ProcessInfo
        {
            public string Side { get; init; } = SIDE_BUYER;

            /// <summary>The application sends data to the API (a purchase order, a goods receipt).</summary>
            public bool IsPush { get; init; }

            /// <summary>The API can be read by a manual or scheduled pull.</summary>
            public bool CanPull { get; init; }

            /// <summary>Area of the target fields the API's records are mapped to; null when it has no field mapping.</summary>
            public string? MappingArea { get; init; }

            public string[] RequiredTargets { get; init; } = Array.Empty<string>();
        }

        private static readonly Dictionary<IntegrationProcessType, ProcessInfo> Processes = new()
        {
            [IntegrationProcessType.GET_PO] = new ProcessInfo
            {
                CanPull = true,
                MappingArea = "PurchaseOrder",
                RequiredTargets = new[] { "PurchaseOrder.PoNumber", "PurchaseOrder.SupplierCode", "PurchaseOrder.SupplierName", "PurchaseOrder.Currency" }
            },
            [IntegrationProcessType.GET_SUPPLIER] = new ProcessInfo
            {
                CanPull = true,
                MappingArea = "Supplier",
                RequiredTargets = new[] { "Supplier.SupplierCode", "Supplier.Name" }
            },
            [IntegrationProcessType.POST_GRN] = new ProcessInfo { IsPush = true },
            [IntegrationProcessType.POST_PO] = new ProcessInfo { IsPush = true },
            [IntegrationProcessType.GET_STOCK] = new ProcessInfo
            {
                CanPull = true,
                MappingArea = "Stock",
                RequiredTargets = new[] { "Stock.MaterialCode", "Stock.Quantity" }
            },
            [IntegrationProcessType.GET_MATERIAL] = new ProcessInfo(),
            [IntegrationProcessType.GET_CONTRACT] = new ProcessInfo(),
            [IntegrationProcessType.POST_SUPPLIER] = new ProcessInfo { IsPush = true },
            [IntegrationProcessType.GET_CATALOG] = new ProcessInfo
            {
                Side = SIDE_SUPPLIER,
                CanPull = true,
                MappingArea = "Catalog",
                RequiredTargets = new[] { "Catalog.Sku", "Catalog.Name", "Catalog.Price", "Catalog.Currency", "Catalog.UnitOfMeasure" }
            },
            [IntegrationProcessType.GET_CATALOG_STOCK] = new ProcessInfo
            {
                Side = SIDE_SUPPLIER,
                CanPull = true,
                MappingArea = "CatalogStock",
                RequiredTargets = new[] { "CatalogStock.Sku", "CatalogStock.AvailableStock" }
            },
            [IntegrationProcessType.POST_SALES_ORDER] = new ProcessInfo { Side = SIDE_SUPPLIER, IsPush = true },
        };

        /// <summary>The API type's description, or null for a type that is not offered.</summary>
        public static ProcessInfo? Find(IntegrationProcessType processType)
        {
            return Processes.TryGetValue(processType, out ProcessInfo? info) ? info : null;
        }

        /// <summary>
        /// Whether an organization of the given type (the token's OrganizationType) may configure the API type.
        /// </summary>
        public static bool IsAvailableTo(IntegrationProcessType processType, string? organizationType)
        {
            ProcessInfo? info = Find(processType);
            return info != null && string.Equals(info.Side, organizationType, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>The target field prefix of the API type's records, for example "Catalog." for the catalog.</summary>
        public static bool OwnsTarget(IntegrationProcessType processType, string targetField)
        {
            string? area = Find(processType)?.MappingArea;
            if (area == null)
            {
                return false;
            }

            // Purchase orders are mapped with their items.
            return targetField.StartsWith(area + ".", StringComparison.OrdinalIgnoreCase)
                || (processType == IntegrationProcessType.GET_PO && targetField.StartsWith("PurchaseOrderItem.", StringComparison.OrdinalIgnoreCase));
        }
    }
}

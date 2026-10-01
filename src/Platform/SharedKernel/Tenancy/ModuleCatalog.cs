namespace SharedKernel.Tenancy
{
    /// <summary>
    /// The modules a customer realm can be licensed for, and which API features belong to each.
    /// </summary>
    public static class ModuleCatalog
    {
        /// <summary>RFQ, auctions, contracts, catalogs, approvals. Part of every realm.</summary>
        public const string SOURCING = "SOURCING";

        /// <summary>Buying platform: cart, wishlists, outlets, purchase order hand-off.</summary>
        public const string BUYING = "BUYING";

        /// <summary>Hospitality operations: invoice capture, goods receipts, stock.</summary>
        public const string OPERATIONS = "OPERATIONS";

        public static readonly IReadOnlyList<string> All = new[] { SOURCING, BUYING, OPERATIONS };

        /// <summary>
        /// The module a feature key belongs to, or null when the feature is part of the base platform.
        /// </summary>
        public static string? RequiredModule(string? featureKey)
        {
            if (string.IsNullOrWhiteSpace(featureKey))
            {
                return null;
            }

            string key = featureKey.ToUpperInvariant();
            if (key.StartsWith("OPERATIONS_", StringComparison.Ordinal))
            {
                return OPERATIONS;
            }

            if (key.Contains("WISHLIST", StringComparison.Ordinal) || key.EndsWith("_OUTLET", StringComparison.Ordinal))
            {
                return BUYING;
            }

            return null;
        }
    }
}

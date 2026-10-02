namespace Supplier.Application.Contracts
{
    public interface IOperationsApiClient
    {
        /// <summary>
        /// Asks the integration service to read the product stock of these supplier organizations from
        /// their own stock APIs now. A supplier without a stock API is left as it is. Never throws:
        /// when the refresh cannot be done, the stock already stored is what the buyer sees.
        /// </summary>
        Task RefreshProductStock(
            List<Guid> supplierOrganizationIds,
            CancellationToken cancellationToken = default);
    }
}

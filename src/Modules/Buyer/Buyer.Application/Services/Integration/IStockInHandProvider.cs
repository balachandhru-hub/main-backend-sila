namespace Buyer.Application.Services.Integration
{
    /// <summary>
    /// Integration point for stock in hand. Nothing in this solution holds the buyer's own stock, so the
    /// default implementation returns null. Connect the inventory source (ERP stock, Operations stock) by
    /// registering another implementation of this interface in Buyer.API.
    /// </summary>
    public interface IStockInHandProvider
    {
        /// <summary>
        /// Stock the buyer has in the storage location for the material, or null when it is not known.
        /// </summary>
        Task<decimal?> GetStockInHandAsync(
            Guid buyerId,
            string? storageLocation,
            string? materialCode,
            CancellationToken cancellationToken);
    }
}

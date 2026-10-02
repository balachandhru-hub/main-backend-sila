using Buyer.Application.Contracts;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;

namespace Buyer.Application.Services.Integration
{
    /// <summary>
    /// Stock in hand from the buyer's own ERP, through the stock API the buyer configured under
    /// Integrations. The ERP is asked once per request and the answer serves every line of it.
    /// A buyer without an active stock API has no known stock in hand (null).
    /// </summary>
    public class ErpStockInHandProvider : IStockInHandProvider
    {
        private readonly IRepositoryWrapper _repository;
        private readonly IOperationsIntegrationClient _integrationClient;

        // The stock of each buyer asked about in this request.
        private readonly Dictionary<Guid, StockInHandResponseDto> _stockByBuyer = new();

        public ErpStockInHandProvider(IRepositoryWrapper repository, IOperationsIntegrationClient integrationClient)
        {
            _repository = repository;
            _integrationClient = integrationClient;
        }

        public async Task<decimal?> GetStockInHandAsync(
            Guid buyerId,
            string? storageLocation,
            string? materialCode,
            CancellationToken cancellationToken)
        {
            // A product that is not mapped to a material has no stock figure in the ERP.
            if (string.IsNullOrWhiteSpace(materialCode))
            {
                return null;
            }

            if (!_stockByBuyer.TryGetValue(buyerId, out StockInHandResponseDto? stock))
            {
                BuyerBusinessProfile? buyer = _repository.BuyerBusinessProfile.FindFirstByCondition(x => x.Id == buyerId);
                stock = buyer == null
                    ? new StockInHandResponseDto()
                    : await _integrationClient.GetStockInHandAsync(buyer.OrganizationId, cancellationToken);
                _stockByBuyer[buyerId] = stock;
            }

            if (!stock.Configured || stock.Failed)
            {
                return null;
            }

            List<StockInHandItemDto> ofMaterial = stock.Items
                .Where(x => string.Equals(x.MaterialCode, materialCode.Trim(), StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (ofMaterial.Count == 0)
            {
                // The ERP answered and does not list the material: there is none in hand.
                return 0;
            }

            // The stock of the line's own storage location when the ERP reports locations; an ERP that
            // reports no location gives one figure per material.
            List<StockInHandItemDto> ofLocation = string.IsNullOrWhiteSpace(storageLocation)
                ? ofMaterial
                : ofMaterial.Where(x => string.IsNullOrWhiteSpace(x.StorageLocation)
                    || string.Equals(x.StorageLocation, storageLocation.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
            return ofLocation.Sum(x => x.Quantity);
        }
    }
}

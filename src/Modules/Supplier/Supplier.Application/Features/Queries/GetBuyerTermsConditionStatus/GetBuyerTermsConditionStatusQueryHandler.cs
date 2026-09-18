using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;
using Supplier.Domain.Dto;
using Supplier.Infrastructure.Contracts.IRepository;

namespace Supplier.Application.Features.Queries.GetBuyerTermsConditionStatus
{
    public class GetBuyerTermsConditionStatusQueryHandler
        : IRequestHandler<GetBuyerTermsConditionStatusQuery, List<BuyerTermsAndConditionStatusDto>>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public GetBuyerTermsConditionStatusQueryHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<List<BuyerTermsAndConditionStatusDto>> Handle(
            GetBuyerTermsConditionStatusQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInfo(
                $"Fetching Buyer Terms and Condition status for BuyerRFQId: {request.RFQId}");

            // One BuyerRFQ fans out to one SupplierRFQ per invited supplier,
            // so this returns every invited supplier's status in one call.
            var supplierRFQs = await _repository.SupplierRFQ
                .FindByCondition(x => x.BuyerRFQId == request.RFQId)
                .ToListAsync(cancellationToken);

            if (!supplierRFQs.Any())
            {
                _logger.LogError(
                    $"RFQ not found for BuyerRFQId: {request.RFQId}");
                throw new NotFoundCustomException(
                    "RFQ not found.",
                    $"No RFQ exists with BuyerRFQId: {request.RFQId}.");
            }

            var supplierIds = supplierRFQs
                .Select(x => x.SupplierId)
                .Distinct()
                .ToList();

            var supplierNames = await _repository.SupplierBusinessProfile
                .FindByCondition(x =>
                    supplierIds.Contains(x.Id) &&
                    x.IsActive)
                .ToDictionaryAsync(
                    x => x.Id,
                    x => x.OrganizationName,
                    cancellationToken);

            var result = supplierRFQs.Select(supplierRFQ => new BuyerTermsAndConditionStatusDto
            {
                SupplierId = supplierRFQ.SupplierId,
                SupplierName = supplierNames.TryGetValue(supplierRFQ.SupplierId, out var name)
                    ? name
                    : null,
                BuyerTermsAndConditionAccepted = supplierRFQ.BuyerTermsAndConditionAccepted
            }).ToList();

            _logger.LogInfo(
                $"Buyer Terms and Condition status fetched for BuyerRFQId: {request.RFQId}. " +
                $"Suppliers: {result.Count}");

            return result;
        }
    }
}

using Buyer.Domain.Common;
using Buyer.Domain.Dto;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Queries.GetSupplierTermsConditionStatus
{
    public class GetSupplierTermsConditionStatusQueryHandler
        : IRequestHandler<GetSupplierTermsConditionStatusQuery, SupplierTermsAndConditionStatusDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public GetSupplierTermsConditionStatusQueryHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<SupplierTermsAndConditionStatusDto> Handle(
            GetSupplierTermsConditionStatusQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInfo(
                $"Fetching Supplier Terms and Condition status for RFQId: {request.RFQId}");

            var rfq = await _repository.RFQ
                .FindByCondition(x => x.Id == request.RFQId)
                .FirstOrDefaultAsync(cancellationToken);

            if (rfq == null)
            {
                _logger.LogError($"RFQ not found for RFQId: {request.RFQId}");
                throw new NotFoundCustomException(
                    "RFQ not found.",
                    $"No RFQ exists with Id: {request.RFQId}.");
            }

            var buyer = _repository.BuyerBusinessProfile
                .FindFirstByCondition(x =>
                    x.Id == rfq.BuyerId &&
                    x.IsActive);

            return new SupplierTermsAndConditionStatusDto
            {
                BuyerId = rfq.BuyerId,
                BuyerName = buyer?.OrganizationName,
                Status = rfq.SupplierTermsAndConditionAccepted
                    ? Common.ACCEPTED_STATUS
                    : Common.REJECTED_STATUS
            };
        }
    }
}

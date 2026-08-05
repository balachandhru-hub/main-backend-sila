using Buyer.Domain.Dto;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Queries.SupplierVerificationRequest
{
    public class GetSupplierVerificationRequestDetailQueryHandler
        : IRequestHandler<GetSupplierVerificationRequestDetailQuery, SupplierVerificationRequestDetailDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public GetSupplierVerificationRequestDetailQueryHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<SupplierVerificationRequestDetailDto> Handle(
            GetSupplierVerificationRequestDetailQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Fetching Supplier Verification Request : {request.RequestId}");

            var result = await _repository.SupplierVerificationRequest
                .FindByCondition(x => x.Id == request.RequestId && x.IsActive)
                .Select(x => new SupplierVerificationRequestDetailDto
                {
                   
                    RFQId = x.RFQId,
                    RFQNumber = x.RFQNumber,
                    BuyerId = x.BuyerOrganizationId,
                    SupplierOrganizationId = x.SupplierOrganizationId,
                    TemplateId = x.RFQVerificationTemplateId,
                    Status = x.Status,
                    Remarks = x.Remarks,
                    DueDate = x.DueDate
                   
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (result == null)
            {
                _logger.LogError($"Supplier Verification Request not found : {request.RequestId}");

                throw new NotFoundCustomException(
                    "Supplier Verification Request not found.",
                    "Invalid Request Id.");
            }

            _logger.LogInfo($"Supplier Verification Request fetched successfully : {request.RequestId}");

            return result;
        }
    }
}
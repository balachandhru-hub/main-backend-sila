using Buyer.Domain.Dto;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Application.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Queries.Invitation
{
    public class InvitationSummaryCountQueryHandler
        : IRequestHandler<InvitationSummaryCountQuery, int>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;
        private readonly ISupplierApiClient _supplierService;

        public InvitationSummaryCountQueryHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            ISupplierApiClient supplierService)
        {
            _repository = repository;
            _logger = logger;
            _supplierService = supplierService;
        }

        public async Task<int> Handle(
            InvitationSummaryCountQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInfo(
                $"Getting total invitation count for OrganizationId: {request.OrganizationId}, OrganizationType: {request.OrganizationType}");

            // Buyer
            if (request.OrganizationType.Equals(
                "Buyer",
                StringComparison.OrdinalIgnoreCase))
            {
                var buyerId = await _repository.BuyerBusinessProfile
                    .FindByCondition(x =>
                        x.OrganizationId == request.OrganizationId)
                    .Select(x => x.Id)
                    .FirstOrDefaultAsync(cancellationToken);

                if (buyerId == Guid.Empty)
                {
                    throw new NotFoundCustomException(
                        "Not Found",
                        "Buyer not found for the organization.");
                }

                return await _repository.SupplierVerificationRequest
                    .FindByCondition(x =>
                        x.BuyerOrganizationId == buyerId)
                    .CountAsync(cancellationToken);
            }

            // Supplier
            if (request.OrganizationType.Equals(
                "Supplier",
                StringComparison.OrdinalIgnoreCase))
            {
                var supplierId = await _supplierService.GetSupplierId(
                    cancellationToken);

                if (supplierId == Guid.Empty)
                {
                    throw new NotFoundCustomException(
                        "Not Found",
                        "Supplier not found for the organization.");
                }

                return await _repository.SupplierVerificationRequest
                    .FindByCondition(x =>
                        x.SupplierOrganizationId == supplierId)
                    .CountAsync(cancellationToken);
            }

            // Invalid Organization Type
            throw new BadRequestCustomException(
                "Invalid Organization Type",
                "Organization type must be Buyer or Supplier.");
        }
    }
}
using Buyer.Application.Contracts;
using Buyer.Domain.Common;
using Buyer.Domain.Dto;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Queries.Invitation
{
    public class InvitationSummaryQueryHandler
        : IRequestHandler<InvitationSummaryQuery, SupplierInvitationSummaryDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;
        private readonly ISupplierApiClient _supplierService;

        public InvitationSummaryQueryHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            ISupplierApiClient supplierService)
        {
            _repository = repository;
            _logger = logger;
            _supplierService = supplierService;
        }

        public async Task<SupplierInvitationSummaryDto> Handle(
            InvitationSummaryQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInfo(
                $"Getting invitation summary for OrganizationId: {request.OrganizationId}, Type: {request.OrganizationType}");

            IQueryable<Buyer.Domain.Entities.SupplierVerificationRequest> query;

            // BUYER
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

                _logger.LogInfo(
                    $"Getting invitations for BuyerId: {buyerId}");

                query = _repository.SupplierVerificationRequest
                    .FindByCondition(x =>
                        x.BuyerOrganizationId == buyerId);
            }

            // SUPPLIER
            else if (request.OrganizationType.Equals(
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

                _logger.LogInfo(
                    $"Getting invitations for SupplierId: {supplierId}");

                query = _repository.SupplierVerificationRequest
                    .FindByCondition(x =>
                        x.SupplierOrganizationId == supplierId);
            }

            // INVALID ORGANIZATION TYPE
            else
            {
                throw new BadRequestCustomException(
                    "Invalid Organization Type",
                    "Organization type must be Buyer or Supplier.");
            }

            var result = new SupplierInvitationSummaryDto
            {
                All = await query.CountAsync(cancellationToken),

                Draft = await query.CountAsync(
                    x => x.Status == Common.DRAFT,
                    cancellationToken),

                Pending = await query.CountAsync(
                    x => x.Status == Common.PENDING,
                    cancellationToken),

                Accepted = await query.CountAsync(
                    x => x.Status == Common.ACCEPT,
                    cancellationToken),

                Declined = await query.CountAsync(
                    x => x.Status == Common.DECLINE,
                    cancellationToken)
            };

            _logger.LogInfo(
                $"Invitation summary fetched successfully for OrganizationId: {request.OrganizationId}");

            return result;
        }
    }
}
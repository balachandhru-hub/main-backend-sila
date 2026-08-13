using Buyer.Application.Contracts;
using Buyer.Domain.Dto;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Queries.Invitation
{
    public class SupplierInvitationQueryHandler
        : IRequestHandler<SupplierInvitationQuery, List<RFQListDto>>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;
        private readonly ISupplierApiClient _supplierService;

        public SupplierInvitationQueryHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            ISupplierApiClient supplierService)
        {
            _repository = repository;
            _logger = logger;
            _supplierService = supplierService;
        }

        public async Task<List<RFQListDto>> Handle(
            SupplierInvitationQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInfo(
                "Get All RFQ Master Data for Supplier");

           
            var supplierId = await _supplierService.GetSupplierId(
                cancellationToken);

            if (supplierId == Guid.Empty)
            {
                _logger.LogError("SupplierId was not found.");

                throw new UnauthorizedAccessException(
                    "SupplierId was not found.");
            }

            _logger.LogInfo(
                $"Getting RFQ invitations for SupplierId: {supplierId}");

          
            var invitationData = await _repository.SupplierVerificationRequest
                .FindByCondition(x =>
                    x.SupplierOrganizationId == supplierId)
                .Include(x => x.RFQ)
                .Select(x => new
                {
                    RFQId = x.RFQ.Id,
                    RFQNumber = x.RFQ.RFQNumber,
                    Title = x.RFQ.Title,
                    Description = x.RFQ.Description,
                    EndDate = x.RFQ.EndDate,
                    DeliveryLocation = x.RFQ.DeliveryLocation,

                    BuyerOrganizationId = x.BuyerOrganizationId,

                    Status = x.Status
                })
                
                .ToListAsync(cancellationToken);

                if (!invitationData.Any())
                {
                    _logger.LogError(
                        "No RFQ invitations found for this supplier.");

                    throw new KeyNotFoundException(
                        "No RFQ invitations found for this supplier.");
                }

         
            var buyerOrganizationIds = invitationData
                .Select(x => x.BuyerOrganizationId)
                .Distinct()
                .ToList();

          
            var buyerOrganizations = await _repository.BuyerBusinessProfile
                .FindByCondition(x =>
                    buyerOrganizationIds.Contains(x.Id))
                .Select(x => new
                {
                    x.Id,
                    x.OrganizationName
                })
                .ToDictionaryAsync(
                    x => x.Id,
                    x => x.OrganizationName,
                    cancellationToken);

            // Build final response
            var result = invitationData
                .Select(x =>
                {
                    buyerOrganizations.TryGetValue(
                        x.BuyerOrganizationId,
                        out var organizationName);

                    return new RFQListDto
                    {
                        RFQId = x.RFQId,
                        RFQNumber = x.RFQNumber,
                        Title = x.Title,
                        Description = x.Description,
                        EndDate = x.EndDate,
                        DeliveryLocation = x.DeliveryLocation,

                        OrganizationName = organizationName,

                        // Status from SupplierVerificationRequest
                        Status = x.Status
                    };
                })
                .OrderByDescending(x => x.EndDate)
                .Skip(request.Index)
                .Take(request.Limit)
                .ToList();

            return result;
        }
    }
}
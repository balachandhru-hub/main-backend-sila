using Buyer.Domain.Dto;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Application.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.LoggerServices;
using DocumentFormat.OpenXml.Office2010.Excel;

namespace Buyer.Application.Features.Queries.Invitation
{
    public class BuyerInvitationQueryHandler
        : IRequestHandler<BuyerInvitationQuery, List<RFQListDto>>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;
        private readonly ISupplierApiClient _supplierService;

        public BuyerInvitationQueryHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            ISupplierApiClient supplierService)
        {
            _repository = repository;
            _logger = logger;
            _supplierService = supplierService;
        }

        public async Task<List<RFQListDto>> Handle(
            BuyerInvitationQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInfo(
                $"Get All RFQ Master Data for OrganizationId: {request.OrganizationId}");

            // First get BuyerId using OrganizationId
            var buyerId = await _repository.BuyerBusinessProfile
                .FindByCondition(x =>
                    x.OrganizationId == request.OrganizationId)
                .Select(x => x.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (buyerId == Guid.Empty)
            {
                _logger.LogError(
                    $"Buyer not found for OrganizationId: {request.OrganizationId}");

                throw new KeyNotFoundException(
                    "Buyer not found for the organization.");
            }

            _logger.LogInfo(
                $"BuyerId found: {buyerId}");

            // Get invitation data using BuyerOrganizationId
            var invitationData = await _repository.SupplierVerificationRequest
                .FindByCondition(x =>
                    x.BuyerOrganizationId ==buyerId)
                .Include(x => x.RFQ)
                .Select(x => new
                {
                    Id = x.Id,
                    RFQId = x.RFQ.Id,
                    RFQNumber = x.RFQ.RFQNumber,
                    Title = x.RFQ.Title,
                    Description = x.RFQ.Description,
                    EndDate = x.RFQ.EndDate,
                    DeliveryLocation = x.RFQ.DeliveryLocation,
                    SupplierOrganizationId = x.SupplierOrganizationId,
                    Status = x.Status
                })
            .ToListAsync(cancellationToken);

            if (!invitationData.Any())
            {
                _logger.LogError(
                    "No RFQ invitations found for this buyer.");

                throw new KeyNotFoundException(
                    "No RFQ invitations found for this buyer.");
            }
            // Get unique supplier IDs
            var supplierIds = invitationData
                .Select(x => x.SupplierOrganizationId)
                .Distinct()
                .ToList();

       
            var suppliers = new Dictionary<Guid, SupplierProfileDto>();

            foreach (var supplierId in supplierIds)
            {
                var supplier = await _supplierService.GetSupplierById(
                    supplierId,
                    cancellationToken);

                if (supplier != null)
                {
                    suppliers[supplierId] = supplier;
                }
            }

          
            var result = invitationData
                .Select(x =>
                {
                    suppliers.TryGetValue(
                        x.SupplierOrganizationId,
                        out var supplier);

                    return new RFQListDto
                    {
                        Id= x.Id,
                        RFQId = x.RFQId,
                        RFQNumber = x.RFQNumber,
                        Title = x.Title,
                        Description = x.Description,
                        EndDate = x.EndDate,
                        DeliveryLocation = x.DeliveryLocation,
                        OrganizationName =supplier?.BusinessProfile?.OrganizationName,
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
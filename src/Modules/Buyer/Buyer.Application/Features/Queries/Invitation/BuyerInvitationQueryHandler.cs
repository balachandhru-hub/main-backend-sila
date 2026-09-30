using Buyer.Domain.Dto;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Application.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.LoggerServices;

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

            // Get invitation query using BuyerOrganizationId
            var invitationQuery = _repository.SupplierVerificationRequest
                .FindByCondition(x =>
                    x.BuyerOrganizationId == buyerId);

            // Search filter
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var search = request.Search.Trim();

                invitationQuery = invitationQuery.Where(x =>
                    x.RFQ.RFQNumber.Contains(search) ||
                    x.RFQ.Title.Contains(search) ||
                    x.RFQ.Description.Contains(search));
            }

            // Status filter
            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                var status = request.Status.Trim();

                invitationQuery = invitationQuery.Where(x =>
                    x.Status == status);
            }

            var invitationData = await invitationQuery
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

            // One batch lookup for the names. It skips suppliers the Supplier service cannot
            // find (e.g. an inactive profile) instead of failing, so one missing supplier no
            // longer breaks the whole invitation list; those rows show no organization name.
            var supplierNames = (await _supplierService.GetSupplierNamesByIds(
                    supplierIds,
                    cancellationToken))
                .GroupBy(x => x.SupplierId)
                .ToDictionary(g => g.Key, g => g.First().SupplierName);

            var missingSupplierIds = supplierIds
                .Where(id => !supplierNames.ContainsKey(id))
                .ToList();

            if (missingSupplierIds.Any())
            {
                _logger.LogError(
                    $"Supplier profile not found or inactive for SupplierOrganizationId(s): {string.Join(", ", missingSupplierIds)}");
            }

            var result = invitationData
                .Select(x =>
                {
                    supplierNames.TryGetValue(
                        x.SupplierOrganizationId,
                        out var supplierName);

                    return new RFQListDto
                    {
                        Id = x.Id,
                        RFQId = x.RFQId,
                        RFQNumber = x.RFQNumber,
                        Title = x.Title,
                        Description = x.Description,
                        EndDate = x.EndDate,
                        DeliveryLocation = x.DeliveryLocation,
                        OrganizationName = supplierName,
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
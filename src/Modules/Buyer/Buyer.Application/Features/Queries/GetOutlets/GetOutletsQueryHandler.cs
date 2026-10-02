using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Queries.GetOutlets
{
    public class GetOutletsQueryHandler : IRequestHandler<GetOutletsQuery, List<OutletResponseDto>>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public GetOutletsQueryHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<List<OutletResponseDto>> Handle(GetOutletsQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Fetching outlets. OrganizationId: {request.OrganizationId}, UserId: {request.UserId}");

            BuyerBusinessProfile? buyer = _repository.BuyerBusinessProfile.FindFirstByCondition(
                x => x.OrganizationId == request.OrganizationId && x.IsActive);
            if (buyer == null)
            {
                _logger.LogError($"Buyer not found. OrganizationId: {request.OrganizationId}");
                throw new NotFoundCustomException("Buyer not found.", "The signed-in organization does not have a buyer profile.");
            }

            List<BuyerOutlet> outlets = await _repository.WeeklyBucket.ListOutletsAsync(buyer.Id, cancellationToken);

            // A user assigned to outlets sees only those. A user with no assignment (the buyer administrator) sees all.
            List<Guid> assignedOutletIds = await _repository.BuyerOutletUserMapping
                .FindByCondition(x => x.UserId == request.UserId && x.IsActive)
                .Select(x => x.OutletId)
                .ToListAsync(cancellationToken);
            if (assignedOutletIds.Count > 0)
            {
                outlets = outlets.Where(outlet => assignedOutletIds.Contains(outlet.Id)).ToList();
            }

            List<Guid> flowIds = outlets
                .Where(outlet => outlet.MasterApprovalFlowId != null)
                .Select(outlet => outlet.MasterApprovalFlowId!.Value)
                .Distinct()
                .ToList();
            List<Buyer.Domain.Entities.MasterApprovalFlow> flows = flowIds.Count == 0
                ? new List<Buyer.Domain.Entities.MasterApprovalFlow>()
                : await _repository.MasterApprovalFlow
                    .FindByCondition(x => flowIds.Contains(x.Id) && x.IsActive)
                    .ToListAsync(cancellationToken);

            List<Guid> propertyIds = outlets
                .Where(outlet => outlet.PropertyId != null)
                .Select(outlet => outlet.PropertyId!.Value)
                .Distinct()
                .ToList();
            List<BuyerProperty> properties = propertyIds.Count == 0
                ? new List<BuyerProperty>()
                : await _repository.BuyerProperty
                    .FindByCondition(x => propertyIds.Contains(x.Id) && x.BuyerId == buyer.Id && x.IsActive)
                    .ToListAsync(cancellationToken);

            _logger.LogInfo($"Outlets fetched. Count: {outlets.Count}, BuyerId: {buyer.Id}");
            return outlets.Select(outlet =>
            {
                BuyerProperty? property = properties.FirstOrDefault(x => x.Id == outlet.PropertyId);
                return new OutletResponseDto
                {
                    Id = outlet.Id,
                    OutletName = outlet.OutletName,
                    OutletCode = outlet.OutletCode,
                    Description = outlet.Description,
                    ExternalShipTo = outlet.ExternalShipTo,
                    AddressLine1 = outlet.AddressLine1,
                    City = outlet.City,
                    Country = outlet.Country,
                    MasterApprovalFlowId = outlet.MasterApprovalFlowId,
                    ApprovalName = flows.FirstOrDefault(flow => flow.Id == outlet.MasterApprovalFlowId)?.ApprovalName,
                    PropertyId = outlet.PropertyId,
                    PropertyName = property?.PropertyName,
                    PlantCode = property?.PlantCode,
                    CompanyCode = property?.CompanyCode,
                    StorageLocation = outlet.StorageLocation
                };
            }).ToList();
        }
    }
}

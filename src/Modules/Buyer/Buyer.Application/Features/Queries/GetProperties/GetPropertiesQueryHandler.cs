using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Queries.GetProperties
{
    public class GetPropertiesQueryHandler : IRequestHandler<GetPropertiesQuery, List<PropertyResponseDto>>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public GetPropertiesQueryHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<List<PropertyResponseDto>> Handle(GetPropertiesQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Fetching properties. OrganizationId: {request.OrganizationId}");

            BuyerBusinessProfile? buyer = _repository.BuyerBusinessProfile.FindFirstByCondition(
                x => x.OrganizationId == request.OrganizationId && x.IsActive);
            if (buyer == null)
            {
                _logger.LogError($"Buyer not found. OrganizationId: {request.OrganizationId}");
                throw new NotFoundCustomException("Buyer not found.", "The signed-in organization does not have a buyer profile.");
            }

            List<BuyerProperty> properties = await _repository.BuyerProperty
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive)
                .OrderBy(x => x.PropertyName)
                .ToListAsync(cancellationToken);

            List<Guid> flowIds = properties
                .Where(property => property.MasterApprovalFlowId != null)
                .Select(property => property.MasterApprovalFlowId!.Value)
                .Distinct()
                .ToList();
            List<Buyer.Domain.Entities.MasterApprovalFlow> flows = flowIds.Count == 0
                ? new List<Buyer.Domain.Entities.MasterApprovalFlow>()
                : await _repository.MasterApprovalFlow
                    .FindByCondition(x => flowIds.Contains(x.Id) && x.BuyerId == buyer.Id && x.IsActive)
                    .ToListAsync(cancellationToken);

            _logger.LogInfo($"Properties fetched. Count: {properties.Count}, BuyerId: {buyer.Id}");
            return properties.Select(property => new PropertyResponseDto
            {
                Id = property.Id,
                CompanyCode = property.CompanyCode,
                PlantCode = property.PlantCode,
                PropertyName = property.PropertyName,
                MasterApprovalFlowId = property.MasterApprovalFlowId,
                ApprovalName = flows.FirstOrDefault(flow => flow.Id == property.MasterApprovalFlowId)?.ApprovalName
            }).ToList();
        }
    }
}

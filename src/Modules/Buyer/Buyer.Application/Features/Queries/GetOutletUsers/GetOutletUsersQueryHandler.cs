using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Queries.GetOutletUsers
{
    public class GetOutletUsersQueryHandler : IRequestHandler<GetOutletUsersQuery, List<OutletUserMappingDto>>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public GetOutletUsersQueryHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<List<OutletUserMappingDto>> Handle(GetOutletUsersQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Fetching outlet users. OrganizationId: {request.OrganizationId}");

            BuyerBusinessProfile? buyer = _repository.BuyerBusinessProfile.FindFirstByCondition(
                x => x.OrganizationId == request.OrganizationId && x.IsActive);
            if (buyer == null)
            {
                _logger.LogError($"Buyer not found. OrganizationId: {request.OrganizationId}");
                throw new NotFoundCustomException("Buyer not found.", "The signed-in organization does not have a buyer profile.");
            }

            List<Guid> outletIds = (await _repository.Wishlist.ListOutletsAsync(buyer.Id, cancellationToken))
                .Select(outlet => outlet.Id)
                .ToList();
            List<BuyerOutletUserMapping> mappings = await _repository.BuyerOutletUserMapping
                .FindByCondition(x => outletIds.Contains(x.OutletId) && x.IsActive)
                .ToListAsync(cancellationToken);

            _logger.LogInfo($"Outlet users fetched. Count: {mappings.Count}, BuyerId: {buyer.Id}");
            return mappings
                .GroupBy(mapping => mapping.UserId)
                .Select(group => new OutletUserMappingDto
                {
                    UserId = group.Key,
                    OutletIds = group.Select(mapping => mapping.OutletId).Distinct().ToList()
                })
                .ToList();
        }
    }
}

using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
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
            _logger.LogInfo($"Fetching outlets. OrganizationId: {request.OrganizationId}");

            BuyerBusinessProfile? buyer = _repository.BuyerBusinessProfile.FindFirstByCondition(
                x => x.OrganizationId == request.OrganizationId && x.IsActive);
            if (buyer == null)
            {
                _logger.LogError($"Buyer not found. OrganizationId: {request.OrganizationId}");
                throw new NotFoundCustomException("Buyer not found.", "The signed-in organization does not have a buyer profile.");
            }

            List<BuyerOutlet> outlets = await _repository.Wishlist.ListOutletsAsync(buyer.Id, cancellationToken);
            _logger.LogInfo($"Outlets fetched. Count: {outlets.Count}, BuyerId: {buyer.Id}");
            return outlets.Select(outlet => new OutletResponseDto
            {
                Id = outlet.Id,
                OutletName = outlet.OutletName,
                OutletCode = outlet.OutletCode,
                Description = outlet.Description,
                ExternalShipTo = outlet.ExternalShipTo,
                AddressLine1 = outlet.AddressLine1,
                City = outlet.City,
                Country = outlet.Country
            }).ToList();
        }
    }
}

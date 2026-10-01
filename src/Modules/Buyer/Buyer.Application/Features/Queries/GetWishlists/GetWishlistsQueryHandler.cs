using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Queries.GetWishlists
{
    public class GetWishlistsQueryHandler : IRequestHandler<GetWishlistsQuery, List<WishlistListItemDto>>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public GetWishlistsQueryHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<List<WishlistListItemDto>> Handle(GetWishlistsQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Fetching wishlists. OrganizationId: {request.OrganizationId}, Index: {request.Index}, Limit: {request.Limit}");

            BuyerBusinessProfile buyer = GetBuyer(request.OrganizationId);
            int index = request.Index < 0 ? 0 : request.Index;
            int limit = request.Limit <= 0 ? 20 : Math.Min(request.Limit, 100);
            List<Wishlist> rows = await _repository.Wishlist
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive)
                .OrderByDescending(x => x.DateUpdated)
                .Skip(index)
                .Take(limit)
                .ToListAsync(cancellationToken);
            List<BuyerOutlet> outlets = await _repository.Wishlist.ListOutletsAsync(buyer.Id, cancellationToken);
            Dictionary<Guid, string> outletNames = outlets.ToDictionary(x => x.Id, x => x.OutletName);

            _logger.LogInfo($"Wishlists fetched. Count: {rows.Count}, BuyerId: {buyer.Id}");
            return rows.Select(row => new WishlistListItemDto
            {
                Id = row.Id,
                WishlistName = row.WishlistName,
                OutletName = outletNames.TryGetValue(row.OutletId, out string? name) ? name : null,
                CreatedBy = row.CreatedBy,
                DateCreated = row.DateCreated,
                Status = row.Status,
                ApprovalName = row.ApprovalName,
                BuyerErpDocumentNumber = row.BuyerErpDocumentNumber,
                SupplierErpDocumentNumber = row.SupplierErpDocumentNumber,
                LastError = row.LastError
            }).ToList();
        }

        private BuyerBusinessProfile GetBuyer(Guid organizationId)
        {
            BuyerBusinessProfile? buyer = _repository.BuyerBusinessProfile.FindFirstByCondition(
                x => x.OrganizationId == organizationId && x.IsActive);
            if (buyer == null)
            {
                _logger.LogError($"Buyer not found. OrganizationId: {organizationId}");
                throw new NotFoundCustomException("Buyer not found.", "The signed-in organization does not have a buyer profile.");
            }

            return buyer;
        }
    }
}

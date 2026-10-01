using Buyer.Domain.Common;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Queries.GetWishlist
{
    public class GetWishlistQueryHandler : IRequestHandler<GetWishlistQuery, WishlistResponseDto>
    {
        private static readonly HashSet<string> EditableStatuses = new(StringComparer.OrdinalIgnoreCase)
        {
            Common.WISHLIST_DRAFT,
            Common.WISHLIST_REJECTED
        };

        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public GetWishlistQueryHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<WishlistResponseDto> Handle(GetWishlistQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Fetching wishlist. WishlistId: {request.WishlistId}, OrganizationId: {request.OrganizationId}");

            BuyerBusinessProfile buyer = GetBuyer(request.OrganizationId);
            Wishlist? wishlist = await _repository.Wishlist.GetTrackedAsync(request.WishlistId, buyer.Id, cancellationToken);
            if (wishlist == null)
            {
                _logger.LogError($"Wishlist not found. WishlistId: {request.WishlistId}, BuyerId: {buyer.Id}");
                throw new NotFoundCustomException("Wishlist not found.", "No wishlist exists for this buyer organization.");
            }

            BuyerOutlet? outlet = await _repository.Wishlist.GetOutletAsync(wishlist.OutletId, wishlist.BuyerId, cancellationToken);
            List<WishlistItem> items = await _repository.Wishlist.GetItemsAsync(wishlist.Id, cancellationToken);
            WishlistApprovalFlow? flow = await _repository.Wishlist.GetApprovalFlowAsync(wishlist.Id, cancellationToken);
            List<WishlistApprovalUserMapping> steps = flow == null
                ? new List<WishlistApprovalUserMapping>()
                : await _repository.Wishlist.GetApprovalUsersAsync(flow.Id, cancellationToken);

            _logger.LogInfo($"Wishlist fetched. WishlistId: {wishlist.Id}");
            return new WishlistResponseDto
            {
                Id = wishlist.Id,
                BuyerOrganizationId = wishlist.BuyerOrganizationId,
                BuyerId = wishlist.BuyerId,
                OutletId = wishlist.OutletId,
                OutletName = outlet?.OutletName,
                WishlistName = wishlist.WishlistName,
                Description = wishlist.Description,
                SupplierOrganizationId = wishlist.SupplierOrganizationId,
                SupplierName = wishlist.SupplierName,
                Status = wishlist.Status,
                MasterApprovalFlowId = wishlist.MasterApprovalFlowId,
                ApprovalName = wishlist.ApprovalName,
                CreatedBy = wishlist.CreatedBy,
                DateCreated = wishlist.DateCreated,
                UpdatedBy = wishlist.UpdatedBy,
                DateUpdated = wishlist.DateUpdated,
                SubmittedOn = wishlist.SubmittedOn,
                FinalApprovedOn = wishlist.FinalApprovedOn,
                BuyerErpDocumentType = wishlist.BuyerErpDocumentType,
                BuyerErpDocumentNumber = wishlist.BuyerErpDocumentNumber,
                SupplierErpDocumentNumber = wishlist.SupplierErpDocumentNumber,
                Currency = wishlist.Currency,
                DeliveryInstruction = wishlist.DeliveryInstruction,
                RequiredDate = wishlist.RequiredDate,
                LastError = wishlist.LastError,
                IsFrozen = !EditableStatuses.Contains(wishlist.Status),
                Items = items.Select(item => new WishlistItemResponseDto
                {
                    Id = item.Id,
                    MaterialId = item.MaterialId,
                    MaterialCode = item.MaterialCode,
                    MaterialName = item.MaterialName,
                    UnitOfMeasure = item.UnitOfMeasure,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    Currency = item.Currency,
                    RequiredDate = item.RequiredDate
                }).ToList(),
                ApprovalSteps = steps.Select(step => new WishlistApprovalStepDto
                {
                    UserId = step.UserId,
                    Order = step.Order,
                    Status = step.Status,
                    Comment = step.Comment,
                    ActedOn = step.ActedOn
                }).ToList()
            };
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

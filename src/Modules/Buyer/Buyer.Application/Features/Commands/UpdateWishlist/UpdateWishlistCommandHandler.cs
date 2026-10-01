using Buyer.Application.Contracts;
using Buyer.Domain.Common;
using Buyer.Domain.Dto;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.UpdateWishlist
{
    public class UpdateWishlistCommandHandler : IRequestHandler<UpdateWishlistCommand, Unit>
    {
        private static readonly HashSet<string> EditableStatuses = new(StringComparer.OrdinalIgnoreCase)
        {
            Common.WISHLIST_DRAFT,
            Common.WISHLIST_REJECTED
        };

        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;
        private readonly ISupplierApiClient _supplierApiClient;

        public UpdateWishlistCommandHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            ISupplierApiClient supplierApiClient)
        {
            _repository = repository;
            _logger = logger;
            _supplierApiClient = supplierApiClient;
        }

        public async Task<Unit> Handle(UpdateWishlistCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInfo(
                $"Updating wishlist. WishlistId: {request.WishlistId}, OrganizationId: {request.OrganizationId}, UserId: {request.UserId}");

            WishlistWriteDto dto = request.Request;
            if (dto.OutletId == Guid.Empty)
            {
                throw new BadRequestCustomException("Outlet is required.", "Select an outlet.");
            }

            if (string.IsNullOrWhiteSpace(dto.WishlistName))
            {
                throw new BadRequestCustomException("Wishlist name is required.", "Enter a wishlist name.");
            }

            BuyerBusinessProfile buyer = GetBuyer(request.OrganizationId);
            Wishlist? wishlist = await _repository.Wishlist.GetTrackedAsync(request.WishlistId, buyer.Id, cancellationToken);
            if (wishlist == null)
            {
                _logger.LogError($"Wishlist not found. WishlistId: {request.WishlistId}, BuyerId: {buyer.Id}");
                throw new NotFoundCustomException("Wishlist not found.", "No wishlist exists for this buyer organization.");
            }

            if (!EditableStatuses.Contains(wishlist.Status))
            {
                throw new BadRequestCustomException(
                    "Wishlist is frozen.",
                    "A wishlist can be edited only before it is submitted for approval, or after it is rejected.");
            }

            BuyerOutlet? outlet = await _repository.Wishlist.GetOutletAsync(dto.OutletId, buyer.Id, cancellationToken);
            if (outlet == null)
            {
                _logger.LogError($"Outlet not found. OutletId: {dto.OutletId}, BuyerId: {buyer.Id}");
                throw new NotFoundCustomException("Outlet not found.", "Select an outlet that belongs to this buyer organization.");
            }

            // A user assigned to outlets can raise wishlists only for those outlets.
            List<Guid> assignedOutletIds = await _repository.BuyerOutletUserMapping
                .FindByCondition(x => x.UserId == request.UserId && x.IsActive)
                .Select(x => x.OutletId)
                .ToListAsync(cancellationToken);
            if (assignedOutletIds.Count > 0 && !assignedOutletIds.Contains(outlet.Id))
            {
                _logger.LogError($"Outlet is not assigned to the user. OutletId: {outlet.Id}, UserId: {request.UserId}");
                throw new BadRequestCustomException("Outlet is not assigned to you.", "Select one of your outlets.");
            }

            // The approval flow comes from the outlet. A draft stays editable and may be saved before the outlet has one.
            bool isDraft = dto.SaveAsDraft;
            Guid? flowId = outlet.MasterApprovalFlowId ?? dto.MasterApprovalFlowId;
            bool hasFlow = flowId != null && flowId != Guid.Empty;
            if (!isDraft && !hasFlow)
            {
                throw new BadRequestCustomException(
                    "Approval flow is required.",
                    "This outlet has no approval flow. Ask your buyer administrator to assign one to the outlet.");
            }

            MasterApprovalFlow? flow = null;
            if (hasFlow)
            {
                flow = await _repository.MasterApprovalFlow.FindFirstByConditionAsync(
                    x => x.Id == flowId && x.BuyerId == buyer.Id && x.IsActive);
                if (flow == null)
                {
                    _logger.LogError($"Approval flow not found. ApprovalFlowId: {flowId}");
                    throw new NotFoundCustomException("Approval flow not found.", "The approval flow does not belong to this buyer organization.");
                }

                if (!string.Equals(flow.Type, Common.WISHLIST_APPROVAL_TYPE, StringComparison.OrdinalIgnoreCase))
                {
                    throw new BadRequestCustomException("Approval flow type is not Wishlist.", "Select an approval configuration of type WISHLIST.");
                }
            }

            List<WishlistItem> items = await BuildItemsAsync(wishlist.Id, dto, cancellationToken);
            if (items.Count == 0)
            {
                throw new BadRequestCustomException("Wishlist has no items.", "Add at least one product from the product catalog.");
            }

            wishlist.OutletId = outlet.Id;
            wishlist.WishlistName = dto.WishlistName.Trim();
            wishlist.Description = dto.Description;
            wishlist.SupplierOrganizationId = dto.SupplierOrganizationId;
            wishlist.SupplierName = dto.SupplierName;
            wishlist.MasterApprovalFlowId = flow?.Id;
            wishlist.ApprovalName = flow?.ApprovalName;
            wishlist.Currency = dto.Currency;
            wishlist.DeliveryInstruction = dto.DeliveryInstruction;
            wishlist.RequiredDate = dto.RequiredDate;
            wishlist.LastError = null;
            if (isDraft || flow == null)
            {
                wishlist.Status = Common.WISHLIST_DRAFT;
            }
            else
            {
                wishlist.Status = Common.WISHLIST_PENDING_APPROVAL;
                wishlist.SubmittedOn = DateTime.UtcNow;
                wishlist.SubmittedBy = request.UserId;
            }

            List<WishlistItem> existing = await _repository.Wishlist.GetItemsAsync(wishlist.Id, cancellationToken);
            _repository.WishlistItem.DeleteRange(existing);
            foreach (WishlistItem item in items)
            {
                _repository.WishlistItem.Create(item);
            }

            if (isDraft || flow == null)
            {
                AddAudit(wishlist.Id, request.UserId, Common.AUDIT_MODIFIED, "Wishlist draft updated.");
            }
            else
            {
                await CopyApprovalUsersAsync(wishlist, flow, cancellationToken);
                AddAudit(wishlist.Id, request.UserId, Common.AUDIT_MODIFIED, "Wishlist updated and sent for approval.");
            }

            await _repository.SaveAsync();

            _logger.LogInfo($"Wishlist updated. WishlistId: {wishlist.Id}, UserId: {request.UserId}");
            return Unit.Value;
        }

        // Wishlist items are products of the supplier product catalog. MaterialId holds the catalog id;
        // name, unit and price are read from the Supplier service, not from the request.
        private async Task<List<WishlistItem>> BuildItemsAsync(
            Guid wishlistId,
            WishlistWriteDto request,
            CancellationToken cancellationToken)
        {
            List<WishlistItem> items = new List<WishlistItem>();
            foreach (WishlistItemWriteDto line in request.Items ?? new List<WishlistItemWriteDto>())
            {
                if (line.Quantity <= 0)
                {
                    throw new BadRequestCustomException("Quantity must be greater than zero.", "Enter a quantity for every product.");
                }

                BuyerCatalogItemDto? product = await _supplierApiClient.GetBuyerCatalogById(line.MaterialId, cancellationToken);
                if (product == null || product.CatalogId == null)
                {
                    _logger.LogError($"Product not found in the product catalog. CatalogId: {line.MaterialId}");
                    throw new NotFoundCustomException("Product not found.", "Select a product from the product catalog.");
                }

                items.Add(new WishlistItem
                {
                    Id = Guid.NewGuid(),
                    WishlistId = wishlistId,
                    MaterialId = product.CatalogId.Value,
                    MaterialCode = product.CatalogId.Value.ToString(),
                    MaterialName = product.CatalogName ?? product.Description ?? string.Empty,
                    UnitOfMeasure = product.UnitOfMeasure,
                    Quantity = line.Quantity,
                    UnitPrice = product.Price ?? line.UnitPrice,
                    Currency = product.Currency ?? line.Currency ?? request.Currency,
                    RequiredDate = line.RequiredDate ?? request.RequiredDate
                });
            }

            return items;
        }

        private async Task CopyApprovalUsersAsync(Wishlist wishlist, MasterApprovalFlow flow, CancellationToken cancellationToken)
        {
            List<ApprovalFlowUserMapping> templateUsers = await _repository.ApprovalFlowUserMapping
                .FindByCondition(x => x.ApprovalFlowId == flow.Id && x.IsActive)
                .OrderBy(x => x.Order)
                .ToListAsync(cancellationToken);
            if (templateUsers.Count == 0)
            {
                _logger.LogError($"Approval flow has no approvers. ApprovalFlowId: {flow.Id}");
                throw new BadRequestCustomException("Approval flow has no approvers.", "Add approvers to the selected approval flow.");
            }

            WishlistApprovalFlow? instance = await _repository.Wishlist.GetApprovalFlowAsync(wishlist.Id, cancellationToken);
            if (instance == null)
            {
                instance = new WishlistApprovalFlow
                {
                    Id = Guid.NewGuid(),
                    WishlistId = wishlist.Id,
                    ApprovalCode = flow.ApprovalCode,
                    ApprovalName = flow.ApprovalName,
                    MasterApprovalFlowId = flow.Id,
                    Type = flow.Type,
                    TotalAmount = flow.TotalAmount,
                    Currency = flow.Currency
                };
                _repository.WishlistApprovalFlow.Create(instance);
            }
            else
            {
                instance.ApprovalCode = flow.ApprovalCode;
                instance.ApprovalName = flow.ApprovalName;
                instance.MasterApprovalFlowId = flow.Id;
                instance.Type = flow.Type;
                instance.TotalAmount = flow.TotalAmount;
                instance.Currency = flow.Currency;
                List<WishlistApprovalUserMapping> previous = await _repository.Wishlist.GetApprovalUsersAsync(instance.Id, cancellationToken);
                _repository.WishlistApprovalUserMapping.DeleteRange(previous);
            }

            foreach (ApprovalFlowUserMapping templateUser in templateUsers)
            {
                _repository.WishlistApprovalUserMapping.Create(new WishlistApprovalUserMapping
                {
                    Id = Guid.NewGuid(),
                    WishlistApprovalFlowId = instance.Id,
                    UserId = templateUser.UserId,
                    Order = templateUser.Order,
                    Status = Common.PENDING
                });
            }
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

        private void AddAudit(Guid wishlistId, Guid userId, string action, string? detail)
        {
            _repository.WishlistAudit.Create(new WishlistAudit
            {
                Id = Guid.NewGuid(),
                WishlistId = wishlistId,
                Action = action,
                Detail = detail,
                ActorUserId = userId
            });
        }
    }
}

using Buyer.Domain.Common;
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

        public UpdateWishlistCommandHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
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

            if (dto.MasterApprovalFlowId == null || dto.MasterApprovalFlowId == Guid.Empty)
            {
                throw new BadRequestCustomException("Approval flow is required.", "Select an approval flow.");
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
                    "Submitted wishlists cannot be edited until they are rejected.");
            }

            BuyerOutlet? outlet = await _repository.Wishlist.GetOutletAsync(dto.OutletId, buyer.Id, cancellationToken);
            if (outlet == null)
            {
                _logger.LogError($"Outlet not found. OutletId: {dto.OutletId}, BuyerId: {buyer.Id}");
                throw new NotFoundCustomException("Outlet not found.", "Select an outlet that belongs to this buyer organization.");
            }

            MasterApprovalFlow? flow = await _repository.MasterApprovalFlow.FindFirstByConditionAsync(
                x => x.Id == dto.MasterApprovalFlowId && x.BuyerId == buyer.Id && x.IsActive);
            if (flow == null)
            {
                _logger.LogError($"Approval flow not found. ApprovalFlowId: {dto.MasterApprovalFlowId}");
                throw new NotFoundCustomException("Approval flow not found.", "The approval flow does not belong to this buyer organization.");
            }

            if (!string.Equals(flow.Type, Common.WISHLIST_APPROVAL_TYPE, StringComparison.OrdinalIgnoreCase))
            {
                throw new BadRequestCustomException("Approval flow type is not Wishlist.", "Select an approval configuration of type WISHLIST.");
            }

            List<WishlistItem> items = await BuildItemsAsync(buyer.Id, wishlist.Id, dto, cancellationToken);
            if (items.Count == 0)
            {
                throw new BadRequestCustomException("Wishlist has no items.", "Add at least one material.");
            }

            wishlist.OutletId = outlet.Id;
            wishlist.WishlistName = dto.WishlistName.Trim();
            wishlist.Description = dto.Description;
            wishlist.SupplierOrganizationId = dto.SupplierOrganizationId;
            wishlist.SupplierName = dto.SupplierName;
            wishlist.MasterApprovalFlowId = flow.Id;
            wishlist.ApprovalName = flow.ApprovalName;
            wishlist.Currency = dto.Currency;
            wishlist.DeliveryInstruction = dto.DeliveryInstruction;
            wishlist.RequiredDate = dto.RequiredDate;
            wishlist.Status = Common.WISHLIST_PENDING_APPROVAL;
            wishlist.SubmittedOn = DateTime.UtcNow;
            wishlist.SubmittedBy = request.UserId;
            wishlist.LastError = null;

            List<WishlistItem> existing = await _repository.Wishlist.GetItemsAsync(wishlist.Id, cancellationToken);
            _repository.Wishlist.RemoveRange(existing);
            foreach (WishlistItem item in items)
            {
                _repository.Wishlist.Add(item);
            }

            await CopyApprovalUsersAsync(wishlist, flow, cancellationToken);
            AddAudit(wishlist.Id, request.UserId, Common.AUDIT_MODIFIED, "Wishlist updated and sent for approval again.");
            await _repository.SaveAsync();

            _logger.LogInfo($"Wishlist updated. WishlistId: {wishlist.Id}, UserId: {request.UserId}");
            return Unit.Value;
        }

        private async Task<List<WishlistItem>> BuildItemsAsync(
            Guid buyerId,
            Guid wishlistId,
            WishlistWriteDto request,
            CancellationToken cancellationToken)
        {
            List<WishlistItem> items = new List<WishlistItem>();
            foreach (WishlistItemWriteDto line in request.Items ?? new List<WishlistItemWriteDto>())
            {
                if (line.Quantity <= 0)
                {
                    throw new BadRequestCustomException("Quantity must be greater than zero.", "Enter a quantity for every material.");
                }

                ItemBuyerMaster? material = await _repository.ItemBuyerMaster.FindFirstByConditionAsync(
                    x => x.Id == line.MaterialId && x.BuyerId == buyerId && x.IsActive);
                if (material == null)
                {
                    _logger.LogError($"Material not found. MaterialId: {line.MaterialId}, BuyerId: {buyerId}");
                    throw new NotFoundCustomException("Material not found.", "Select a material from the buyer catalog.");
                }

                items.Add(new WishlistItem
                {
                    Id = Guid.NewGuid(),
                    WishlistId = wishlistId,
                    MaterialId = material.Id,
                    MaterialCode = material.MaterialCode,
                    MaterialName = material.Description,
                    UnitOfMeasure = material.OrderUnitOfMeasure ?? material.BaseUnitOfMeasure,
                    Quantity = line.Quantity,
                    UnitPrice = line.UnitPrice,
                    Currency = line.Currency ?? request.Currency,
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
                _repository.Wishlist.Add(instance);
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
                _repository.Wishlist.RemoveRange(previous);
            }

            foreach (ApprovalFlowUserMapping templateUser in templateUsers)
            {
                _repository.Wishlist.Add(new WishlistApprovalUserMapping
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
            _repository.Wishlist.Add(new WishlistAudit
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

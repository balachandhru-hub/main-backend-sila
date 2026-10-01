using Buyer.Domain.Common;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.CreateWishlist
{
    public class CreateWishlistCommandHandler : IRequestHandler<CreateWishlistCommand, Guid>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public CreateWishlistCommandHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<Guid> Handle(CreateWishlistCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Creating wishlist. OrganizationId: {request.OrganizationId}, UserId: {request.UserId}");

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
                _logger.LogError($"Approval flow not found. ApprovalFlowId: {dto.MasterApprovalFlowId}, BuyerId: {buyer.Id}");
                throw new NotFoundCustomException("Approval flow not found.", "The approval flow does not belong to this buyer organization.");
            }

            if (!string.Equals(flow.Type, Common.WISHLIST_APPROVAL_TYPE, StringComparison.OrdinalIgnoreCase))
            {
                throw new BadRequestCustomException("Approval flow type is not Wishlist.", "Select an approval configuration of type WISHLIST.");
            }

            List<WishlistItem> items = await BuildItemsAsync(buyer.Id, dto, cancellationToken);
            if (items.Count == 0)
            {
                throw new BadRequestCustomException("Wishlist has no items.", "Add at least one material.");
            }

            Wishlist wishlist = new Wishlist
            {
                Id = Guid.NewGuid(),
                BuyerOrganizationId = request.OrganizationId,
                BuyerId = buyer.Id,
                OutletId = outlet.Id,
                WishlistName = dto.WishlistName.Trim(),
                Description = dto.Description,
                SupplierOrganizationId = dto.SupplierOrganizationId,
                SupplierName = dto.SupplierName,
                Status = Common.WISHLIST_PENDING_APPROVAL,
                MasterApprovalFlowId = flow.Id,
                ApprovalName = flow.ApprovalName,
                Currency = dto.Currency,
                DeliveryInstruction = dto.DeliveryInstruction,
                RequiredDate = dto.RequiredDate,
                SubmittedOn = DateTime.UtcNow,
                SubmittedBy = request.UserId
            };
            _repository.Wishlist.Create(wishlist);
            foreach (WishlistItem item in items)
            {
                item.WishlistId = wishlist.Id;
                _repository.Wishlist.Add(item);
            }

            await CopyApprovalUsersAsync(wishlist, flow, cancellationToken);
            AddAudit(wishlist.Id, request.UserId, Common.AUDIT_CREATED, "Wishlist created and sent for approval.");
            await _repository.SaveAsync();

            _logger.LogInfo($"Wishlist created. WishlistId: {wishlist.Id}, OrganizationId: {request.OrganizationId}, UserId: {request.UserId}");
            return wishlist.Id;
        }

        private async Task<List<WishlistItem>> BuildItemsAsync(Guid buyerId, WishlistWriteDto request, CancellationToken cancellationToken)
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

            WishlistApprovalFlow instance = new WishlistApprovalFlow
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

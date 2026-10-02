using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Commands.UpdatePersonalWishlistItem
{
    public class UpdatePersonalWishlistItemCommand : IRequest<Unit>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid WishlistId { get; set; }
        public Guid ItemId { get; set; }
        public QuantityWriteDto Request { get; set; } = new();
    }
}

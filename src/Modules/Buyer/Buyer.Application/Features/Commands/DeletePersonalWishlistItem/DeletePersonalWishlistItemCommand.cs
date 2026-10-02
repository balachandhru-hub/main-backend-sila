using MediatR;

namespace Buyer.Application.Features.Commands.DeletePersonalWishlistItem
{
    public class DeletePersonalWishlistItemCommand : IRequest<Unit>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid WishlistId { get; set; }
        public Guid ItemId { get; set; }
    }
}

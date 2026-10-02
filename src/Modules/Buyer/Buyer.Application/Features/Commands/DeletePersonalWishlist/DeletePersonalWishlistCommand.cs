using MediatR;

namespace Buyer.Application.Features.Commands.DeletePersonalWishlist
{
    public class DeletePersonalWishlistCommand : IRequest<Unit>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid WishlistId { get; set; }
    }
}

using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Commands.DecideWishlist
{
    public class DecideWishlistCommand : IRequest<Unit>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid WishlistId { get; set; }
        public WishlistDecisionDto Decision { get; set; } = new();
    }
}

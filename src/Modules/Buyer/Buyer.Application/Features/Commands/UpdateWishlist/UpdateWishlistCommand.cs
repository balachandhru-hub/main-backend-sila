using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Commands.UpdateWishlist
{
    public class UpdateWishlistCommand : IRequest<Unit>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid WishlistId { get; set; }
        public WishlistWriteDto Request { get; set; } = new();
    }
}

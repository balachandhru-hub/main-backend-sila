using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Commands.CreateWishlist
{
    public class CreateWishlistCommand : IRequest<Guid>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public WishlistWriteDto Request { get; set; } = new();
    }
}

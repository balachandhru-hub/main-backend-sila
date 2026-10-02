using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Commands.UpdatePersonalWishlist
{
    public class UpdatePersonalWishlistCommand : IRequest<Unit>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid WishlistId { get; set; }
        public PersonalWishlistWriteDto Request { get; set; } = new();
    }
}

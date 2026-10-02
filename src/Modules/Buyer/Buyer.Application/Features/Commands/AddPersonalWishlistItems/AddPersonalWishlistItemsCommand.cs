using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Commands.AddPersonalWishlistItems
{
    public class AddPersonalWishlistItemsCommand : IRequest<Unit>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid WishlistId { get; set; }
        public PersonalWishlistItemsWriteDto Request { get; set; } = new();
    }
}

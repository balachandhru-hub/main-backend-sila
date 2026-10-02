using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Queries.GetPersonalWishlist
{
    public class GetPersonalWishlistQuery : IRequest<PersonalWishlistResponseDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid WishlistId { get; set; }
    }
}

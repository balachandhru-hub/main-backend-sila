using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Queries.GetPersonalWishlists
{
    public class GetPersonalWishlistsQuery : IRequest<List<PersonalWishlistListItemDto>>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
    }
}

using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Queries.GetWishlists
{
    public class GetWishlistsQuery : IRequest<List<WishlistListItemDto>>
    {
        public Guid OrganizationId { get; set; }
        public int Index { get; set; }
        public int Limit { get; set; }
    }
}

using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Queries.GetWishlist
{
    public class GetWishlistQuery : IRequest<WishlistResponseDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid WishlistId { get; set; }
    }
}

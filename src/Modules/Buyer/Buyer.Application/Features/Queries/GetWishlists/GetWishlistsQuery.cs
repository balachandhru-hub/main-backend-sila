using Buyer.Application.Services;
using Buyer.Domain.Dtos;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Queries.GetWishlists
{
    public class GetWishlistsQuery : IRequest<List<WishlistListItemDto>>
    {
        public Guid OrganizationId { get; set; }
        public int Index { get; set; }
        public int Limit { get; set; }
    }
}

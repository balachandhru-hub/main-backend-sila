using Buyer.Application.Services;
using Buyer.Domain.Dtos;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Queries.GetWishlist
{
    public class GetWishlistQuery : IRequest<WishlistResponseDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid WishlistId { get; set; }
    }
}

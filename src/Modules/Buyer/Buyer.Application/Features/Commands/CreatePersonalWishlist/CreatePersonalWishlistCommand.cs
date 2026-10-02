using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Commands.CreatePersonalWishlist
{
    public class CreatePersonalWishlistCommand : IRequest<Guid>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public PersonalWishlistWriteDto Request { get; set; } = new();
    }
}

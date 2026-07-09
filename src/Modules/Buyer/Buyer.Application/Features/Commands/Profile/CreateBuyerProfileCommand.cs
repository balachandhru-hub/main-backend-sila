using Buyer.Domain.Dto;
using MediatR;

namespace Buyer.Application.Features.Profile.Commands
{
    public record CreateBuyerProfileCommand(
        CreateBuyerDto CreateBuyerDto
    ) : IRequest<Guid>;
}
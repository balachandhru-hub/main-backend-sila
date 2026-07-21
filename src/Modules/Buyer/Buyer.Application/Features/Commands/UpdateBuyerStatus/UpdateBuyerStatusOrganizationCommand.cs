using Buyer.Domain.Dto;
using MediatR;


namespace Buyer.Application.Features.Commands.Buyer.UpdateBuyerStatusOrganization
{
    public class UpdateBuyerStatusOrganizationCommand : IRequest<bool>
    {
        public UpdateBuyerStatusDto Buyer { get; set; } = default!;
    }
}
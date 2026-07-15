using Buyer.Domain.Dto;
using MediatR;

namespace Buyer.Application.Features.Commands.Buyer.UpdateRejectedBuyer
{
    public class UpdateRejectedBuyerCommand : IRequest<bool>
    {
        public UpdateRejectedBuyerDto Buyer { get; set; } = default!;
    }
}
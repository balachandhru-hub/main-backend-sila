
using MediatR;
using SharedKernel.Dto;
namespace Buyer.Application.Features.StatusUpdate.Commands
{
    public class UpdateBuyerStatusCommand : IRequest<bool>
    {
        public Guid BuyerId { get; set; }

        public string Status { get; set; }

        public string? Comments { get; set; }
    }
}
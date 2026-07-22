using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Commands.CreateRFQ
{
    public class CreateRFQCommand : IRequest<bool>
    {
        public CreateRFQDto RFQ { get; set; } = default!;
    }
}
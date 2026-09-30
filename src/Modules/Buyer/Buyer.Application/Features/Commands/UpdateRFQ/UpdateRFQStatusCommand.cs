using Buyer.Domain.Dto;
using MediatR;

namespace Buyer.Application.Features.Commands.UpdateRFQ
{
    public class UpdateRFQStatusCommand : IRequest<bool>
    {
        public UpdateRFQStatusDto Request { get; }

        public UpdateRFQStatusCommand(UpdateRFQStatusDto request)
        {
            Request = request;
        }
    }
}
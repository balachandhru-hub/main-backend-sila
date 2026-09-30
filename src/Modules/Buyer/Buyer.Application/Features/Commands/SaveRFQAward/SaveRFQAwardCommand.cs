using Buyer.Domain.Dto;
using MediatR;

namespace Buyer.Application.Features.Commands.SaveRFQAward
{
    public class SaveRFQAwardCommand : IRequest<Guid>
    {
        public SaveRFQAwardDto Request { get; }

        public SaveRFQAwardCommand(SaveRFQAwardDto request)
        {
            Request = request;
        }
    }
}

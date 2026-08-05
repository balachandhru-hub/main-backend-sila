using Buyer.Domain.Dto;
using MediatR;

namespace Buyer.Application.Features.Commands.UpdateVerificationRequestStatus
{
    public class UpdateVerificationRequestStatusCommand : IRequest<bool>
    {
        public UpdateVerificationRequestStatusDto Request { get; set; }

        public UpdateVerificationRequestStatusCommand(
            UpdateVerificationRequestStatusDto request)
        {
            Request = request;
        }
    }
}
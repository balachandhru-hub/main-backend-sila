using MediatR;

namespace Buyer.Application.Features.Commands.Invitation
{
    public class UpdateSupplierVerificationStatusCommand : IRequest<bool>
    {
        public Guid RequestId { get; set; }
        public string Status { get; set; }
         public string? Remarks { get; set; }
    }
}
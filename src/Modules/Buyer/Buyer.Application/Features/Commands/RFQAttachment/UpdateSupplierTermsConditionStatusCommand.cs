using MediatR;

namespace Buyer.Application.Features.Commands.RFQAttachment
{
    public class UpdateSupplierTermsConditionStatusCommand : IRequest<Guid>
    {
        public Guid RFQId { get; set; }
        public string Status { get; set; }
    }
}

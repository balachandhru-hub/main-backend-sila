using MediatR;

namespace Supplier.Application.Features.Commands.RFQAttachment
{
    public class UpdateBuyerTermsConditionStatusCommand : IRequest<Guid>
    {
        public Guid RFQId { get; set; }
        public Guid OrganizationId { get; set; }
        public string Status { get; set; }
    }
}

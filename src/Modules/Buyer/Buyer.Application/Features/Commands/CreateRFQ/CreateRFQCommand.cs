using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Commands.CreateRFQ
{
   public class CreateRFQCommand : IRequest<Guid>
{
    public Guid OrganizationId { get; }
    public CreateRFQDto RFQ { get; }

    public CreateRFQCommand(Guid organizationId, CreateRFQDto rfq)
    {
        OrganizationId = organizationId;
        RFQ = rfq;
    }
}
}
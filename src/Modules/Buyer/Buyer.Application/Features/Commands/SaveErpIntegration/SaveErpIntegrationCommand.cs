using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Commands.SaveErpIntegration
{
    public class SaveErpIntegrationCommand : IRequest<Guid>
    {
        public Guid OrganizationId { get; set; }
        public ErpIntegrationWriteDto Request { get; set; } = new();
    }
}

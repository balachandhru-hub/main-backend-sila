using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Commands.UpdateErpIntegration
{
    public class UpdateErpIntegrationCommand : IRequest<Guid>
    {
        public Guid OrganizationId { get; set; }
        public Guid ConfigurationId { get; set; }
        public ErpIntegrationWriteDto Request { get; set; } = new();
    }
}

using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Commands.CreateErpIntegration
{
    public class CreateErpIntegrationCommand : IRequest<Guid>
    {
        public Guid OrganizationId { get; set; }
        public ErpIntegrationWriteDto Request { get; set; } = new();
    }
}

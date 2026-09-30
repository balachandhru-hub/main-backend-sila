using Buyer.Application.Services;
using Buyer.Domain.Dtos;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.SaveErpIntegration
{
    public class SaveErpIntegrationCommand : IRequest<Guid>
    {
        public Guid OrganizationId { get; set; }
        public ErpIntegrationWriteDto Request { get; set; } = new();
    }
}

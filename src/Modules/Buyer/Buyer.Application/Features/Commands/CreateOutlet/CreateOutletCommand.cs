using Buyer.Application.Services;
using Buyer.Domain.Dtos;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.CreateOutlet
{
    public class CreateOutletCommand : IRequest<Guid>
    {
        public Guid OrganizationId { get; set; }
        public OutletWriteDto Request { get; set; } = new();
    }
}

using Buyer.Application.Services;
using Buyer.Domain.Dtos;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Queries.GetErpIntegration
{
    public class GetErpIntegrationQuery : IRequest<ErpIntegrationResponseDto?>
    {
        public Guid OrganizationId { get; set; }
    }
}

using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Queries.GetErpIntegration
{
    public class GetErpIntegrationQuery : IRequest<List<ErpIntegrationResponseDto>>
    {
        public Guid OrganizationId { get; set; }
    }
}

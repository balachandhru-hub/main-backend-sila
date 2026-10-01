using MediatR;
using Operations.Domain.Dtos;

namespace Operations.Application.Features.Queries.GetIntegrationTargetFields
{
    /// <summary>
    /// Lists the purchase order and supplier fields an ERP payload can be mapped to.
    /// </summary>
    public class GetIntegrationTargetFieldsQuery : IRequest<List<IntegrationTargetFieldResponseDto>>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
    }
}

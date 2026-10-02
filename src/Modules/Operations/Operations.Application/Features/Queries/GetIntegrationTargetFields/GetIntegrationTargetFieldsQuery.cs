using MediatR;
using Operations.Domain.Dtos;
using Operations.Domain.Enums;

namespace Operations.Application.Features.Queries.GetIntegrationTargetFields
{
    /// <summary>
    /// Lists the fields an API payload can be mapped to: all of them, or those of one API type.
    /// </summary>
    public class GetIntegrationTargetFieldsQuery : IRequest<List<IntegrationTargetFieldResponseDto>>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public IntegrationProcessType? ProcessType { get; set; }
    }
}

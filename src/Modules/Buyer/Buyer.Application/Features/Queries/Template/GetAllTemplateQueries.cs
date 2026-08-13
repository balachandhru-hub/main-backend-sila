using Buyer.Domain.Dto;
using MediatR;

namespace Buyer.Application.Features.Queries.Template
{
    public class GetVerificationTemplatesQuery
        : IRequest<List<VerificationTemplateResponseDto>>
    {
        public Guid OrganizationId { get; set; }


    }
}
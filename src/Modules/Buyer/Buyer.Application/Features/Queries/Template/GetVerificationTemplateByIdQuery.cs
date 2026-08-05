using Buyer.Domain.Dto;
using MediatR;

namespace Buyer.Application.Features.Queries.Template
{
    public class GetVerificationTemplateByIdQuery : IRequest<VerificationTemplateResponseDto>
    {
        public Guid TemplateId { get; set; }
    }
}
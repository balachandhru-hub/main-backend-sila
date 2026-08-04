using Buyer.Domain.Dto;
using MediatR;

namespace Buyer.Application.Features.Queries.DefaultTemplate
{
    public class GetDefaultVerificationTemplateQuery : IRequest<GetDefaultVerificationTemplateDto>
    {
        public Guid SupplierId { get; set; }
    }
}
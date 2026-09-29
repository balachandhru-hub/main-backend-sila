using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Queries.GetAllContractTemplates
{
    public class GetAllContractTemplatesQuery : IRequest<List<ContractTemplateResponseDto>>
    {
        public Guid BuyerId { get; set; }
        public int Index { get; set; }
        public int Limit { get; set; }
    }
}

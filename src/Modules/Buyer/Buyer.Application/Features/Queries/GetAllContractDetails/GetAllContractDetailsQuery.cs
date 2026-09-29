using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Queries.GetAllContractDetails
{
    public class GetAllContractDetailsQuery : IRequest<List<ContractDetailsResponseDto>>
    {
        public Guid BuyerId { get; set; }
        public int Index { get; set; }
        public int Limit { get; set; }
    }
}

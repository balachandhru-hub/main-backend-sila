using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Queries.GetContractDetails
{
    public class GetContractDetailsQuery : IRequest<ContractDetailsResponseDto>
    {
        public Guid Id { get; }

        public GetContractDetailsQuery(Guid id)
        {
            Id = id;
        }
    }
}

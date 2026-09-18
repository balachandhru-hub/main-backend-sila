using Buyer.Domain.Dto;
using MediatR;

namespace Buyer.Application.Features.Queries.GetContract
{
    public class GetContractQuery : IRequest<ContractResponseDto>
    {
        public Guid ContractId { get; }

        public GetContractQuery(Guid contractId)
        {
            ContractId = contractId;
        }
    }
}

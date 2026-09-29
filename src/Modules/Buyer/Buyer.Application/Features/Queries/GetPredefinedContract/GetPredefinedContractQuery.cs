using Buyer.Domain.Dto;
using MediatR;

namespace Buyer.Application.Features.Queries.GetPredefinedContract
{
    public class GetPredefinedContractQuery : IRequest<PredefinedContractResponseDto>
    {
        public Guid ContractId { get; }

        public GetPredefinedContractQuery(Guid contractId)
        {
            ContractId = contractId;
        }
    }
}

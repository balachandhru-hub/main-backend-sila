using Buyer.Domain.Dto;
using MediatR;

namespace Buyer.Application.Features.Queries.GetAllPredefinedContracts
{
    public class GetAllPredefinedContractsQuery : IRequest<List<PredefinedContractResponseDto>>
    {
        public int Index { get; set; }
        public int Limit { get; set; }
        public Guid BuyerId { get; set; }
        public Guid UserId { get; set; }
        public Guid RoleId { get; set; }
    }
}

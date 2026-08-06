using Buyer.Domain.Dto;
using MediatR;

namespace Buyer.Application.Features.Queries.GetAllBuyers
{
    public class GetAllBuyersQuery : IRequest<List<GetAllBuyerDto>>
    {
        public int Index { get; set; } 

        public int Limit { get; set; } 
        public string? OrganizationName { get; set; }

        public string? Status { get; set; }
    }
}